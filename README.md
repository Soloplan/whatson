
<img src="./whatsON-logo.png" width="100" height="100">

# whatsON

A build monitoring tool written in C#.
  <br>
  <br>

<p align="center">
  <a href="https://github.com/Soloplan/whatson/issues/new?template=bug.md">Report bug</a>
  ·
  <a href="https://github.com/Soloplan/whatson/issues/new?template=feature.md&labels=feature">Request feature</a>
</p>


**whatsON** is a tool that provides the possibility to monitor the status and progress of build processes. It's designed to give developers or other collaborators a quick overview of the infrastructure's health and to notify them about issues in the build.

![alt text](https://raw.githubusercontent.com/Soloplan/whatson/master/WebContent/Screenshot.png)

## Table of contents

- [Features](#features)
- [Quick start](#quick-start)
- [Build from source](#build-from-source)
- [Feature requests and bugs](#feature-requests-and-bugs)
- [Plugins](#plugins)
- [Documentation](#documentation)
- [Contribiution](#contribution)
- [Working with coding agents](#working-with-coding-agents)
- [Creators](#creators)
- [License](#License)

## Features
- Monitor multiple builds status with a lot of additional information.
- Supports multiple build automation systems. By default Jenkins and CruiseControl.NET, but you can create your own plugin!
- Quick view for status of last 5 builds (Jenkins plugin)
- Easy configuration directly on main window.
- Wizard to add new projects.
- Configurable Windows notifications.
- System, light, and dark appearance, with automatic updates when Windows changes theme.

## Quick start

1. [Download the latest release.](https://github.com/Soloplan/whatson/releases/latest)
1. Save it to the location from which you will be using it.
1. Start the app from file *Soloplan.WhatsON.GUI.exe*
1. If you want the app to start during the system start, open the configuration (gear icon at the top, left corner) and set the option "Run when windows starts"
1. To add a new project build for monitoring, you can use the wizard (plus icon at the top, left corner).

Choose **Settings → General → Color theme** to follow Windows or select Light or
Dark. Changes preview immediately; select **Save changes** to keep them. Reset
or close Settings to return to the saved appearance.

## Build from source

Install the .NET 10 SDK on Windows, then run from the repository root:

```powershell
dotnet build src/Soloplan.WhatsON.slnx -c Release
dotnet test src/Soloplan.WhatsON.slnx -c Release --no-build
dotnet publish src/Soloplan.WhatsON.GUI/Soloplan.WhatsON.GUI.csproj -c Release --no-build -o src/bin/Release
```

Run `src/bin/Release/Soloplan.WhatsON.GUI.exe`. Copy the entire publish directory
when deploying, including the runtime configuration, dependencies, and plugins.
Use an empty publish directory to avoid mixing in old .NET Framework binaries.
These builds require the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
on Windows. Custom plugins built for .NET Framework must be rebuilt for .NET 10.
Project settings and package versions are shared in `src/Directory.Build.*`
and `src/Directory.Packages.props`.

Jenkins uses `SoloPipeline@1.7` on the Windows `dotnet-framework` build container.
It builds and tests the full solution, excluding the NUnit categories
`Integration` and `UI`. The GUI test assembly is categorized as `UI` because it
needs the Windows desktop runtime and includes desktop interactions. Run the
same container-compatible test selection locally with:

```powershell
dotnet test src/Soloplan.WhatsON.slnx -c Release --no-build --filter "TestCategory!=Integration&TestCategory!=UI"
```

The unfiltered command above still runs every test. CI records and archives
NUnit results and skips packaging and publishing after test failures.
Snapshots are published to `whatson/beta` (master) or `whatson/alpha` (develop).
Tags are published to their own release folder.

Versions are generated automatically as `1.yyMM.build-channel`, using the UTC
build start month and Jenkins build number: for example, `1.2610.42-alpha`
for develop and `1.2610.42-beta` for master. Feature branches use alpha.
The Jenkins environment uses the complete `TAG_NAME` as the release version:
`Directory.Build.props` strips an optional leading `v`, so tag `v1.2610.42`
produces `1.2610.42`. Tags must otherwise be valid SDK versions; prerelease
tags such as `v1.2610.42-rc.1` also work. Local
builds default to `1.yyMM.0-local`. All projects use the standard SDK `Version`
property; no versioning package or manual project-file updates are needed.
The About dialog shows the informational version, including the channel suffix
and Git commit metadata added by the SDK. Jenkins build numbers are per job.

Jenkins also produces a self-contained x64 MSIX and an App Installer update feed,
using Windows packaging tools and Azure signing. GitHub release
uploads are manual. See [MSIX packaging and updates](src/Soloplan.WhatsON.Package/README.md)
for build prerequisites, signing, channel URLs, and the upload procedure.

View-model property notifications use PropertyChanged.Fody with the shared
`src/FodyWeavers.xml` configuration. Notification roots are partial classes
implementing `INotifyPropertyChanged`; Fody generates the events and invokers.
Each presentation plugin assembly needs its own Fody and PropertyChanged.Fody
package references to weave its setters. The former `NotifyPropertyChanged`
base class has been removed; external plugins inheriting it must migrate and
rebuild.

## Languages

whatsON supports English and German. The default **System** language follows the
Windows display language when it is English or German, and falls back to English
for other languages. Existing configurations without a language preference also
use **System**. Choose **Settings → General → Language** to select System,
English, or Deutsch, save your changes, and restart whatsON. Explicit language
choices are preserved in configuration imports and exports.

Interface text is stored in `src/Soloplan.WhatsON/Localization/Strings.resx`
and `Strings.de.resx`. The .NET SDK generates the public `Strings` class during
every build; add or edit resources in the `.resx` files, not generated code.
Use `Strings.None` in C# and `{x:Static loc:Strings.None}` in WPF, where `loc`
references `clr-namespace:Soloplan.WhatsON.Localization;assembly=Soloplan.WhatsON`.
Use named methods such as `Localizer.DeleteNamedProject(projectName)` for messages
with arguments, and `Localizer.GetStateName(state)` for build states. Resource
properties and method arguments are checked by the compiler. Keep the same keys
and formatting placeholders in both translations.
English is the fallback when a culture has no translation. Project names, URLs,
server-provided descriptions, and diagnostic logs retain their original content.

Existing validation/tray resources and connector settings captions use the
`Properties/Resources*.resx` and `Properties/ConfigurationItems*.resx` files in
their owning projects. These also use standard .NET satellite resources.
The publish directory's `de` folder must be distributed with the application.
Localization tests check translation completeness, formatting placeholders,
fallback, persistence, and translated controls.

## Feature requests and bugs

Have a bug or a feature request please search for it in existing and closed issues. If your problem or idea is not addressed yet, please [open a new issue](https://github.com/Soloplan/whatson/issues/new).

## Plugins

The plugins in whatsON allows to monitor builds from various build servers.
It is possible to develop your own plugin, if it would be useful for the comunity, we would be happy if you would share it's code.
By default, it allows to monitor these servers:
 - [Jenkins](https://jenkins.io/)
 - [CrusieControl.NET](https://github.com/ccnet/CruiseControl.NET)
 
 ## Documentation
 
 Documentation is available as a [github WIKI](https://github.com/Soloplan/whatson/wiki)
 
 ## Contribution
 
 You can create your own plugin, see this [guide](https://github.com/Soloplan/whatson/wiki/Developing-a-new-plugin) for details.
 
 You can checkout the [Travis CI plugin](https://github.com/steffen-wilke/whatson-travis-ci) as example.
 
 See also [contribution guidelines](https://github.com/Soloplan/whatson/wiki/Contribiution).
 
## Working with coding agents

See [AGENTS.md](AGENTS.md) for the repository layout, Windows build prerequisites,
build and test commands, coding conventions, and agent coordination guidance.

 ## Creators

**Christian Heidl**

**Steffen Wilke**

- <https://github.com/steffen-wilke>

**Dominik Gouda**

- <https://github.com/dominikgolda>

**Krzysztof Lorenc**

- <https://github.com/krzysztof-lorenc>

**Tomasz Skowron**

- <https://github.com/skowront>

## License

[MIT license](https://github.com/Soloplan/whatson/blob/master/LICENSE)
