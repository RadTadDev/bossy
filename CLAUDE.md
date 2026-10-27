# Bossy: Claude working guide

Bossy is a developer console (CLI) for Unity. It runs in the editor, in builds, and at runtime, and exposes commands through a consistent authoring API. The package itself lives in `Assets/Bossy` and is installed by other projects via git URL, so everything under that folder is shipped code.

## Goals

- Speed up Unity development workflows.
- Improve debugging.
- Give access to the whole codebase and Unity environment from the console without excessive integration into project code.

## Project layout

- `Assets/Bossy/Runtime`: the package. Subfolders: `Bossy` (builder, top-level console, lifecycle), `Command` (attributes, commands, command library), `Execution` (sessions, pipeline, IO), `Frontend` (parsing, autocomplete, hosts, views), `Schema` (reflection-based command registry and validation), `Settings`, `Utils`.
- `Assets/Bossy/Tests`: Editor tests (`Editor/`) and shared test utilities and mocks (`Utils/`).
- `Assets/Bossy/Documentation/README.md`: user-facing docs for the package. Update it when behavior users depend on changes.
- `Assets/Startup`: scratch scripts for the development project. Not part of the package; do not treat it as an example of good code.
- `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, and generated `.csproj`/`.sln` files: Unity-generated, git-ignored. Never edit them.

## Git and review workflow

- `main` is the base branch. Never commit directly to `main`.
- Create a short-lived branch for each change, named `feature/<short-kebab-name>` (or `fix/<short-kebab-name>` for bug fixes).
- Keep each branch to one logical change. Commit in small steps with clear messages.
- Open a PR against `main` for review. The user reviews and merges.
- Never push to, rebase, or force-push `main`.
- Pushing and opening PRs are outward-facing actions. Confirm with the user before the first push of a branch if they have not asked for it.

### PR descriptions

Each PR description must contain:

1. **Summary**: what changed and why, in a few sentences.
2. **Decisions made**: every architectural or medium-level choice I made without asking, with a one-line rationale each. The user wants to be informed of these even when they did not have to approve them.
3. **Test steps**: concrete steps the user can follow in the Unity Editor (and in a build or play mode, if relevant) to verify the change.
4. **Known gaps**: anything not done, not tested, or deliberately left out.

## Decision making

- The user makes the important architectural decisions: public API shape, new subsystems, new dependencies or packages, changes to how commands are authored or discovered, and changes to the permission model.
- For medium-level decisions (where a class responsibility goes, how to split files, how a feature is structured internally), make the call, follow the existing patterns, and list it under "Decisions made" in the PR.
- For small details (naming within a method, formatting), follow the surrounding code and do not call them out.
- If a change would alter public behavior or break existing command authors, stop and ask before implementing it.

## Code standards

- Clean, readable, and easy to maintain is the top priority. Prefer simple code over clever or heavily optimized code. Do not trade clarity for performance unless there is a measured need.
- Match the existing style of the file and folder you are working in: naming, brace style, XML doc comments, and region/section comments. Read neighboring files before writing new code.
- Public types and members in `Runtime` have XML doc comments. Keep that convention for new public API.
- Namespaces follow folder structure under `Bossy`. Note that most command library classes currently use `Bossy.Runtime.Command.Library`, while the command attributes and base types live in `Bossy.Command`. Do not spread the inconsistency further; new command classes should follow the existing command library convention until the user decides otherwise.
- Do not introduce new static mutable state or global singletons without calling it out as a decision.
- Do not add a dependency on a Unity package or third-party library without asking.
- Do not leave dead code, commented-out code, or `TODO`s without a reason. If you must leave a `TODO`, say why in the PR.
- Errors should be reported through the console (`Format.Error`, `ctx.WriteError`, and so on) with a clear message, not swallowed silently.
- Place public fields, accessors, functions, etc. above their private and protected counterparts. Classes should list events, then accessors, then fields, then methods and always public, then protected, then private within those. Dispose methods should be placed directly beneath the constructor, or at the top of the file if no constructor is given. The unity lifecycle methods for Awake, Start, OnEnable, OnDisable, and OnDestroy should be at the top of the file despite being private.

## Testing and verification

- Tests are NUnit tests run from the Unity Test Runner (EditMode). Add or update tests for behavior you change, especially parsing, schema, and command execution logic.
- You cannot always run the Unity Editor from the terminal. When you cannot run tests or the editor, say so explicitly in the PR and in your report. Do not claim something works when you only read the code.
- Where practical, keep logic in plain C# classes that can be tested without the Editor.

## Working with Unity

- Unity compiles only what is in the project. After adding a file, make sure its `.meta` file is created and committed with it.
- Avoid `UnityEditor` APIs in runtime code. Guard editor-only code with `#if UNITY_EDITOR` or put it in an Editor assembly.
- Be careful with `static` state across domain reloads and play mode. Explain any change to lifecycle or static state in the PR.
