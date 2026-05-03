# Bossy

A developer console for Unity. Run commands in the editor, in builds, and at runtime with a consistent authoring API.

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Startup & Configuration](#startup--configuration)
- [Authoring Commands](#authoring-commands)
    - [Command Attribute](#command-attribute)
    - [Arguments](#arguments)
    - [Positional](#positional)
    - [Optional](#optional)
    - [Switch](#switch)
    - [Variadic](#variadic)
    - [CommandContext](#commandcontext)
    - [Return Values](#return-values)
- [Async Commands](#async-commands)
- [Built-in Commands](#built-in-commands)
- [Validation](#validation)
- [Examples](#examples)

---

## Installation

1. In unity, open the pacakge manager via `Window -> Pacakge Management -> Pacakge Manager`.
2. "https://github.com/RadTadDev/bossy.git?path=/Assets/Bossy#main"
> TODO: Add UPM installation instructions / Asset Store link.

---

## Quick Start

**1. Configure Bossy at startup.**

Create a `BossyConfig` asset and assign it via the startup entry point, or build one in code:

```csharp
// TODO: confirm config builder API
var config = BossyConfig.Create()
    .WithDiscovery(/* assembly or type list */)
    .Build();
```

**2. Write a command.**

```csharp
using Bossy;

[Command("greet", "Prints a greeting.")]
public class GreetCommand : SimpleCommand
{
    [Positional("The name to greet.")]
    private string _name;

    [Switch('l', "Print in lowercase.")]
    private bool _lowercase;

    protected override CommandStatus Execute(SimpleContext ctx)
    {
        var message = $"Hello, {_name}!";
        ctx.Write(_lowercase ? message.ToLower() : message);
        return CommandStatus.Ok;
    }
}
```

**3. Open the console and run it.**

```
> greet World
Hello, World!

> greet World -l
hello, world!
```

---

## Startup & Configuration

> TODO: Document the ScriptableObject-based startup entry point, assembly discovery configuration, and any host-specific setup (editor window vs. runtime overlay).

---

## Authoring Commands

All commands inherit from either `SimpleCommand` (synchronous) or `Command` (async). Arguments are declared as private fields with attributes. The parser discovers and populates them automatically before `Execute` is called.

### Command Attribute

```csharp
[Command("name", "A short description of what this command does.")]
public class MyCommand : SimpleCommand { ... }
```

| Parameter | Description |
|---|---|
| `name` | The name used to invoke the command in the console. Must be unique. |
| `description` | Shown in help output and the registry. Should be a single concise sentence. |

Commands can be nested under a parent command using subcommand syntax:

```csharp
// TODO: confirm subcommand declaration API
```

---

### Arguments

Arguments are private fields decorated with one of four attributes. The parser resolves them in this order: positionals (by declared order), optionals, switches, then the variadic.

---

### Positional

A required argument resolved by position.

```csharp
[Positional("The target file path.")]
private string _path;
```

- Multiple positionals are filled left to right in declaration order.
- A positional after a variadic is not permitted.

---

### Optional

A named argument that may be omitted. Uses its default field value if not supplied.

```csharp
[Optional("The output directory. Defaults to the project root.")]
private string _output = "Assets/";
```

> TODO: confirm optional invocation syntax (e.g. `--output ./Builds` vs `-output`).

---

### Switch

A boolean flag toggled by a short character name.

```csharp
[Switch('v', "Enable verbose output.")]
private bool _verbose;
```

Invoked with a `-` prefix:

```
> mycommand -v
```

Multiple switches can be combined:

```
> mycommand -vr
```

> TODO: confirm whether combined switches are supported.

---

### Variadic

Captures zero or more remaining arguments as an array. Only one variadic is permitted per command, and it must be the last argument. The array is never null — it is empty if no arguments were supplied.

```csharp
[Variadic("The files to process.")]
private string[] _files;
```

```
> process file1.cs file2.cs file3.cs
```

---

### CommandContext

`CommandContext` (exposed as `SimpleContext` in synchronous commands) is the sole API surface available during execution.

| Member | Description |
|---|---|
| `ctx.Write(string)` | Writes a line to the console output. |
| `ctx.WriteError(string)` | Writes a formatted error line. |
| `ctx.Bossy` | Access to the Bossy instance, including the schema registry. |

#### Formatting output

The `Format` static class provides rich text helpers:

```csharp
ctx.Write(Format.Bold("Important"));
ctx.Write(Format.Italics("subtle note"));
ctx.Write(Format.Color("warning", Format.Yellow));
```

To align a list of items into two columns:

```csharp
ctx.Write(Format.Align(
    items,
    item => item.Name,
    item => item.Description,
    Format.LightBlue
));
```

---

### Return Values

`Execute` returns a `CommandStatus`:

| Value | Meaning |
|---|---|
| `CommandStatus.Ok` | Command completed successfully. |
| `CommandStatus.Error` | Command failed. Use `ctx.WriteError` before returning. |

---

## Async Commands

For commands that need to perform async work, inherit from `Command` and implement `ExecuteAsync`:

```csharp
[Command("fetch", "Fetches data from a remote endpoint.")]
public class FetchCommand : Command
{
    [Positional("The URL to fetch.")]
    private string _url;

    public override async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
    {
        var result = await SomeAsyncOperation(_url);
        ctx.Write(result);
        return CommandStatus.Ok;
    }
}
```

Async commands run cooperatively on the main thread via `async`/`await`. Long-running operations should respect cancellation:

```csharp
// TODO: document CancellationToken access via CommandContext if exposed.
```

---

## Built-in Commands

Bossy ships with a small set of built-in commands:

| Command | Description |
|---|---|
| `help <command>` | Displays usage, arguments, and subcommands for a command. |
| `reg [parent]` | Lists all registered commands, optionally filtered to a parent. |
| `bossy invalid` | Lists commands that failed schema validation at startup. |

### help

```
help <command> [subcommands...] [-v] [-r]
```

| Flag | Description |
|---|---|
| `-v` | Show argument types, field names, and validation attributes. |
| `-r` | Recursively display all subcommands. |

---

## Validation

Bossy validates all command schemas when the registry is built at startup. Commands with problems are flagged but remain in the registry at a degraded state — they are not available for execution.

Run `bossy invalid` to inspect any problems:

```
> bossy invalid
Command 'clears' has the following problems:
    Warning: Description is null or empty

==== Summary ====
-clears: 0 error(s) and 1 warning(s)
Commands with errors are not available for execution.
```

The only unrecoverable startup error is duplicate command names, which prevents Bossy from initializing entirely.

---

## Examples

### A command with all argument types

```csharp
[Command("deploy", "Deploys build artifacts to a target.")]
public class DeployCommand : SimpleCommand
{
    [Positional("The build target name.")]
    private string _target;

    [Optional("Override the output path.")]
    private string _output = "Builds/";

    [Switch('d', "Perform a dry run without writing files.")]
    private bool _dryRun;

    [Switch('v', "Enable verbose logging.")]
    private bool _verbose;

    [Variadic("Additional tags to apply to this deployment.")]
    private string[] _tags;

    protected override CommandStatus Execute(SimpleContext ctx)
    {
        if (_dryRun)
        {
            ctx.Write(Format.Color("[Dry Run] No files will be written.", Format.Yellow));
        }

        ctx.Write(Format.Align(
            new (string, string)[]
            {
                ("Target",  _target),
                ("Output",  _output),
                ("Tags",    string.Join(", ", _tags)),
            },
            t => t.Item1,
            t => t.Item2,
            Format.LightBlue
        ));

        return CommandStatus.Ok;
    }
}
```

### Reading from the schema registry

```csharp
[Command("describe", "Prints the description of a registered command.")]
public class DescribeCommand : SimpleCommand
{
    [Positional("The command name.")]
    private string _command;

    protected override CommandStatus Execute(SimpleContext ctx)
    {
        var status = ctx.Bossy.SchemaRegistry.TryResolveSchema(_command, out var schema);

        if (status is not SchemaQueryStatus.Found)
        {
            ctx.WriteError($"Unknown command: '{_command}'");
            return CommandStatus.Error;
        }

        ctx.Write(schema.Description);
        return CommandStatus.Ok;
    }
}
```