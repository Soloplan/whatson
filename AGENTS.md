# Agent guide

whatsON is a C# Windows desktop application for monitoring Jenkins and
CruiseControl.NET builds. Keep changes focused and follow the existing patterns.

## Repository layout

- `src/Soloplan.WhatsON.sln`: the solution, including both test projects.
- `src/Soloplan.WhatsON/`: shared models, configuration, plugin composition,
  serialization, and observation scheduling; targets .NET Standard 2.0.
- `src/Soloplan.WhatsON.Jenkins/`: Jenkins API and connector; targets .NET Standard 2.0.
- `src/Soloplan.WhatsON.CruiseControl/`: CruiseControl.NET connector; targets .NET Framework 4.7.2.
- `src/Soloplan.WhatsON.GUI/`: WPF application, configuration UI, wizard, tray,
  and notifications; targets .NET Framework 4.7.2.
- `src/Soloplan.WhatsON.GUI.Common/`: shared WPF controls and view models.
- `src/Soloplan.WhatsON.Jenkins.GUI/` and
  `src/Soloplan.WhatsON.CruiseControl.GUI/`: connector presentation plugins.
- `src/Soloplan.WhatsON.Jenkins.Tests/`: NUnit tests with fake API helpers and RhinoMocks.
- `src/Soloplan.WhatsON.GUI/Tests/`: NUnit GUI tests.
- `config/`: shared MSBuild targets, assembly metadata, and StyleCop settings.
- `Jenkinsfile`: CI build, NUnit execution, and artifact publishing.

## Build and test

Run commands from the repository root in a Visual Studio Developer PowerShell.
The full solution requires Windows, Visual Studio/MSBuild with .NET desktop
build tools, the .NET Framework 4.7.2 targeting pack, and a .NET SDK for the
SDK-style projects. Restore requires access to the referenced NuGet packages.
Use Visual Studio MSBuild for the full WPF solution.

```powershell
msbuild src/Soloplan.WhatsON.sln -restore -m -p:Configuration=Debug '-p:Platform=Any CPU'
```

Use `Configuration=Release` for the configuration used by CI. The application
and plugin projects share `src/bin/<Configuration>/`; the entry point is
`Soloplan.WhatsON.GUI.exe`. Test projects use their own `bin/<Configuration>/`.

Run the affected NUnit assembly using Visual Studio Test Explorer or an
installed NUnit 3 console runner. For example, after the Debug build:

```powershell
nunit3-console src/Soloplan.WhatsON.Jenkins.Tests/bin/Debug/Soloplan.WhatsON.Jenkins.Tests.dll
nunit3-console src/Soloplan.WhatsON.GUI/Tests/bin/Debug/Soloplan.WhatsON.GUI.Tests.dll
```

The console runner is an external prerequisite, not bundled in this repository.
Both test projects reference NUnit and its adapter but do not reference
`Microsoft.NET.Test.Sdk`; do not treat a successful `dotnet test` exit alone as
evidence that tests ran. Confirm discovery and executed test counts.

For a core-only change, a narrower build is:

```powershell
dotnet build src/Soloplan.WhatsON/Soloplan.WhatsON.csproj -c Debug
```

Build affected projects and run relevant tests for code changes. Add a small
regression test for changed behavior, reusing the existing NUnit/fake patterns.
For WPF changes, also check the affected window, bindings, and interaction when
a desktop session is available. Report missing prerequisites and checks that
could not run; do not change frameworks or packages just to bypass them.

## Editing and coordination

- Inspect `git status` first and preserve unrelated or pre-existing edits.
- Use the Graft workflow below before searching or opening source. Trace
  callers before changing shared contracts or making a multi-file refactor.
- Keep connector logic in its connector project and presentation in the GUI
  projects. Check both connectors and their presentation plugins when changing
  a shared model or interface.
- Follow `config/stylecop.json`, `config/whatson.ruleset`, and neighboring code:
  two-space indentation, spaces instead of tabs, existing naming and brace style.
- Some WPF projects use explicit `Compile` and `Page` entries in their project
  files; update those entries when adding, moving, or deleting source or XAML.
- Edit resource/settings inputs rather than generated `*.Designer.cs` files.
  Keep saved configuration compatible unless the task explicitly changes its format.
- Keep credentials and live server details out of source, fixtures, and logs.
  Use fake responses for tests; do not trigger real builds to verify a connector.
- When delegating work, give each agent a distinct file or responsibility scope.
  Agents share the checkout: do not revert another agent's edits. The coordinating
  agent reviews the combined diff and runs integration checks after work joins.
- Before finishing, inspect the diff for accidental changes and report what
  changed, what was verified, and any remaining limitation.

<!-- graft:start -->
## Graft — repo context graph

This repo is indexed in `graft/`: small linked markdown nodes that explain each
system and carry exact file:line spans, kept in sync with the code through git.

For ANY task here — understanding how something works, finding where code lives,
or scoping a change — get context from the graph before grepping or opening
source files. Re-ask freely (it's cheap) and reuse literal identifiers you
already have (symbol, error string, file name) as the query. New to this repo?
Run `graft map` first — a token-budgeted orientation (dir clusters, hubs,
hotspots), no LLM, no key.

- Run `graft ask "<your question>" --source` → ranked nodes with the relevant
  code spans inlined (each hit's ≤8-line crux by default; `--full` for whole
  definitions when the crux isn't enough). Match the tool to the task shape:
  for understanding or editing, the top node IS the answer — cite its
  `covers:` file:line spans and edit straight from `--source`. For
  exhaustive tasks ("every occurrence / every caller of this pattern"), ranked
  results are top-N, not complete — run `graft grep "<literal>"` instead
  (exhaustive over indexed files, grouped by enclosing symbol), falling back
  to raw `grep -rn` only for unindexed files.
- `graft skeleton <file>` → every definition's signature + span, ~10× cheaper
  than reading the file; use it to skim an API surface.
- `graft callers <symbol>` gives precomputed, exact edges — who calls this.
  Add `--direction out` for what it calls, or `--depth N` to walk
  transitively for the full blast radius. For structural questions, skip
  ranking and use this directly.
- Or browse: `graft/INDEX.md` lists every node; follow the links.
- Monorepos and folders of multiple repos rank fairly across sub-projects —
  hits carry `[scope/]` labels naming which one they're from. Narrow with
  `graft ask "<task>" --in <scope>/` once you know where you're working.

If a returned span is truncated ("+N more lines"), open the file at that exact
range before finalizing. Only open source files when a node genuinely lacks a
needed detail, and then at the exact file:line the node points to — never
re-read whole files.

After big code changes, refresh the graph with `graft build` (deterministic,
no API key, $0).
<!-- graft:end -->

If the `graft` executable is unavailable, use the connected Graft tools when
available: `graft_repo_map`, `graft_find_code`, `graft_find_all`,
`graft_file_api`, and `graft_trace_calls` correspond to the CLI operations above.
If neither interface is available, start at `graft/INDEX.md` and follow its
cards. If the graph is absent too, report that limitation and use targeted
`rg` searches and source reads. Do not invent graph results.
