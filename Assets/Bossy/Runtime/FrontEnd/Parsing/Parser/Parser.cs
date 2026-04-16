using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Registry;
using Bossy.Schema;
using Bossy.Shell;

namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// Parses a string input to a <see cref="CommandGraph"/>.
    /// </summary>
    internal class Parser
    {
        private readonly SchemaRegistry _schemaRegistry;
        private readonly TypeAdapterRegistry _adapterRegistry;
        private readonly OperatorList _operators;
        
        public Parser(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, OperatorList operators)
        {
            _schemaRegistry = schemaRegistry;
            _adapterRegistry = adapterRegistry;
            _operators = operators;
        }

        /// <summary>
        /// Parses a string into a command graph.
        /// </summary>
        /// <param name="input">The input to parse.</param>
        /// <returns>True is parsing was successful, otherwise false.</returns>
        public ParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new EmptyInputError();
            
            // 1. Expand macros
            // TODO: Likely inject the session Macro/Alias manager on construction
            
            // 2. Split into nodes
            var tokens = Tokenizer.Tokenize(input, _operators.ToEnumerable());
            var pipelineResult = MakePipeline(tokens, out var isWindowed);
            if (pipelineResult.IsError) return pipelineResult.Error;
            
            // 3. Parse each node to a command
            var pipeline = pipelineResult.Value;
            foreach (var node in pipeline)
            {
                var commandResult = ParseCommand(node.Tokens);
                if (commandResult.IsError) return commandResult.Error;
                node.Command = commandResult.Value;
            }
            
            // 4. Build the command graph.
            return new ParseSucceeded(BuildGraph(pipeline, isWindowed));
        }

        private ParseStep<List<ParseNode>> MakePipeline(List<string> tokens, out bool isWindowed)
        {
            var pipeline = new List<ParseNode>();
            var current = new List<string>();
            string lastOperator = null;
            isWindowed = false;
            
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var isFirst = i == 0;
                var isLast = i == tokens.Count - 1;

                if (token == _operators.WindowOperator)
                {
                    if (!isFirst && !isLast)
                    {
                        return ParseStep<List<ParseNode>>.Fail(new BadWindowOperatorError());
                    }

                    isWindowed = true;
                    lastOperator = token;
                    continue;
                }

                var link = TokenToLink(token);
                if (link.HasValue)
                {
                    if (lastOperator != null)
                    {
                        return ParseStep<List<ParseNode>>.Fail(new ContiguousOperatorsError(lastOperator, token));
                    }

                    if (isFirst || isLast)
                    {
                        return ParseStep<List<ParseNode>>.Fail(new BadOperatorPositionError(token));
                    }

                    pipeline.Add(new ParseNode(current, link.Value));
                    current = new List<string>();
                    lastOperator = token;
                }
                else
                {
                    current.Add(token);
                    lastOperator = null;
                }
            }

            if (current.Count > 0) pipeline.Add(new ParseNode(current, CommandGraphLink.None));
            return ParseStep<List<ParseNode>>.Ok(pipeline);
        }
        
        private CommandGraphLink? TokenToLink(string token)
        {
            if (token == _operators.ThenOperator) return CommandGraphLink.Then;
            if (token == _operators.AndOperator) return CommandGraphLink.And;
            if (token == _operators.OrOperator) return CommandGraphLink.Or;
            if (token == _operators.PipeOperator) return CommandGraphLink.Pipe;
            return null;
        }

        private ParseStep<ICommand> ParseCommand(IEnumerable<string> tokens)
        {
            var stream = new TokenStream(tokens);
            
            var result = GetSchema(stream);
            if (result.IsError) return ParseStep<ICommand>.Fail(result.Error);
            var schema = result.Value;
            
            // Begin parsing command as per schema
            var command = schema.Instantiate();
            
            // 1. Explode remaining tokens into arg list
            var argTokens = stream.Explode();
            
            // 2. Expand aggregate flags
            ExpandAggregateFlags(argTokens);
            
            // 3. Foreach flag starting with - or -- ensure match in schema
            for (var i = 0; i < argTokens.Count; i++)
            {
                var token = argTokens[i];
                
                if (!token.StartsWith("-")) continue;
                
                // 3.a. If false - return unknown switch
                ArgumentSchema argSchema;
                if (token.StartsWith("--"))
                {
                    if (!schema.TryFindSwitch(token[2..], out argSchema))
                    {
                        return ParseStep<ICommand>.Fail(new InvalidSwitchError(token));
                    }
                }
                else
                {
                    if (!schema.TryFindSwitch(token[1], out argSchema))
                    {
                        return ParseStep<ICommand>.Fail(new InvalidSwitchError(token));
                    }
                }
                
                // 3.b. Remove name from parent list and dump every value after into substream
                argTokens.RemoveAt(i);
                var substream = new TokenStream(argTokens.GetRange(i, argTokens.Count - i));
                
                // 3.c. Parse (return on error) and check position of stream. Remove that many tokens from overall list 
                var targetType = argSchema.FieldInfo.FieldType;
                var adaptResult = _adapterRegistry.TryConvert(targetType, substream, out var value);
                if (!adaptResult.Success)
                {
                    return ParseStep<ICommand>.Fail(new TypeAdaptError(targetType, token, adaptResult.ErrorMessage));
                }
                
                argSchema.FieldInfo.SetValue(command, value);
                
                // 3.d. Run switch validators
            }
                
                
            // 4. Parse in positionals - return on error or not enough
            
            // 5. Parse in optionals - return on error
            
            // 6. Parse in variadic - return on error
            
            return ParseStep<ICommand>.Ok(null);
        }

        private static void ExpandAggregateFlags(List<string> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (!token.StartsWith("-") || token.StartsWith("--") || token.Length <= 2) continue;

                var flags = token[1..].Select(f => $"-{f}").ToList();
                tokens.RemoveAt(i);
                tokens.InsertRange(i, flags);
                i += flags.Count - 1;
            }
        }

        private ParseStep<CommandSchema> GetSchema(TokenStream stream)
        {
            // Stream will always have at least one token due to previous error checking
            stream.TryConsume(out var root);

            if (!_schemaRegistry.TryResolveSchema(root, out var schema))
            {
                return ParseStep<CommandSchema>.Fail(new NoMatchingCommandError(root));
            }

            var subcommands = new List<string>();
            while (stream.TryPeek(out var next))
            {
                if (!_schemaRegistry.TryResolveSchema(root, subcommands.Append(next), out schema))
                {
                    break;
                }
                
                subcommands.Add(next);
                stream.TryConsume(out _);
            }

            // Get last successful command
            _schemaRegistry.TryResolveSchema(root, subcommands, out schema);
            return ParseStep<CommandSchema>.Ok(schema);
        }

        private static CommandGraph BuildGraph(List<ParseNode> pipeline, bool isWindowed)
        {
            var builder = CommandGraph.Create(isWindowed).Execute(pipeline.First().Command);
            pipeline.RemoveAt(0);
            
            foreach (var node in pipeline)
            {
                switch (node.Link)
                {
                    case CommandGraphLink.None:
                        return builder.Build();
                    case CommandGraphLink.Then:
                        builder.Then(node.Command);
                        break;
                    case CommandGraphLink.And:
                        builder.And(node.Command);
                        break;
                    case CommandGraphLink.Or:
                        builder.Or(node.Command);
                        break;
                    case CommandGraphLink.Pipe:
                        builder.AndPipeTo(node.Command);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            return builder.Build();
        }
        
        private readonly struct ParseStep<T>
        {
            public readonly T Value;
            public readonly ParseResult Error;
            public bool IsError => Error != null;

            private ParseStep(T value, ParseResult error) { Value = value; Error = error; }

            public static ParseStep<T> Ok(T value) => new(value, null);
            public static ParseStep<T> Fail(ParseResult error) => new(default, error);
        }
    }
}