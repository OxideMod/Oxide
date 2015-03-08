Oxide 2 [![Build Status](https://travis-ci.org/OxideMod/Oxide.png)](https://travis-ci.org/OxideMod/Oxide)
=======

Oxide 2 is a complete rewrite of the popular, original Oxide mod for the game Rust. Oxide has a focus on modularity and extensibility.

The core is highly abstracted and loosely coupled, and could be used to mod any game that uses .NET such as 7 Days to Die, The Forest, Space Engineers, and more. Support for games, plugin languages, and other functionality is added via extensions. When loading, Oxide 2 scans the binary folder for DLL extensions. Extension filenames are formatted as `Oxide.Ext.Name.dll`.

The current official extensions are listed below:

 * Oxide.Ext.CSharp - _Allows raw [CSharp plugins](http://en.wikipedia.org/wiki/C_Sharp_(programming_language)) to be loaded_
 * Oxide.Ext.JavaScript - _Allows [JavaScript](http://en.wikipedia.org/wiki/JavaScript) plugins to be loaded_
 * Oxide.Ext.Lua - _Allows [Lua](http://www.lua.org/) plugins to be loaded_
 * Oxide.Ext.MySql - _Allows plugins to access a [MySQL](http://www.mysql.com/) database_
 * Oxide.Ext.Python - _Allows [Python](http://en.wikipedia.org/wiki/Python_(programming_language)) plugins to be loaded_
 * Oxide.Ext.Rust - _Provides support for the [Rust](http://playrust.com/) Experimental server_
 * Oxide.Ext.SevenDays - _Provides support for the [7 Days to Die](http://7daystodie.com/) server_
 * Oxide.Ext.SQLite - _Allows plugins to access a [SQLite](http://www.sqlite.org/) database_
 * Oxide.Ext.Unity - _Provides support for [Unity](http://unity3d.com/) games_

Third-party, unofficial extensions available:

 * [Oxide.Ext.RustIO.dll](http://forum.rustoxide.com/resources/768/) - _Provides generation of map images, lightweight web server, and live map_

Examples of what extensions may be available in the future:

 * Oxide.Ext.IRC - _Allows plugins to access an IRC server_
 * Oxide.Ext.Updater - _Allows for automatic plugin checking and updating_

# Compiling Source

While we recommend using one of the [official release builds](http://forum.rustoxide.com/download/), you can compile your own builds if you'd like.

 1. Clone the git repository locally using `git clone https://github.com/OxideMod/Oxide.git`
 2. Open the solution the latest version of [Visual Studio 2015](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx) which includes .NET Framework 4.6.
 3. Build the project using the solution. If you get errors, you're most likely not using Visual Studio 2015.

Keep in mind that only official builds are supported by the Oxide team and community.
