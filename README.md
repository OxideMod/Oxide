Oxide 2 [![Build Status](https://travis-ci.org/strykes/Oxide.png)](https://travis-ci.org/strykes/Oxide)
=======

Oxide 2 is a complete rewrite of the popular, original Oxide mod for the game Rust. Oxide 2 has a focus on modularity and extensibility. The core is highly abstracted and loosely coupled, and could be used to mod any game that uses .NET such as 7 Days to Die, The Forest, Space Engineers, and more.

Extensions
----------

When loading, Oxide 2 scans the binary folder for DLL extensions.

Extension filenames are formatted as follows:  
Oxide.Ext.Name.dll

Current extensions are listed below:

 * Oxide.Ext.CSharp - Allows raw CSharp plugins to be loaded
 * Oxide.Ext.JavaScript - Allows JavaScript plugins to be loaded
 * Oxide.Ext.Lua - Allows Lua plugins to be loaded
 * Oxide.Ext.Python - Allows Python plugins to be loaded
 * Oxide.Ext.Rust - Provides support for the Rust Experimental server
 * Oxide.Ext.Unity - Provides support for Unity games

As an example to what kind of extensions may be used in the future, here is a non-exhaustive list of possibilities:

 * Oxide.Ext.MySQL - Allows plugins to access a MySQL database
 * Oxide.Ext.WebServer - Allows the server or modded game to also host a web server

Building from Source
--------------------

 1. Clone the git repository locally.
 2. Open the solution in Visual Studio (2013 is recommended, but it should work on earlier versions).
 3. Compile the project. If you get errors, it probably means you're missing .NET framework.
