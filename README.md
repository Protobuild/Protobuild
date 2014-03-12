Protobuild
==========

Protobuild is a project generation system for C#.  It aims to make maintaining projects and their dependencies easier by requiring only minimum specification of the project requirements and by inferring the rest from the projects in the module.  It also eases cross-platform development as it allows you to specify projects that are conditional based on the platform that is being built for.

_**Documentation such as usage guides can be found on the [GitHub Wiki](https://github.com/hach-que/Protobuild/wiki).**_

Modules
-----------

Protobuild works on the concept of modules; modules contain a series of projects and submodules.  Each module contains it's own copy of Protobuild and the `Build` folder (which is where the generator and project files are stored).

To create a new Protobuild module to work in, download the Protobuild.exe file to a directory and run it.  When running it you will be prompted to turn this folder into a module.  This is done by extracting required files into a `Build` folder.

Projects
-----------

You can add new projects by creating the `.definition` files yourself, or by using the Protobuild Module Manager.  To start the Module Manager, run `Protobuild.exe --manager-gui`.

The Module Manager provides a set of templates you can use for new projects; these will be default, empty projects with bare bones configuration.  The Protobuild Module Manager will run the generation script to create C# projects for the current platform you are running on, from these project definitions.

Generation
------------

You can generate the projects for you current platform by just running Protobuild.  If you want to generate projects for a specific target platform, you can do so with `Protobuild.exe --generate <Platform>`.

Synchronisation
-----------------

Unlike other project generators, Protobuild supports synchronising the changes made in the C# projects back into the definition files.  This means that changes you make in the IDE are automatically made in your build definitions.

To synchronise your projects to the build definitions, just run Protobuild again.  You should run Protobuild before you commit to source control.

Source Control
-----------------

Since the definition files under `Build\Projects` are the source of truth for the project structure, you should not include the C# project or solution files in your source control repository.  These will change whenever the definition files change, and they serve no benefit to be included (users can run the `Protobuild.exe` file in the root of the source control project to generate the required C# project and solution files).

Platforms
------------

Protobuild supports the following platforms directly, but by customizing the project generation XSLT you can support any platform you like:

* Android (via Xamarin)
* iOS (via Xamarin)
* Linux
* MacOS
* Ouya (via Xamarin)
* Windows
* Windows8
* WindowsPhone

For example, to generate for the WindowsPhone project, use `Protobuild.exe --generate WindowsPhone`.
