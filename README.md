Oxide-2
=======

Oxide 2 is a complete rewrite of the original popular Oxide mod for the game Rust.
Oxide 2 has a focus on modularity and extensibility.
The core is highly abstracted and loosely coupled, and could be used to mod any game that uses .NET.
Functionality is applied through the use of extensions.
This means Oxide 2 could be used to mod other games such as The Forest or Space Engineers.

Extensions
----------

When loading, Oxide 2 scans the binary folder for dll extensions.

Extension filenames are formatted as follows:  
Oxide.Ext.Name.dll

Current extensions are listed below.

 * Oxide.Ext.Unity - Provides support for Unity games
 * Oxide.Ext.Rust - Provides support for the Rust server
 * Oxide.Ext.Lua - Allows Lua plugins to be loaded

As an example to what kind of extensions may be used in the future, here is a non-exhaustive stive list of possibilites.

 * Oxide.Ext.Py - Allows python plugins to be loaded
 * Oxide.Ext.JS - Allows javascript plugins to be loaded
 * Oxide.Ext.MySQL - Allows plugins to access a MySQL database
 * Oxide.Ext.WebServer - Allows the server or modded game to also host a web server

Installation for Rust Server Users
----------------------------------

 1. Clone the git repo locally
 2. Open the solution in visual studio (2013 is recommended, should work on earlier versions)
 3. You will probably get a missing project error, don't worry about that
 4. Go into the project properties for Oxide.Core, go to "Build Events", and change the "Post-build event command line" to point at your server directory
	Alternatively, just remove the build even completely by making it blank, though you'll need to copy dlls manually if you do this
	Do the same for the other projects (apart from Oxide.Tests)
 5. Compile everything. If you get errors, it probably means you're missing .net framework or you didn't change the build events in step 4 properly
 6. Navigate to the "Dependencies" folder. Copy lua52.dll next to RustDedicated.exe. Copy KeraLua.dll, KopiLua.dll and NLua.dll into RustDedicated_Data/Managed, they should sit next to Oxide.Core.dll and the extensions
 7. Copy oxide.root.json next to RustDedicated.exe
 8. Navigate to the "Patched" folder and copy the dlls into RustDedicated_Data/Managed. Overwrite the existing ones.
 9. Launch the server like you do normally. If everything goes well, there will be a block of Oxide output in the console and no red
 10. Once the server has launched once, a folder called "oxide" will be created in your server instance directory. This will hold your "plugins", "data", "logs" and "config" folders.