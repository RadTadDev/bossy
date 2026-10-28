# Autocomplete Rules

This is the rule catalogue for the autocomplete engine (`Runtime/Frontend/Autocomplete`). It is for people working on Bossy itself; command authors should read the Auto Complete section of the README instead.

The rule numbers are cited in comments in `AutocompleteEngine` and `CompletionWalker`, and each rule is pinned by a test in `AutocompleteEngineTest`, whose sections are headed with the same numbers. When you change a rule, update this file, the comment, and the test together.

Notation: `|` is the cursor. "At the cursor" means the token is still being typed (no space after it). "Moved on" means a space or more tokens follow it.

---

## Input and cursor

- **R1.** Only the text before the cursor decides what is suggested. Tokens after the cursor are ignored. *Changed by P6:* a completion replaces the whole token at the cursor and keeps the text after it.
- **R2.** A space before the cursor means "predict the next token". No space means "complete the token at the cursor".
- **R3.** Empty or whitespace-only input gives no completions and no message.

## Command names

- **R4.** The first token is matched against root command names and aliases (see R30 for ranking). *Changed by P8:* if nothing matches, the status line says `No command matches 'x'` while typing, and it is an error once moved on.
- **R5.** While the first token is at the cursor, the matching command names and aliases are listed. Aliases show their expansion as the description.
- **R6.** Once moved on, the first token resolves to the exact command if there is one, otherwise to the closest match. So `hepl -r ` still gets `help`'s suggestions. This is intentional. *Extended by P7:* the correction (`hepl` → `help`) is offered as the first row.

## Operators

- **R7.** *Replaced by P2.* Originally, a window operator anywhere after the first token gave `Ready to execute`.
- **R8.** Any other operator ends a command, and completion restarts on the text after it. *Extended by P1:* the commands before the operator must parse first, or their parser error is shown.

## Quotes

- **R9.** *Replaced by P5.* Originally, an unfinished quote only showed a hint.

## Switches

Until a `--` token, a token that starts with `-` is a switch, unless it is quoted or is a negative number (P16).

- **R10.** `--|` lists every long switch not used yet. `-- ` (moved on) is the surrender token: no later token is a switch.
- **R11.** `--na|` lists the long switches that match `na`. If none match, the status line says `No switch matches '--na'`.
- **R12.** `-|` lists every switch not used yet. A lone `-` moved on is an error: `'-' is not a valid token`.
- **R13.** `-abc` (more than one letter):
  - If every letter is a bool switch, at the cursor the status line says `Aggregate flags: -a, -b, -c`, and once moved on every one of them is used.
  - Otherwise, at the cursor, if the letters start a long name, that long switch is suggested (`-verb` suggests `--verbose`).
  - Otherwise it is the parser's error: an unknown switch, or a non-bool switch in a group (P15).
- **R14.** `-x|` lists the switches by short name, with an exact match first. Accepting inserts the short name.
- **R15.** A switch name moved on:
  - An unknown name is the parser's error.
  - A known name is used: it is no longer suggested, and subcommands are no longer allowed (R19).
- **R16.** `--sw |`: a bool switch reads no value, so completion moves on to the next argument. Any other switch shows its value hint and suggested values.
- **R17.** An invalid switch value: once moved on with all its tokens, it is the parser's error. At the cursor, the hint is marked `invalid` once all its tokens are typed.
- **R18.** A valid switch value is used once moved on. At the cursor, the switch's suggested values are listed with its hint.

A switch reads its value from the tokens after it greedily, as the parser does.

## Ordered arguments

Ordered arguments are subcommands, positionals, optionals, and the variadic.

- **R19.** Subcommands are only allowed before any argument, switches included. An exact subcommand name switches to that subcommand's arguments.
- **R20.** The next ordered argument is the first positional not filled, otherwise the first optional not filled. A bool needs an explicit `true` or `false` here.
- **R21.** A valid value is used once moved on. At the cursor, its suggested values are listed with its hint, plus subcommand names while subcommands are still allowed.
- **R22.** An optional whose value does not fit, on a command with a variadic that accepts the value: the value goes to the variadic and the remaining optionals are skipped, as in the parser. Otherwise, an invalid value moved on is the parser's error, and at the cursor it is marked `invalid`.
- **R23.** A value with too few tokens shows its hint with progress (P13) and no error.
- **R24.** Each variadic value works the same way as R21 to R23.
- **R25.** With no ordered argument left, only a subcommand name can follow. If none matches, it is the parser's `Unexpected token(s)` error (fixes B3).
- **R26.** Predicting the next token (R2): subcommands, if still allowed, together with the next positional's values and hint. With every positional filled, the line is ready (P3).

## Suggested values and ranking

- **R27.** *Replaced by P12.* Originally, only `string` values with whitespace were quoted.
- **R28.** Suggested values are filtered by the text typed at the cursor, without its quotes.
- **R29.** *Replaced by P10.* Originally, rows read `argName: value`.
- **R30.** *Replaced by P9.* Originally, a score from prefix, trigram and Levenshtein matching, with a 0.1 threshold and the top 5 shown.
- **R31.** *Replaced by P4.* Originally, a hint or error was the only row shown.

See also the suggestion function rules below.

---

## Suggestion functions

These cover the values that come from a `[Suggest]` function or are produced automatically.

- A `[Suggest]` function is static. It takes no parameters or one `SuggestionContext`, and returns `IEnumerable<string>` or `IEnumerable<Suggestion>`. It is looked up once per argument.
- Without `[Suggest]`, an enum argument suggests its names, and a bool positional or optional suggests `true` and `false`. A bool switch reads no value (R16), so it suggests none.
- `SuggestionContext.TryGet<T>` returns an argument's value only once it is fully typed and moved on, converted by its type adapter. A variadic gives the elements typed before the one being completed, and returns false while there are none. An unknown name or a wrong type throws, and is reported by P14.
- Suggested values apply only to the first token of a value that takes several tokens.
- Results are cached. The function runs again only if a value it read through `TryGet` or `Current` changed, or if it is asked about a different argument. The cache is dropped when a completion asks for no suggestions, so leaving the value and coming back runs the function again.
- No command is ever constructed for autocomplete, so constructors and field initializers never run, and default values are never shown.
