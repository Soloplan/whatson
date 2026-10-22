# Agent guide

whatsON is a C# Windows desktop application for monitoring Jenkins and
CruiseControl.NET builds. Keep changes focused and follow the existing patterns.

## Repository layout

- `src/Soloplan.WhatsON.slnx`: the solution, including all four test projects.
- `src/Soloplan.WhatsON/`: shared models, configuration, plugin composition,
  serialization, and observation scheduling; targets .NET 10.
- `src/Soloplan.WhatsON.Jenkins/`: Jenkins API and connector; targets .NET 10.
- `src/Soloplan.WhatsON.CruiseControl/`: CruiseControl.NET connector; targets .NET 10.
- `src/Soloplan.WhatsON.GUI/`: WPF application, configuration UI, wizard, tray,
  and notifications; targets .NET 10 for Windows.
- `src/Soloplan.WhatsON.GUI.Common/`: shared WPF controls and view models.
- `src/Soloplan.WhatsON.Package/`: standalone Visual Studio MSIX packaging project,
  self-contained x64 publish profile integration, and App Installer update feeds.
- `src/Soloplan.WhatsON.Jenkins.GUI/` and
  `src/Soloplan.WhatsON.CruiseControl.GUI/`: connector presentation plugins.
- `src/Soloplan.WhatsON.Jenkins.Tests/`: NUnit tests with fake API helpers.
- `src/Soloplan.WhatsON.Tests/`: configuration persistence, notification settings,
  connector history, and scheduler tests.
- `src/Soloplan.WhatsON.CruiseControl.Tests/`: report parsing, status mapping, cache, and history tests.
- `src/Soloplan.WhatsON.GUI/Tests/`: NUnit GUI tests.
- `src/Directory.Build.props`, `src/Directory.Build.targets`, and
  `src/Directory.Packages.props`: shared SDK project settings and package versions.
- `config/`: shared StyleCop settings; assembly metadata is in `src/Directory.Build.props`.
- `Jenkinsfile`: CI build, NUnit execution, and artifact publishing.

## Build and test

Run commands from the repository root on Windows with the .NET 10 SDK.
`global.json` selects the .NET 10 SDK and permits newer feature bands within
that version. Restore requires access to the referenced NuGet packages.
The Jenkins Windows build agent needs the same SDK.

Jenkins uses `SoloPipeline@1.7` and tests the full solution with the `Integration`
and `UI` NUnit categories excluded in the Windows build container. Categorize
tests that need external services as `Integration` and desktop tests as `UI`.
The GUI test assembly has an inherited `UI` category. Snapshot publishing
includes `master` as beta and `develop` as alpha.

Versioning uses the SDK `Version` property, set in the Jenkins environment as
`1.yyMM.build-channel` (UTC build start month), or the complete `TAG_NAME`.
Develop and feature branches use alpha; master uses beta; tags use stable.
`Directory.Build.props` removes an optional leading `v` from the version.
Release tags must otherwise be valid SDK versions, e.g. `v1.2610.42`.
Local builds default to `1.yyMM.0-local`.
Keep the About dialog on `AssemblyInformationalVersion` to display the suffix.

MSIX packaging is separate from the normal solution build because it requires
Visual Studio's Desktop Bridge/MSIX targets. Follow
`src/Soloplan.WhatsON.Package/README.md`; pass `RuntimeIdentifier=win-x64` during
restore and build, and verify the copied manifest and feed when changing packaging.
Jenkins signs master/develop/tag packages using Azure signing
and archives them. GitHub publishing is manual; do not add automatic uploads.

```powershell
dotnet build src/Soloplan.WhatsON.slnx -c Debug
dotnet test src/Soloplan.WhatsON.slnx -c Debug --no-build
```

Use `-c Release` for the configuration used by CI. SDK outputs are per project:
`bin/<Configuration>/<TargetFramework>/`. The application entry point is
`src/Soloplan.WhatsON.GUI/bin/Debug/net10.0-windows10.0.19041.0/Soloplan.WhatsON.GUI.exe`.
All four test projects use the .NET test SDK and NUnit adapter; confirm discovery
and executed test counts. A single test project can be passed to `dotnet test`
instead of the solution.

Publish the application and its built-in connector/presentation plugins into
an empty output directory with:

```powershell
dotnet publish src/Soloplan.WhatsON.GUI/Soloplan.WhatsON.GUI.csproj -c Release -o src/bin/Release
```

Distribute the entire publish directory, including `.deps.json` and
`.runtimeconfig.json`. This framework-dependent build requires the .NET 10
Desktop Runtime on Windows. External plugins must also be rebuilt for .NET 10.

For a core-only change, a narrower build is:

```powershell
dotnet build src/Soloplan.WhatsON/Soloplan.WhatsON.csproj -c Debug
```

Build affected projects and run relevant tests for code changes. Add a small
regression test for changed behavior, reusing the existing NUnit/fake patterns.
Use `WireMock.Net.Minimal` for HTTP test servers; do not use TCP listeners or
hand-written HTTP responders in unit tests.
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
- Nullable reference types are enabled in every project and nullable diagnostics
  are build errors. Represent missing values with nullable types, initialize
  required state, and validate external data; do not suppress checks with `!`
  or `#nullable disable`.
- SDK projects include C# and WPF pages automatically. The main GUI excludes
  its nested `Tests` directory; plugin data templates remain raw WPF resources.
  Preserve those exceptions when changing project items.
- Edit resource/settings inputs rather than generated `*.Designer.cs` files.
  Keep saved configuration compatible unless the task explicitly changes its format.
- UI property notifications use PropertyChanged.Fody, configured in `src/FodyWeavers.xml`.
  Declare notification roots as partial classes implementing `INotifyPropertyChanged`;
  Fody generates the event and invokers. Use auto-properties and `OnChangedMethod`
  callbacks for domain side effects instead of handwritten notification setters.
  Refresh collection-derived values through the generated invoker when necessary.
  WPF dependency properties and domain events keep their native mechanisms.
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
