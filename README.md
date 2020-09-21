
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
- [Feature requests and bugs](#feature-requests-and-bugs)
- [Plugins](#plugins)
- [Documentation](#documentation)
- [Contribiution](#contribution)
- [Creators](#creators)
- [License](#License)

## Features
- Monitor multiple builds status with a lot of additional information.
- Supports multiple build automation systems. By default Jenkins and CruiseControl.NET, but you can create your own plugin!
- Quick view for status of last 5 builds (Jenkins plugin)
- Easy configuration directly on main window.
- Wizard to add new projects.
- Configurable Windows notifications.
- Dark and light theme

## Quick start

1. [Download the latest release.](https://github.com/Soloplan/whatson/releases/latest)
1. Save it to the location from which you will be using it.
1. Start the app from file *Soloplan.WhatsON.GUI.exe*
1. If you want the app to start during the system start, open the configuration (gear icon at the top, left corner) and set the option "Run when windows starts"
1. To add a new project build for monitoring, you can use the wizard (plus icon at the top, left corner).

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
