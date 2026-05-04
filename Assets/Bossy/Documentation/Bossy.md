# Bossy

A developer console for Unity. Run commands in the editor, in builds, and at runtime with a consistent authoring API.

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Bootstrapping](#bootstrapping)
- [Authoring Commands](#authoring-commands)
    - [Declaring a Command Attribute](#declaring-a-command)
    - [Arguments](#arguments)
    - [Positional](#positional)
    - [Optional](#optional)
    - [Switch](#switch)
    - [Variadic](#variadic)
    - [Command Context](#command-context)
    - [Return Values](#return-values)
    - [Argument Validation](#argument-validation)
- [Async Commands](#async-commands)
- [Validation](#validation)
- [Piping](#piping)
- [Type Adapting](#type-adapting)
- [Custom Command UI](#custom-command-ui)

---

## Installation

1. In unity, open the pacakge manager via `Window -> Pacakge Management -> Pacakge Manager`.
2. Click the `+` icon in the top left and select `Install package from git URL...`
3. Input the following line: `https://github.com/RadTadDev/bossy.git?path=/Assets/Bossy#main`

---

## Quick Start

**1. Create the Bossy runtime.**

Create the following script. For more information about proper bootstrapping, see [Bootstrapping](#bootstrapping)

```csharp
using Bossy;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class BossyStartup
{
    private static BossyConsole _bossy;
    static BossyStartup()
    {
        EditorApplication.delayCall += Start;
    }

    private static void Start()
    {
        EditorApplication.delayCall -= Start;
        _bossy = BossyBuilder
            .GetCommands()
            .Automatically()
            .Build();
    }
}

#else

using UnityEngine;

public class BossyStartup
{
    private static BossyConsole _bossy;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Start()
    {
        _bossy = BossyBuilder
            .GetCommands()
            .Automatically()
            .Build();
    }
}
#endif  
```

**2. Press `/` to open the console.**

**3. Enter the command `help` to get information**

**4. Create your own command by making a C# class like this**
```csharp
using Bossy.Command;
using Bossy.Execution;

[Command("greet", "Prints a greeting.")]
public class GreetCommand : SimpleCommand
{
    [Positional(0, "The name to greet.")]
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

**5. Open the console and run it.**

```
> greet World
Hello, World!

> greet World -l
hello, world!
```

---

## Bootstrapping

The script above is good for unconditionally including Bossy in the editor, runtime, and builds. However, you may want to be more selective. You can use `#if DEVELOPMENT_BUILD` to only inlcude the system in development builds, and `#if UNITY_EDITOR` to detect whether you are in the editor or not.

You also can have more fine grained control over the configuration. The above example used `BossyBuilder.GetCommands.Automatically()` to search all loaded assemblies for commands. You can also use `.FromTypes()`, `.InAssembly()` and `.InAssemblies()` for more control over how to discover commands. 

Before calling `.Build()`, you can also pass in custom type adapters (see [Type Adapting](#type-adapting)) using the `.WithAdapter()` call.

---

## Authoring Commands

All commands are classes that inherit from either `SimpleCommand` (for synchronous) or `ICommand` (for async). Arguments are declared as fields with attributes. The parser discovers and populates them automatically before `Execute` is called so you never have to parse manually.

Most commands that do an immediate task should be `SimpleCommand`'s. Any command that wants to read input from a source or do a prolonged action must be an `ICommand`. Note that async `ICommand`'s still run on the main thread, so busy loops and blocking operations should be avoided. Always prefer the `async` variants of methods in such commands.

### Declaring a Command

In addition to inheriting from one of the two bases, you must add the following Command attribute:

```csharp
[Command("name", "A short description of what this command does.")]
public class MyCommand : SimpleCommand { ... }
```

| Parameter     | Description                                                                                  |
|---------------|----------------------------------------------------------------------------------------------|
| `name`        | The name used to invoke the command in the console. Must be unique compared to its siblings. |
| `description` | Shown in help output and the registry. Should be a single concise sentence.                  |

Commands can be nested under a parent command using subcommand syntax:

```csharp
[Command("name", "A short description of what this command does.", typeof(ParentCommand))]
public class MyCommand : SimpleCommand { ... }
```
Note that we include the type of the parent command as an argument in the attribute of the subcommand. Subcommands who have different parents will never have naming collisions with each other, so `cmd1 sub` and `cmd2 sub` are both valid sub commands.

---

### Arguments

Arguments are fields decorated with one of four attributes: positionals, optionals, switches, and variadic.

---

### Positional

A required argument resolved by position.



```csharp
[Positional(0, "The target file path.")]
private string _path;
```

| Parameter     | Description                                                                                                                           |
|---------------|---------------------------------------------------------------------------------------------------------------------------------------|
| `index`       | Unique index starting from 0 and counting up. This declares the order positionals are expected in.                                    |
| `description` | Shown in help output and the registry. Should be a single concise sentence.                                                           |
| `overrideName`| (**Optional**) By default, the positional's name is the name of the declared variable. Use this to override the default name.         | 

- Positionals will be parsed from the command line in the order dictated by the indices.
- Positionals are greedily taken, so they will be prefered over `Optional` and `Variadic` arguments when ambiguous.

The following command is an example using positionals.
```csharp
[Command("mycommand", "My command is for demonstration purposes.")]
public class MyCommand : SimpleCommand
{
    [Positional(0, "The number of times to repeat.")] 
    private int repeat;
    
    [Positional(1, "The phrase to repeat.")]
    private string phrase;
    
    ...
}
```
An example execution:

```csharp
> mycommand 3 hello
hello
hello
hello
```
---

### Optional

A named argument that may be omitted. Uses its default field value if not supplied.

```csharp
[Optional("The output directory. Defaults to the project root.")]
private string _output = "Assets/";
```

| Parameter     | Description                                                                                                                      |
|---------------|----------------------------------------------------------------------------------------------------------------------------------|
| `index`       | Unique index starting from 0 and counting up. This declares the order optionals are expected in. Distince count from positionals |
| `description` | Shown in help output and the registry. Should be a single concise sentence.                                                      |
| `overrideName`| (**Optional**) By default, the optional's name is the name of the declared variable. Use this to override the default name.      | 

- `Optionals` will be considered before `Variadic` arguments.

The following command is an example using positionals.
```csharp
[Command("mycommand", "My command is for demonstration purposes.")]
public class MyCommand : SimpleCommand
{
    [Optional(0, "The path to search")]
    private string root;
    
    ...
}
```
An example execution:

```
> mycommand
searching in Assets/ ...

 - or -
 
> mycommand Assets/Scripts
searching in Assets/Scripts ...
```

---

### Switch

A dashed parameter and value pair. Boolean switches are implicitly true if included and do not require a value

```csharp
[Switch('v', "Enable verbose output.")]
private bool _verbose;
```
| Parameter      | Description                                                                                                               |
|----------------|---------------------------------------------------------------------------------------------------------------------------|
| `shortname`    | A single character short cut. Must be unique within this command.                                                         |
| `description`  | Shown in help output and the registry. Should be a single concise sentence.                                               |
| `overrideName` | (**Optional**) By default, the switch's name is the name of the declared variable. Use this to override the default name. | 


Invoked with a `-` prefix:

```
> mycommand -v

 - or -
> mycommand --verbose
```

Multiple boolean switches can be combined:

```
> mycommand -vr
```

Non boolean switches require values:

```csharp
[Switch('i', "Increment value.")]
private float _increment;
```

```
> mycommand -i 5

 - or -

> mycommand --increment 4.3
```

- Note that in the case of all names, leading underscores are ignored without requireing the override name.
- Switches may appear anywhere except for after variadics.

---

### Variadic

Captures zero or more remaining arguments as an array. Only one variadic is permitted per command. The array is never null — it is empty if no arguments were supplied.

```csharp
[Variadic("The files to process.")]
private string[] _files;
```

```
> process file1.cs file2.cs file3.cs
```

| Parameter     | Description                                                                                                                 |
|---------------|-----------------------------------------------------------------------------------------------------------------------------|
| `description` | Shown in help output and the registry. Should be a single concise sentence.                                                 |
| `overrideName`| (**Optional**) By default, the variadic's name is the name of the declared variable. Use this to override the default name. | 


---

### Command Context

`CommandContext` (exposed as `SimpleContext` in synchronous commands) is the sole API surface available during execution.

| Member                                       | Description                                                                                                |
|----------------------------------------------|------------------------------------------------------------------------------------------------------------|
| `ctx.Write(string)`                          | Writes a line to the console output.                                                                       |
| `ctx.WriteError(string)`                     | Writes a formatted error line.                                                                             |
| `ctx.WriteWarning(string)`                   | Writes a formatted warning line.                                                                           |
| `ctx.NewLine()`                              | Writes a new line.                                                                                         |
| `ctx.ReadAsync<T>()`                         | Reads a typed object from the input stream.                                                                |
| `ctx.ReadAllAsync<T>()`                      | Reads typed objects from the input stream as long as its open. See below.                                  |
| `ctx.CloseOutputStream()`                    | Indicates that no more output is coming. This breaks `ctx.ReadAllAsync<T>()` loops.                        |
| `ctx.Execute(string, ObservablePipe = null)` | Executes another command. Use the observable pipe to get callbacks for the new command's reads and writes. |
| `ctx.Delay(TimeSpace)`                       | Delays for some time.                                                                                      |
| `ctx.Yield()`                                | Yeilds execution contorl. Useful to break tight loops without a delay.                                     |
| `ctx.Bossy`                                  | Access to the Bossy instance, including the schema registry.                                               |
| `ctx.CancelationToken`                       | Gets the command's cancellation token.                                                                     |
| `ctx.Capabilities`                           | Gets the front end Capabilities. See below.                                                                |

Note: When using `ctx.ReadAllAsync<T>()`, you should should use it in a loop like so:
```csharp
await foreach (T obj in ctx.ReadAllAsync<T>())
{
    // Use obj
}
```

Note: You can test for specific capabilities via the following:
```csharp
if (ctx.Capabilities is IClearable clearable)
{
    
}
```

If you want to add your own capability to an existing front end, the CLI for example, do the following:
```csharp
// Make your capability interface
public interface IMyNewCapability
{
    public void DoSomething();
}

// Use the partial keyword to add it to the front end declaration
public partial class CliUserInterfaceView : IMyNewCapabilitiy { }

// And you can now test for it:
if (ctx.Capabilities is IMyNewCapability cap)
{
    cap.DoSomething();
}
```

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

Some `Format` calls have overloads to take in the context and print immediately.

---

### Return Values

`Execute` returns a `CommandStatus`:

| Value                 | Meaning                                                                    |
|-----------------------|----------------------------------------------------------------------------|
| `CommandStatus.Ok`    | Command completed successfully.                                            |
| `CommandStatus.Error` | Command failed. Use `ctx.WriteError` before returning to display an error. |


### Argument Validation

In addition to having typed arguments, you can also apply validators to further reduce the need to check values at runtime. For example, the range attribute can go on a numeric value like so:

```csharp
[Range(0, 5)]
[Positional(0, "The number of times to repeat.")]
private int repeat;
```

To make your own validators, inherit from the `ArgumentValidationAttribute` class. Below is an example of the `Range` attribute:

```csharp
[AttributeUsage(AttributeTargets.Field)]
    public class RangeAttribute : ArgumentValidationAttribute
    {
        public readonly float Min;
        public readonly float Max;
        
        public RangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public override ArgumentValidationResult Validate(object value)
        {
            if (!float.TryParse(value?.ToString(), out var num))
            {
                return ArgumentValidationResult.Fail($"Input type '{value?.GetType().GetFriendlyName()}' is not numeric.");
            }

            if (num >= Min && num <= Max)
            {
                return ArgumentValidationResult.Pass();
            }
            
            return ArgumentValidationResult.Fail($"{num} is outside the range [{Min}, {Max}]");
        }
    }
```

---

## Async Commands

For commands that need to perform async work, inherit from `ICommand` and implement `ExecuteAsync`:

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

---

## Piping
Commands can also pipe data like in bash.

```
> cmd1 | cmd2 | cmd3
```

The example above connects the output of `cmd1` to the input of `cmd2` and the output of `cmd2` to the input of `cmd3`. All commands can be piped and you don't need to consider piping while creating commands. Simple reads and writes are retargeted to the correct commands from the front end when you execute using pipes.


## Validation

Bossy validates all command schemas when the registry is built at startup. Commands with problems are flagged but remain in the registry at a degraded state — they are not available for execution.

Run `bossy invalid` to inspect any problems:

The only unrecoverable startup error is duplicate command names, which prevents Bossy from initializing entirely.

## Type Adapting

In order to allow command line string input to be converted to type arguments, a registry of `ITypeAdapter`'s is kept. Most common types are provided by default, but you can create your own as well.

To do this, make a class that inherits from `BaseTypeAdapter<T>` where `T` is the type that this adapter converts for. Then, register it at start up as described in [Boostrapping](#bootstrapping).

Here is an example of how the int adapter works:

```csharp
public class IntAdapter : BaseTypeAdapter<int>
{
    protected override TypeAdapterResult TryConvertToType(TokenStream stream, out int output)
    {
        if (stream.TryConsume(out var token) && int.TryParse(token, out output))
            return TypeAdapterResult.Pass();

        output = 0;
        return TypeAdapterResult.Fail($"Expected int, got \"{token ?? "nothing"}\"");
    }
}
```

Note that any tokes used must be consumed from the stream. If parsing fails, return a helpful error message.

- For Enum types, you don't need to make a new adapter, just include a call at startup like so `.WithAdapter<EnumAdapter<MyEnum>>()`.

---

## Custom Command UI

Commands can declare custom execution UI. To do this, have your command implement the `IContentView` interface:

```csharp
[Command("mycommand", "Description.")]
public class MyCommand : ICommand, IContentView
{
    public VisualElement CreateView() { }
        
    public void SetSignaler(Signaler signaler) { }

    public void OnFocus() { }

    public void OnDefocus() { }
}
```
You must implement the methods shown above. Most can be no-ops unless needed. `CreateView()` must return the root of the command's UI using the UI Toolkit API. If you prefer you can use the `ContentViewUtility.GetRootFromUxml()` to load a UI document you created using the UI Toolkit Builder.

The signaler is used to send signals that would otherwise be absorbed by your UI. An example is needing to send the toggle command while an input field is focused. You can use the signaler to respect the toggle input instead of typing a `/` in the input bar.