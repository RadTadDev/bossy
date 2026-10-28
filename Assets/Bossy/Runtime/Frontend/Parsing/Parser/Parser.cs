using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bossy.Command;
using Bossy.Schema.Registry;
using Bossy.Schema;
using Bossy.Execution;
using Bossy.Utils;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// Parses a string input to a <see cref="CommandGraph"/>.
    /// </summary>
    public class Parser
    {
        private readonly SchemaRegistry _schemaRegistry;
        private readonly TypeAdapterRegistry _adapterRegistry;
        
        public Parser(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry)
        {
            _schemaRegistry = schemaRegistry;
            _adapterRegistry = adapterRegistry;
        }
        
        /// <summary>
        /// Parses a string into a command graph.
        /// </summary>
        /// <param name="input">The input to parse.</param>
        /// <param name="operators">The operators to use.</param>
        /// <param name="aliases">An optional list of aliases to expand before parsing.</param>
        /// <returns>True is parsing was successful, otherwise false.</returns>
        public ParseResult Parse(string input, OperatorList operators, Dictionary<string, string> aliases = null)
        {
            var bindResult = Bind(input, operators, aliases, out var isWindowed);
            if (bindResult.IsError) return bindResult.Error;
            var pipeline = bindResult.Value;

            // 1. Create each command and give it its bound values
            foreach (var node in pipeline)
            {
                node.Command = node.Schema.Instantiate();
                foreach (var (argSchema, value) in node.Values)
                {
                    argSchema.SetValue(node.Command, value);
                }
            }

            // 2. Run validators on the arguments left at their default values
            foreach (var node in pipeline)
            {
                var argResult = ValidateDefaultArguments(node);
                if (argResult.IsError) return argResult.Error;
            }

            // 3. Build the command graph.
            return new ParseSucceeded(BuildGraph(pipeline, isWindowed));
        }

        /// <summary>
        /// Checks whether a string would parse, without creating any command. Arguments the input does not set are
        /// not validated, since their default values are only known once a command is created.
        /// </summary>
        /// <param name="input">The input to check.</param>
        /// <param name="operators">The operators to use.</param>
        /// <param name="aliases">An optional list of aliases to expand before parsing.</param>
        /// <param name="error">The parse error, or null if the input would parse.</param>
        /// <returns>True if the input would parse, otherwise false.</returns>
        internal bool TryCheck(string input, OperatorList operators, Dictionary<string, string> aliases, out ParseResult error)
        {
            var bindResult = Bind(input, operators, aliases, out _);
            error = bindResult.Error;
            return !bindResult.IsError;
        }

        private ParseStep<List<ParseNode>> Bind(string input, OperatorList operators, Dictionary<string, string> aliases, out bool isWindowed)
        {
            isWindowed = false;
            if (string.IsNullOrWhiteSpace(input)) return ParseStep<List<ParseNode>>.Fail(new EmptyInputError());

            // 1. Expand macros
            if (ShouldExpandAliases(input, aliases))
            {
                input = ExpandAliases(input, aliases);
            }

            // 2. Split into nodes
            var tokens = Tokenizer.Tokenize(input, operators.ToEnumerable());
            var pipelineResult = MakePipeline(tokens, operators, out isWindowed);
            if (pipelineResult.IsError) return pipelineResult;

            // 3. Bind each node's tokens to its command's arguments
            var pipeline = pipelineResult.Value;
            foreach (var node in pipeline)
            {
                // Values are collected rather than set, so no command is created until binding succeeds
                node.Values = new Dictionary<ArgumentSchema, object>();
                var commandResult = BindCommand(node.Tokens, node.Values);
                if (commandResult.IsError) return ParseStep<List<ParseNode>>.Fail(commandResult.Error);
                node.Schema = commandResult.Value;
            }

            // 4. Run validators on the bound values
            foreach (var node in pipeline)
            {
                var argResult = ValidateBoundArguments(node);
                if (argResult.IsError) return ParseStep<List<ParseNode>>.Fail(argResult.Error);
            }

            return pipelineResult;
        }

        private bool ShouldExpandAliases(string input, Dictionary<string, string> aliases)
        {
            if (aliases == null) return false;

            // Don't expand if this command is the alias remove command
            input = input.Trim();

            if (!input.StartsWith("alias")) return true;
            
            input = input["alias".Length..].Trim();

            return !input.StartsWith("remove");
        }
        
        private static string ExpandAliases(string input, Dictionary<string, string> aliases)
        {
            return string.Join(" ", Regex.Matches(input, @"""[^""]*""|(\S+)")
                .Select(m => m.Value.StartsWith("\"")
                    ? m.Value
                    : aliases.TryGetValue(m.Value, out var alias) ? alias : m.Value));
        }

        private ParseStep<List<ParseNode>> MakePipeline(List<Token> tokens, OperatorList operators, out bool isWindowed)
        {
            var pipeline = new List<ParseNode>();
            var current = new List<Token>();
            string lastOperator = null;
            isWindowed = false;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i].Text;
                var isFirst = i == 0;
                var isLast = i == tokens.Count - 1;

                // Quoted text that looks like an operator is a plain value
                if (!tokens[i].IsOperator)
                {
                    current.Add(tokens[i]);
                    lastOperator = null;
                    continue;
                }

                if (token == operators.WindowOperator)
                {
                    if (!isFirst && !isLast)
                    {
                        return ParseStep<List<ParseNode>>.Fail(new BadWindowOperatorError());
                    }

                    isWindowed = true;
                    lastOperator = token;
                    continue;
                }

                if (lastOperator != null)
                {
                    return ParseStep<List<ParseNode>>.Fail(new ContiguousOperatorsError(lastOperator, token));
                }

                if (isFirst || isLast)
                {
                    return ParseStep<List<ParseNode>>.Fail(new BadOperatorPositionError(token));
                }

                pipeline.Add(new ParseNode(current, TokenToLink(operators, token)));
                current = new List<Token>();
                lastOperator = token;
            }

            if (current.Count > 0)
            {
                pipeline.Add(new ParseNode(current, CommandGraphLink.None));
            }
            return ParseStep<List<ParseNode>>.Ok(pipeline);
        }
        
        private CommandGraphLink TokenToLink(OperatorList operators, string token)
        {
            if (token == operators.ThenOperator) return CommandGraphLink.Then;
            if (token == operators.AndOperator) return CommandGraphLink.And;
            if (token == operators.OrOperator) return CommandGraphLink.Or;
            if (token == operators.PipeOperator) return CommandGraphLink.Pipe;
            throw new ArgumentOutOfRangeException(nameof(token), token, "Token is not a linking operator.");
        }

        private ParseStep<CommandSchema> BindCommand(IReadOnlyList<Token> tokens, Dictionary<ArgumentSchema, object> values)
        {
            var stream = new TokenStream(Texts(tokens));

            var result = GetSchema(stream);
            if (result.IsError) return ParseStep<CommandSchema>.Fail(result.Error);
            var schema = result.Value;

            // 1. Collect the tokens after the command path into the arg list
            var argTokens = tokens.Skip(stream.Cursor).ToList();
            
            // 2. Expand aggregate flags
            var aggregateResult = ExpandAggregateFlags(argTokens, schema);
            if (aggregateResult.IsError) return ParseStep<CommandSchema>.Fail(aggregateResult.Error);
            
            // 3. Foreach flag starting with - or -- ensure match in schema
            var switchResult = ParseSwitches(argTokens, schema, values);
            if (switchResult.IsError) return ParseStep<CommandSchema>.Fail(switchResult.Error);

            // 4. Parse in positionals - return on error or not enough
            var positionalResult = ParsePositionals(argTokens, tokens, schema, values);
            if (positionalResult.IsError) return ParseStep<CommandSchema>.Fail(positionalResult.Error);
            
            // 5. Parse in optionals - return on error
            var optionalResult = ParseOptionals(argTokens, tokens, schema, values);
            if (optionalResult.IsError) return ParseStep<CommandSchema>.Fail(optionalResult.Error);
            
            // 6. Parse in variadic - return on error
            var variadicResult = ParseVariadic(argTokens, tokens, schema, values);
            if (variadicResult.IsError) return ParseStep<CommandSchema>.Fail(variadicResult.Error);
            
            return ParseStep<CommandSchema>.Ok(schema);
        }

        private ParseStep ParseSwitches(List<Token> argTokens, CommandSchema schema, Dictionary<ArgumentSchema, object> values)
        {
            var idx = 0;
            
            while (idx < argTokens.Count)
            {
                ArgumentSchema argSchema;
                
                // 1. Only match with - or -- style arguments
                if (!argTokens[idx].IsSwitch)
                {
                    idx++;
                    continue;
                }
                
                var token = argTokens[idx].Text;
                
                // 2. Get matching argument in schema
                if (token.StartsWith("--"))
                {
                    // This allows explicit variadic consumption
                    if (token == "--")
                    {
                        argTokens.RemoveAt(idx);
                        return ParseStep.Ok();
                    }
                    
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
                var substream = new TokenStream(Texts(argTokens.GetRange(idx, argTokens.Count - idx)));
                
                // 4. Parse and remove consumed tokens 
                var adaptResult = _adapterRegistry.TryConvert(argSchema.Type, substream, out var value);
                if (!adaptResult.Success)
                {
                    token = argTokens.Count > idx ? argTokens[idx].Text : "(null)";
                    return ParseStep.Fail(new TypeAdaptError(argSchema.Type, token, adaptResult.ErrorMessage));
                }
                
                argTokens.RemoveRange(idx, substream.Cursor);
                
                // 5. Record the value
                values[argSchema] = value;
            }

            return ParseStep.Ok();
        }

        private ParseStep ParsePositionals(List<Token> argTokens, IReadOnlyList<Token> lineTokens, CommandSchema schema, Dictionary<ArgumentSchema, object> values)
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
                var substream = new TokenStream(Texts(argTokens));
                var adaptResult = _adapterRegistry.TryConvert(argSchema.Type, substream, out var value, true);
                if (!adaptResult.Success)
                {
                    return ParseStep.Fail(new TypeAdaptError(argSchema.Type, argTokens[0].Text, adaptResult.ErrorMessage));
                }
                
                var takeResult = TakeValueTokens(argTokens, substream.Cursor, argSchema, lineTokens);
                if (takeResult.IsError) return takeResult;
                
                // 4. Record the value
                values[argSchema] = value;
            }

            return ParseStep.Ok();
        }
        
        private ParseStep ParseOptionals(List<Token> argTokens, IReadOnlyList<Token> lineTokens, CommandSchema schema, Dictionary<ArgumentSchema, object> values)
        {
            var optionals = schema.GetOrderedOptionalArguments();

            // 1. Loop over all optionals 
            foreach (var argSchema in optionals)
            {
                // 2. Running out of tokens for optionals is OK, but they must appear in order
                if (argTokens.Count <= 0) return ParseStep.Ok();
                
                // 3. Try parsing the tokens to the argument schema and remove consumed tokens
                var substream = new TokenStream(Texts(argTokens));
                var adaptResult = _adapterRegistry.TryConvert(argSchema.Type, substream, out var value, true);

                if (!adaptResult.Success)
                {
                    if (!schema.TryGetVariadic(out _))
                    {
                        return ParseStep.Fail(new TypeAdaptError(argSchema.Type, argTokens[0].Text, adaptResult.ErrorMessage));
                    }
                    return ParseStep.Ok();
                }
                
                var takeResult = TakeValueTokens(argTokens, substream.Cursor, argSchema, lineTokens);
                if (takeResult.IsError) return takeResult;
                
                // 4. Record the value
                values[argSchema] = value;
            }

            return ParseStep.Ok();
        }

        private ParseStep ParseVariadic(List<Token> argTokens, IReadOnlyList<Token> lineTokens, CommandSchema schema, Dictionary<ArgumentSchema, object> values)
        {
            // 1. If there are not variadic args, we are done but must check for unexpected tokens
            if (!schema.TryGetVariadic(out var variadic))
            {
                return argTokens.Count > 0 
                    ? ParseStep.Fail(new UnexpectedTokensError(Texts(argTokens).ToList()))
                    :  ParseStep.Ok();
            }

            // This is guaranteed to be an array by the validator
            var type = variadic.Type.GetElementType()!;

            var args = new List<object>();

            // 2. Loop over all remaining tokens to put into variadic args
            while (argTokens.Count > 0)
            {
                var substream = new TokenStream(Texts(argTokens));
                var result = _adapterRegistry.TryConvert(type, substream, out var value, true);
                if (!result.Success)
                {
                    return ParseStep.Fail(new TypeAdaptError(type, argTokens[0].Text, result.ErrorMessage));
                }
                
                var takeResult = TakeValueTokens(argTokens, substream.Cursor, variadic, lineTokens);
                if (takeResult.IsError) return takeResult;
                
                // 3. Add to list
                args.Add(value);
            }

            // 4. Record the arguments' value
            var array = Array.CreateInstance(type, args.Count);
            for (var i = 0; i < args.Count; i++)
            {
                array.SetValue(args[i], i);
            }
            
            values[variadic] = array;
            
            return ParseStep.Ok();
        }
        
        /*
         * Removes the tokens an ordered value used. Switches are removed before ordered values are read, so the
         * tokens of a value may have had a switch between them in the line, as in "vec 1 -f 2 3". That is an
         * error, so a value's tokens must be next to each other in the line.
         */
        private static ParseStep TakeValueTokens(List<Token> argTokens, int count, ArgumentSchema argSchema, IReadOnlyList<Token> lineTokens)
        {
            for (var i = 1; i < count; i++)
            {
                var previousEnd = argTokens[i - 1].End;
                var nextStart = argTokens[i].Start;

                foreach (var token in lineTokens)
                {
                    if (token.Start >= previousEnd && token.End <= nextStart)
                    {
                        return ParseStep.Fail(new SplitValueError(token.Text, argSchema.Name));
                    }
                }
            }

            argTokens.RemoveRange(0, count);
            return ParseStep.Ok();
        }

        private static ParseStep ExpandAggregateFlags(List<Token> tokens, CommandSchema schema)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!tokens[i].IsSwitch) continue;
                
                var token = tokens[i].Text;

                // Nothing after "--" is a switch, so nothing after it is expanded
                if (token == "--") return ParseStep.Ok();

                if (token.StartsWith("--") || token.Length <= 2) continue;

                // Only bool switches can be combined, since any other switch needs a value after it
                foreach (var shortName in token[1..])
                {
                    if (!schema.TryFindSwitch(shortName, out var argSchema))
                    {
                        return ParseStep.Fail(new InvalidSwitchError($"-{shortName}"));
                    }

                    if (argSchema.Type != typeof(bool))
                    {
                        return ParseStep.Fail(new NonBoolAggregateSwitchError(token, shortName));
                    }
                }

                var flags = token[1..].Select(f => new Token($"-{f}")).ToList();
                tokens.RemoveAt(i);
                tokens.InsertRange(i, flags);
                i += flags.Count - 1;
            }

            return ParseStep.Ok();
        }

        private static IEnumerable<string> Texts(IEnumerable<Token> tokens)
        {
            return tokens.Select(t => t.Text);
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
                
                if (query is SchemaQueryStatus.NotFound) break;
                
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

        private static ParseStep ValidateBoundArguments(ParseNode node)
        {
            foreach (var (arg, value) in node.Values)
            {
                var result = ValidateArgument(arg, value);
                if (result.IsError) return result;
            }

            return ParseStep.Ok();
        }

        private static ParseStep ValidateDefaultArguments(ParseNode node)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (var arg in node.Schema.Arguments)
            {
                if (node.Values.ContainsKey(arg)) continue;

                var field = node.Command.GetType().GetField(arg.FieldInfo.Name, bindingFlags);
                var result = ValidateArgument(arg, field!.GetValue(node.Command));
                if (result.IsError) return result;
            }

            return ParseStep.Ok();
        }

        private static ParseStep ValidateArgument(ArgumentSchema arg, object value)
        {
            foreach (var validator in arg.Validators)
            {
                var result = validator.Validate(value);

                if (!result.Success)
                {
                    return ParseStep.Fail(new ArgumentValidationError(result.Message, arg.Name));
                }
            }

            return ParseStep.Ok();
        }

        private static CommandGraph BuildGraph(List<ParseNode> pipeline, bool isWindowed)
        {
            var first = pipeline.First();
            pipeline = pipeline.Skip(1).ToList();
            
            var builder = CommandGraph.Create(isWindowed).Execute(first.Command, first.Schema);
            var linkToCurrent = first.Link;

            foreach (var node in pipeline)
            {
                switch (linkToCurrent)
                {
                    case CommandGraphLink.None:
                        // This should not happen as we switch on the previous link...
                        return builder.Build();
                    case CommandGraphLink.Then:
                        builder.Then(node.Command, node.Schema);
                        break;
                    case CommandGraphLink.And:
                        builder.And(node.Command, node.Schema);
                        break;
                    case CommandGraphLink.Or:
                        builder.Or(node.Command, node.Schema);
                        break;
                    case CommandGraphLink.Pipe:
                        builder.AndPipeTo(node.Command, node.Schema);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                linkToCurrent = node.Link;
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