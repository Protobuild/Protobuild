Protobuild
==========

_Cross-platform project generation for C#_

Define your project content once and compile code for every platform, in any IDE or build system, on any operating system.  See http://protobuild.org/ for more information.

Getting Started
------------------

Whether you're looking to start using Protobuild in your own project, or using it to generate an existing project, documentation such as usage guides can be found on [Read the Docs](https://protobuild.readthedocs.org/).

Supported Platforms
--------------------

Protobuild supports the following platforms out-of-the-box, but by customizing the project generation you can support any platform you like:

  * Android (via Xamarin)
  * iOS (via Xamarin)
  * Linux
  * MacOS
  * Ouya (via Xamarin)
  * PCL (for [Bait-and-Switch PCL](http://log.paulbetts.org/the-bait-and-switch-pcl-trick/) only)
  * PSMobile
  * Windows
  * Windows8
  * WindowsPhone
  * WindowsPhone81
  * Web (via [JSIL](https://github.com/sq/JSIL))

For example, to generate for the WindowsPhone platform, use `Protobuild.exe --generate WindowsPhone`.
