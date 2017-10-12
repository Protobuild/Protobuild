Protobuild
==========

_Cross-platform project generation for C#_

Define your project content once and compile code for every platform, in any IDE or build system, on any operating system.  See http://protobuild.org/ for more information.

Getting Started
------------------

Whether you're looking to start using Protobuild in your own project, or using it to generate an existing project, documentation such as usage guides can be found on [Read the Docs](https://protobuild.readthedocs.org/).

Build Status
-------------

|     | Status |
| --- | ----- |
| **Core Projects** |  |
| Protobuild | ![](https://build-oss.redpoint.games/buildStatus/icon?job=Protobuild/Protobuild/master) |
| Protobuild Manager | ![](https://build-oss.redpoint.games/buildStatus/icon?job=Protobuild/Protobuild.Manager/master) |
| Protobuild for Visual Studio | ![](https://build-oss.redpoint.games/buildStatus/icon?job=Protobuild/Protobuild.IDE.VisualStudio/master) 

Overview
----------

The goal of Protobuild is to enable developers to easily write cross-platform .NET software for all platforms.  Unlike solutions such as Shared Code Projects and PCLs, Protobuild allows projects to have different references per platform while also taking full advantage of the native APIs available on each platform.

Protobuild offers the following features:

  * **Protobuild ships as a single executable in your repository**; your users don't need to install any software to use it
  * **Generate C# and C++ solutions and projects for any platform that supports .NET**
  * **Two-way project synchronisation**; changes you make in your IDE (adding or removing files) are synchronised automatically back to the cross-platform definition files
  * **Full cross-platform package management**, with packages seperated based on platform instead of frameworks
  * Support for content projects, which define assets for inclusion in cross-platform projects
  * Support for include projects, which define sets of files for inclusion in other cross-platform projects
  * A optional, separately-installed graphical interface which allows you to manage cross-platform projects: https://github.com/hach-que/Protobuild.Manager
  * A cross-platform automated build framework for use on build servers

We put a high focus on the following important principles:

  * **Zero maintainance** - We ensure that you never need to maintain or update your project definitions when new versions of Protobuild are released, because Protobuild ships as a single executable in your repository.  There's nothing for you or your users to install in order to generate projects.
  * **Guaranteed backwards and forwards compatibility** - When you include a third-party that's also using Protobuild, we guarentee that the version of Protobuild in your repository will be able to invoke the third-party library's copy of Protobuild, even when they're different versions.
  * **Complete customizability** - When you need to do something custom in your project, Protobuild offers you the flexibility to do so, without forking Protobuild.  Almost all of Protobuild's project generation is driven by embedded XSLT files; it offers an option `--extract-xslt` with which you can extract those XSLT templates to your repository.  Protobuild will then use your versions of the templates for generating projects.

Documentation
----------------

We have full and extensive [documentation](https://protobuild.readthedocs.org/en/latest/) available on Read the Docs.

Supported Platforms
--------------------

Protobuild supports the following platforms out-of-the-box, but by customizing the project generation you can support any platform you like:

  * Android (via Xamarin)
  * iOS (via Xamarin)
  * tvOS (via Xamarin)
  * Linux
  * MacOS
  * Ouya (via Xamarin)
  * PCL (for [Bait-and-Switch PCL](http://log.paulbetts.org/the-bait-and-switch-pcl-trick/) only)
  * Windows
  * Windows8
  * WindowsPhone
  * WindowsPhone81
  * WindowsUniversal
  * Web (via [JSIL](https://github.com/sq/JSIL))

For example, to generate for the WindowsPhone platform, use `Protobuild.exe --generate WindowsPhone`.

How to Build
-----------------

If you wish to open Protobuild in your IDE, double-click `Protobuild.exe` to generate the solution, and then open the solution in your IDE.  If you are on Mac or Linux, you will need to run `mono Protobuild.exe` from the command-line.

If you want to prepare your changes for submission in a PR, run `Protobuild.exe --automated-build` before you commit in order to run the tests and prepare the final Protobuild executable.  Even when you submit a PR, we will still re-run this step to ensure integrity of the executable.

How to Contribute
--------------------

To contribute to Protobuild, refer to the [contributor documentation](https://protobuild.readthedocs.org/en/latest/contributing.html).

The developer chat is hosted on [Gitter](https://gitter.im/Protobuild/Protobuild)

[![Gitter](https://badges.gitter.im/Protobuild/Protobuild.svg)](https://gitter.im/Protobuild/Protobuild?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)


Providing Feedback / Obtaining Support
-----------------------------------------

To provide feedback or get support about issues, please file a GitHub issue on this repository.  Additional support options are available according to the [support documentation](https://protobuild.readthedocs.org/en/latest/support.html).

License Information
---------------------

Protobuild is licensed under the MIT license.

```
Copyright (c) 2015 Various Authors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
```

Related Projects
--------------------

  * [Protobuild.Manager](https://github.com/hach-que/Protobuild.Manager): A stand-alone graphical tool for creating and opening Protobuild modules
  * [Protobuild.IDE.VisualStudio](https://github.com/hach-que/Protobuild.IDE.VisualStudio): A Visual Studio extension that provides support services for cross-platform projects in Visual Studio
  * [Protobuild.IDE.MonoDevelop](https://github.com/hach-que/Protobuild.IDE.MonoDevelop): A MonoDevelop / Xamarin Studio extension that provides support services for cross-platform projects in MonoDevelop and Xamarin Studio
  * [Protobuild.Docs](https://protobuild.readthedocs.org/en/latest/): The documentation for Protobuild, for the source files to the documentation, please see [this repository](https://github.com/hach-que/Protobuild.Docs)

Community Code of Conduct
------------------------------

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).
