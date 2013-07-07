Protobuild
==========

Protobuild is a project generation system for C#.  It aims to make maintaining projects and their dependencies easier by requiring only minimum specification of the project requirements and by inferring the rest from the projects in the module.  It also eases cross-platform development as it allows you to specify projects that are conditional based on the platform that is being built for.

Modules
-----------

Protobuild works on the concept of modules; modules contain a series of projects and submodules.  Each module contains it's own copy of Protobuild and the `Build` folder (which is where the generator and project files are stored).

To create a new Protobuild module to work in, download the Protobuild.exe file to a directory and run it.  When running it you will be prompted to turn this folder into a module.  This is done by extracting required files into a `Build` folder.

Projects
-----------

You can add new projects using the Protobuild Module Manager.  These will be default, empty projects with bare bones configuration.  The Protobuild Module Manager will run the generation script to create C# projects for the current platform you are running on, from these project definitions.  When the Protobuild Module Manager runs, it also automatically synchronises changes back from the C# project files into the definition files (this is important as it allows you to add and remove files in IDEs as you would normally, without having to manually update the definition files).

Generation
------------

The Protobuild Module Manager will extract a `Main.proj` MSBuild script to the `Build` folder on first run.  This script uses the custom tasks inside the Module Manager to generate the C# projects.

Normally the Module Manager will automatically run this script when it is started, or after adding a new project.  However you can use this script to generate the projects without interaction (for example, for build servers), or if you want to generate projects for a different platform.

Source Control
-----------------

Since the definition files under `Build\Projects` are the source of truth for the project structure, you should not include the C# project or solution files in your source control repository.  These will change whenever the definition files change, and they serve no benefit to be included (users can run the `Protobuild.exe` file in the root of the source control project to generate the required C# project and solution files).
