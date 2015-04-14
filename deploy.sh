#!/bin/bash

function die_with { echo "$*" >&2; exit 1; }

echo "Are you Travis?"
if [ ! $TRAVIS ]; then die_with "You are not Travis!"; fi

echo "Checking if commit is a pull request"
if [ $TRAVIS_PULL_REQUEST == true ]; then die_with "Skipping deployment for pull request!"; fi

echo "Configuring git credentials"
git config --global user.email "travis@travis-ci.org" && git config --global user.name "Travis" || die_with "Failed to configure git credentials!"

echo "Cloning snapshots branch using token"
git clone -q https://$GITHUB_TOKEN@github.com/OxideMod/Snapshots.git $HOME/Snapshots >/dev/null || die_with "Failed to clone snapshots repo!"

function bundle_rust {
    cd $HOME/build/$TRAVIS_REPO_SLUG || die_with "Failed to change to project home!"
    mkdir -p $HOME/temp_rust/RustDedicated_Data/Managed/x64 || die_with "Failed to create directory structure!"

    echo "Copying target files to temp directory"
    cp -vf Oxide.Core/bin/Release/Oxide.Core.dll \
    Oxide.Ext.CSharp/bin/Release/Oxide.Ext.CSharp.dll \
    Oxide.Ext.JavaScript/bin/Release/Oxide.Ext.JavaScript.dll \
    Oxide.Ext.Lua/bin/Release/Oxide.Ext.Lua.dll \
    Oxide.Ext.MySql/bin/Release/Oxide.Ext.MySql.dll \
    Oxide.Ext.Python/bin/Release/Oxide.Ext.Python.dll \
    Oxide.Ext.Rust/bin/Release/Oxide.Ext.Rust.dll \
    Oxide.Ext.SQLite/bin/Release/Oxide.Ext.SQLite.dll \
    Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll \
    $HOME/temp_rust/RustDedicated_Data/Managed || die_with "Failed to copy core and extension DLLs!"
    cp -vf Oxide.Ext.CSharp/Dependencies/Mono.Cecil.dll \
    Oxide.Ext.JavaScript/Dependencies/Jint.dll \
    Oxide.Ext.Lua/Dependencies/*Lua.dll \
    Oxide.Ext.MySql/Dependencies/*.dll \
    Oxide.Ext.Python/Dependencies/*.dll \
    Oxide.Ext.SQLite/Dependencies/System.*.dll \
    $HOME/temp_rust/RustDedicated_Data/Managed || die_with "Failed to copy dependency DLLs!"
    cp -vf Oxide.Ext.Lua/Dependencies/x64/*.dll \
    Oxide.Ext.SQLite/Dependencies/x64/*.dll \
    $HOME/temp_rust/RustDedicated_Data/Managed/x64 || die_with "Failed to copy dependency DLLs!"
    cp -vf Oxide.Ext.Rust/Patched/Assembly-CSharp.dll \
    $HOME/temp_rust/RustDedicated_Data/Managed || die_with "Failed to copy patched server files!"
    cp -vf Oxide.Ext.Rust/Patched/oxide.root.json \
    Oxide.Ext.CSharp/Dependencies/CSharpCompiler.exe \
    Oxide.Ext.CSharp/Dependencies/mono-2.0.dll \
    Oxide.Ext.CSharp/Dependencies/msvcr120.dll \
    $HOME/temp_rust || die_with "Failed to copy config file and root DLLs!"

    echo "Bundling and compressing target files"
    cd $HOME/temp_rust || die_with "Failed to change to temp directory!"
    rm -f $HOME/Snapshots/Oxide-Rust.zip || die_with "Failed to remove old bundle!"
    zip -FS -vr9 $HOME/Snapshots/Oxide-Rust.zip . || die_with "Failed to bundle snapshot files!"
} || die_with "Failed to create Rust bundle!"

function bundle_rustlegacy {
    cd $HOME/build/$TRAVIS_REPO_SLUG || die_with "Failed to change to project home!"
    mkdir -p $HOME/temp_rustlegacy/rust_server_Data/Managed/x86 || die_with "Failed to create directory structure!"

    echo "Copying target files to temp directory"
    cp -vf Oxide.Core/bin/Release/Oxide.Core.dll \
    Oxide.Ext.CSharp/bin/Release/Oxide.Ext.CSharp.dll \
    Oxide.Ext.JavaScript/bin/Release/Oxide.Ext.JavaScript.dll \
    Oxide.Ext.Lua/bin/Release/Oxide.Ext.Lua.dll \
    Oxide.Ext.MySql/bin/Release/Oxide.Ext.MySql.dll \
    Oxide.Ext.Python/bin/Release/Oxide.Ext.Python.dll \
    Oxide.Ext.RustLegacy/bin/Release/Oxide.Ext.RustLegacy.dll \
    Oxide.Ext.SQLite/bin/Release/Oxide.Ext.SQLite.dll \
    Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll \
    $HOME/temp_rustlegacy/rust_server_Data/Managed || die_with "Failed to copy core and extension DLLs!"
    cp -vf Oxide.Core/Dependencies/Newtonsoft.Json.dll \
    Oxide.Ext.CSharp/Dependencies/Mono.Cecil.dll \
    Oxide.Ext.JavaScript/Dependencies/Jint.dll \
    Oxide.Ext.Lua/Dependencies/*Lua.dll \
    Oxide.Ext.MySql/Dependencies/*.dll \
    Oxide.Ext.Python/Dependencies/*.dll \
    Oxide.Ext.SQLite/Dependencies/System.*.dll \
    Oxide.Ext.RustLegacy/Dependencies/System.*.dll \
    $HOME/temp_rustlegacy/rust_server_Data/Managed || die_with "Failed to copy dependency DLLs!"
    cp -vf Oxide.Ext.Lua/Dependencies/x86/*.dll \
    Oxide.Ext.SQLite/Dependencies/x86/*.dll \
    $HOME/temp_rustlegacy/rust_server_Data/Managed/x86 || die_with "Failed to copy dependency DLLs!"
    cp -vf Oxide.Ext.RustLegacy/Patched/Assembly-CSharp.dll \
    $HOME/temp_rustlegacy/rust_server_Data/Managed || die_with "Failed to copy patched server files!"
    cp -vf Oxide.Ext.RustLegacy/Patched/oxide.root.json \
    Oxide.Ext.CSharp/Dependencies/CSharpCompiler.exe \
    Oxide.Ext.CSharp/Dependencies/mono-2.0.dll \
    Oxide.Ext.CSharp/Dependencies/msvcr120.dll \
    $HOME/temp_rustlegacy || die_with "Failed to copy config file and root DLLs!"

    echo "Bundling and compressing target files"
    cd $HOME/temp_rustlegacy || die_with "Failed to change to temp directory!"
    rm -f $HOME/Snapshots/Oxide-RustLegacy.zip || die_with "Failed to remove old bundle!"
    zip -FS -vr9 $HOME/Snapshots/Oxide-RustLegacy.zip . || die_with "Failed to bundle snapshot files!"
} || die_with "Failed to create Rust Legacy bundle!"

function bundle_7dtd {
    cd $HOME/build/$TRAVIS_REPO_SLUG || die_with "Failed to change to project home!"
    mkdir -p $HOME/temp_7dtd/7DaysToDie_Data/Managed/x64 || die_with "Failed to create directory structure!"
    mkdir -p $HOME/temp_7dtd/7DaysToDie_Data/Managed/x86 || die_with "Failed to create directory structure!"

    echo "Copying target files to temp directory"
    cp -vf Oxide.Core/bin/Release/Oxide.Core.dll \
    Oxide.Ext.CSharp/bin/Release/Oxide.Ext.CSharp.dll \
    Oxide.Ext.JavaScript/bin/Release/Oxide.Ext.JavaScript.dll \
    Oxide.Ext.Lua/bin/Release/Oxide.Ext.Lua.dll \
    Oxide.Ext.MySql/bin/Release/Oxide.Ext.MySql.dll \
    Oxide.Ext.Python/bin/Release/Oxide.Ext.Python.dll \
    Oxide.Ext.SevenDays/bin/Release/Oxide.Ext.SevenDays.dll \
    Oxide.Ext.SQLite/bin/Release/Oxide.Ext.SQLite.dll \
    Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed || die_with "Failed to copy core and extension DLLs!"
    cp -vf Oxide.Core/Dependencies/Newtonsoft.Json.dll \
    Oxide.Ext.CSharp/Dependencies/Mono.Cecil.dll \
    Oxide.Ext.JavaScript/Dependencies/Jint.dll \
    Oxide.Ext.Lua/Dependencies/*Lua.dll \
    Oxide.Ext.MySql/Dependencies/*.dll \
    Oxide.Ext.Python/Dependencies/*.dll \
    Oxide.Ext.SevenDays/Dependencies/System.*.dll \
    Oxide.Ext.SQLite/Dependencies/System.*.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed || die_with "Failed to copy dependency DLLs!"
    cp -vf Oxide.Ext.Lua/Dependencies/x64/*.dll \
    Oxide.Ext.SQLite/Dependencies/x64/*.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed/x64 || die_with "Failed to copy dependency DLLs!"
    cp -vf Oxide.Ext.Lua/Dependencies/x86/*.dll \
    Oxide.Ext.SQLite/Dependencies/x86/*.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed/x86 || die_with "Failed to copy dependency DLLs!"
    cp -vf Oxide.Ext.SevenDays/Patched/Assembly-CSharp.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed || die_with "Failed to copy patched server files!"
    cp -vf Oxide.Ext.SevenDays/Patched/oxide.root.json \
    Oxide.Ext.CSharp/Dependencies/CSharpCompiler.exe \
    Oxide.Ext.CSharp/Dependencies/mono-2.0.dll \
    Oxide.Ext.CSharp/Dependencies/msvcr120.dll \
    $HOME/temp_7dtd || die_with "Failed to copy config file and root DLLs!"

    echo "Bundling and compressing target files"
    cd $HOME/temp_7dtd || die_with "Failed to change to temp directory!"
    zip -FS -vr9 $HOME/Snapshots/Oxide-7DaysToDie.zip . || die_with "Failed to bundle snapshot files!"
} || die_with "Failed to create 7 Days to Die bundle!"

bundle_rust; bundle_rustlegacy; bundle_7dtd

echo "Adding, committing, and pushing to snapshots"
cd $HOME/Snapshots || die_with "Failed to change to snapshots directory!"
git add . && git commit -m "Oxide build $TRAVIS_BUILD_NUMBER from https://github.com/$TRAVIS_REPO_SLUG/commit/${TRAVIS_COMMIT:0:7}" || die_with "Failed to add and commit files!"
git push -q origin master >/dev/null || die_with "Failed to push snapshots to GitHub!"

echo "Deployment cycle completed. Happy developing!"
