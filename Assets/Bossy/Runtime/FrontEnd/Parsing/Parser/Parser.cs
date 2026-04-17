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
            var switchResult = ParseSwitches(argTokens, schema, command);
            if (switchResult.IsError) return ParseStep<ICommand>.Fail(switchResult.Error);

            // 4. Parse in positionals - return on error or not enough
            var positionalResult = ParsePositionals(argTokens, schema, command);
            if (positionalResult.IsError) return ParseStep<ICommand>.Fail(positionalResult.Error);
            
            // 5. Parse in optionals - return on error
            var optionalResult = ParseOptionals(argTokens, schema, command);
            if (optionalResult.IsError) return ParseStep<ICommand>.Fail(optionalResult.Error);
            
            // 6. Parse in variadic - return on error
            var variadicResult = ParseVariadic(argTokens, schema, command);
            if (variadicResult.IsError) return ParseStep<ICommand>.Fail(variadicResult.Error);
            
            return ParseStep<ICommand>.Ok(command);
        }

        private ParseStep ParseSwitches(List<string> argTokens, CommandSchema schema, ICommand command)
        {
            var idx = 0;
            
            while (idx < argTokens.Count)
            {
                ArgumentSchema argSchema;
                var token = argTokens[idx];
                
                // 1. Only match with - or -- style arguments
                if (!token.StartsWith("-"))
                {
                    idx++;
                    continue;
                }
                
                // 2. Get matching argument in schema
                if (token.StartsWith("--"))
                {
                    if (!schema.TryFindSwitch(token[2..], out argSchema))
                    {
                        return ParseStep.Fail(new InvalidSwitchError(token));
                    }
                }
                else
                {
                    if (!schema.TryFindSwitch(token[1], out argSchema))
                    {
                        return ParseStep.Fail(new InvalidSwitchError(token));
                    }
                }
                
                // 3. Remove name from tokens and create greedy substream
                argTokens.RemoveAt(idx);
                var substream = new TokenStream(argTokens.GetRange(idx, argTokens.Count - idx));
                
                // 4. Parse and remove consumed tokens 
                var adaptResult = _adapterRegistry.TryConvert(argSchema.Type, substream, out var value);
                if (!adaptResult.Success)
                {
                    return ParseStep.Fail(new TypeAdaptError(argSchema.Type, token, adaptResult.ErrorMessage));
                }
                
                argTokens.RemoveRange(idx, substream.Cursor);
                
                // 5. Run switch validators
                // TODO: Add when validators are added
                
                // 6. Set field value
                argSchema.SetValue(command, value);
            }

            return ParseStep.Ok();
        }

        private ParseStep ParsePositionals(List<string> argTokens, CommandSchema schema, ICommand command)
        {
            var positionals = schema.GetOrderedPositionalArguments();

            // 1. Loop over all positionals 
            foreach (var argSchema in positionals)
            {
                // 2. If we run out of tokens but expect an argument, this is an error
                if (argTokens.Count <= 0)
                {
                    return ParseStep.Fail(new MissingPositionalError(argSchema.Type, argSchema.Name));
                }
                
                // 3. Try parsing the tokens to the argument schema and remove consumed tokens
                var substream = new TokenStream(argTokens);
                var adaptResult = _adapterRegistry.TryConvert(argSchema.Type, substream, out var value);
                if (!adaptResult.Success)
                {
                    return ParseStep.Fail(new TypeAdaptError(argSchema.Type, argTokens[0], adaptResult.ErrorMessage));
                }
                
                argTokens.RemoveRange(0, substream.Cursor);
                
                // 4. Run validators
                // TODO: Run validators
                
                // 5. Set the value
                argSchema.SetValue(command, value);
            }

            return ParseStep.Ok();
        }
        
        private ParseStep ParseOptionals(List<string> argTokens, CommandSchema schema, ICommand command)
        {
            var optionals = schema.GetOrderedOptionalArguments();

            // 1. Loop over all optionals 
            foreach (var argSchema in optionals)
            {
                // 2. Running out of tokens for optionals is OK, but they must appear in order
                if (argTokens.Count <= 0) return ParseStep.Ok();
                
                // 3. Try parsing the tokens to the argument schema and remove consumed tokens
                var substream = new TokenStream(argTokens);
                var adaptResult = _adapterRegistry.TryConvert(argSchema.Type, substream, out var value);

                if (!adaptResult.Success)
                {
                    if (!schema.TryGetVariadic(out _))
                    {
                        return ParseStep.Fail(new TypeAdaptError(argSchema.Type, argTokens[0], adaptResult.ErrorMessage));
                    }
                    return ParseStep.Ok();
                }
                
                argTokens.RemoveRange(0, substream.Cursor);
                
                // 4. Run validators
                // TODO: Run validators
                
                // 5. Set the value
                argSchema.SetValue(command, value);
            }

            return ParseStep.Ok();
        }

        private ParseStep ParseVariadic(List<string> argTokens, CommandSchema schema, ICommand command)
        {
            // 1. If there are not variadic args, we are done but must check for unexpected tokens
            if (!schema.TryGetVariadic(out var variadic))
            {
                return argTokens.Count > 0 
                    ? ParseStep.Fail(new UnexpectedTokensError(argTokens))
                    :  ParseStep.Ok();
            }

            // This is guaranteed to be an array by the validator
            var type = variadic.Type.GetElementType()!;

            var args = new List<object>();
            
            // 2. Loop over all remaining tokens to put into variadic args
            while (argTokens.Count > 0)
            {
                var substream = new TokenStream(argTokens);
                var result = _adapterRegistry.TryConvert(type, substream, out var value);
                if (!result.Success)
                {
                    return ParseStep.Fail(new TypeAdaptError(type, argTokens[0], result.ErrorMessage));
                }
                
                argTokens.RemoveRange(0, substream.Cursor);
                
                // 3. Run validators
                // TODO: Run validators
                
                // 4. Add to list
                args.Add(value);
            }

            // 5. Set the arguments' value
            var array = Array.CreateInstance(type, args.Count);
            for (var i = 0; i < args.Count; i++)
            {
                array.SetValue(args[i], i);
            }
            
            variadic.SetValue(command, array);
            
            return ParseStep.Ok();
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

            var query = _schemaRegistry.TryResolveSchema(root, out var schema);
            
            if (query is SchemaQueryStatus.NotFound)
            {
                return ParseStep<CommandSchema>.Fail(new NoMatchingCommandError(root));
            }

            if (query is SchemaQueryStatus.Invalid)
            {
                return ParseStep<CommandSchema>.Fail(new InvalidSchemaError(new List<string> { root }));
            }

            var subcommands = new List<string>();
            while (stream.TryPeek(out var next))
            {
                query = _schemaRegistry.TryResolveSchema(root, subcommands.Append(next), out schema);
                
                if (query is SchemaQueryStatus.NotFound)
                {
                    break;
                }
                
                subcommands.Add(next);

                // Greedily fail on invalid commands to raise the issue
                if (query is SchemaQueryStatus.Invalid)
                {
                    return  ParseStep<CommandSchema>.Fail(new InvalidSchemaError(subcommands));
                }
                
                stream.TryConsume(out _);
            }

            // Get last successful command
            _schemaRegistry.TryResolveSchema(root, subcommands, out schema);
            return ParseStep<CommandSchema>.Ok(schema);
        }

        private static CommandGraph BuildGraph(List<ParseNode> pipeline, bool isWindowed)
        {
            var builder = CommandGraph.Create(isWindowed).Execute(pipeline.First().Command);
            pipeline = pipeline.Skip(1).ToList();
            
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
        
        
        private readonly struct ParseStep
        {
            public readonly ParseResult Error;
            public bool IsError => Error != null;

            private ParseStep(ParseResult error) { Error = error; }

            public static ParseStep Ok() => new(null);
            public static ParseStep Fail(ParseResult error) => new(error);
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