[license]: https://tldrlegal.com/l/mit
[docs]: http://oxidemod.github.io/Docs/
[forums]: http://oxidemod.org/
[issues]: https://github.com/OxideMod/Oxide/issues
[downloads]: http://oxidemod.org/downloads/

# Oxide Mod [![License](http://img.shields.io/badge/license-MIT-lightgrey.svg?style=flat)][License] [![Build Status](https://travis-ci.org/OxideMod/Oxide.png)](https://travis-ci.org/OxideMod/Oxide)

A complete rewrite of the popular, original Oxide API and Lua plugin framework. Previously only available for the legacy Rust game, Oxide now supports numerous games. Oxide's focus is on modularity and extensibility. The core is highly abstracted and loosely coupled, and could be used to mod any game that uses the .NET Framework.

Support for each game and plugin language is added via extensions. When loading, Oxide scans the binary folder for DLL extensions. Extension filenames are formatted as `Oxide.Ext.Name.dll` or `Oxide.Game.Name.dll`.

## Official Core Extensions

 * Oxide.Ext.CSharp - _Allows plugins written in [CSharp](http://en.wikipedia.org/wiki/C_Sharp_(programming_language)) to be loaded_
 * Oxide.Ext.JavaScript - _Allows plugins written in [JavaScript](http://en.wikipedia.org/wiki/JavaScript) to be loaded_
 * Oxide.Ext.Lua - _Allows plugins written in [Lua](http://www.lua.org/) to be loaded_
 * Oxide.Ext.MySql - _Allows plugins to access a [MySQL](http://www.mysql.com/) database_
 * Oxide.Ext.Python - _Allows plugins written in [Python](http://en.wikipedia.org/wiki/Python_(programming_language)) to be loaded_
 * Oxide.Ext.SQLite - _Allows plugins to access a [SQLite](http://www.sqlite.org/) database_
 * Oxide.Ext.Unity - _Provides support for [Unity](http://unity3d.com/) powered games_

## Supported Game Extensions
 * Oxide.Game.BeastsOfPrey - _Provides support for the [Beasts of Prey](http://www.beastsofprey.com/) server_
 * Oxide.Game.ReignOfKings - _Provides support for the [Reign of Kings](http://www.reignofkings.net/) server_
 * Oxide.Game.Rust - _Provides support for the [Rust](http://playrust.com/) Experimental server_
 * Oxide.Game.RustLegacy - _Provides support for the [Rust](http://playrust.com/) Legacy server_
 * Oxide.Game.SevenDays - _Provides support for the [7 Days to Die](http://7daystodie.com/) server_
 * Oxide.Game.TheForest - _Provides support for the [The Forest](http://survivetheforest.com/) server_

## Community Extensions

 * [Oxide.Ext.RustIO](http://oxidemod.org/extensions/rust-io.768/) - _Provides generation of map images and a live map for Rust_

## Open Source

Oxide is free, open source software distributed under the [MIT License][license]. We accept and encourage contributions from our community, and sometimes give cookies in return.

## Compiling Source

While we recommend using one of the [official release builds][downloads], you can compile your own builds if you'd like. Keep in mind that only official builds are supported by the Oxide team and community.

 1. Clone the git repository locally using `git clone https://github.com/OxideMod/Oxide.git` or download the [latest ZIP](https://github.com/OxideMod/Oxide/archive/master.zip).
 2. Open the solution in the latest version of [Visual Studio 2015](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx), which includes .NET Framework 4.6.
 3. Build the project using the solution. If you get errors, you're likely not using the latest Visual Studio 2015.
 4. Copy the files from the `Bundles` directory for your game of choice to your server installation.

## Getting Help

* The best place to start with plugin development is the official [API documentation][docs].
* Still need help? Search our [community forums][forums] or create a new thread if needed.

## Contributing

* Got an idea or suggestion? Use the [community forums][forums] to share and discuss it.
* Troubleshoot issues you run into on the community forums so everyone can help and reference it later.
* File detailed [issues] on GitHub (version number, what you did, and actual vs expected outcomes).
* Want Oxide and plugins for your favorite game? Hook us up and we'll see what we can do!

## Reporting Security Issues

Please disclose security issues responsibly by emailing security@oxidemod.org with a full description. We'll work on releasing an updated version as quickly as possible. Please do not email non-security issues; use the [forums] or [issue tracker][issues] instead.
