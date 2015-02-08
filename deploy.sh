#!/bin/bash

function die_with { echo "$*" >&2; exit 1; }

echo "Checking if commit is a pull request"
if [ $TRAVIS_PULL_REQUEST == true ]; then die_with "Skipping deployment for pull request!"; fi

echo "Configuring git credentials"
git config --global user.email "travis@travis-ci.org" && git config --global user.name "Travis" || die_with "Failed to configure git credentials!"

echo "Cloning snapshots branch using token"
git clone -q https://$GITHUB_TOKEN@github.com/OxideMod/Snapshots.git $HOME/Snapshots >/dev/null || die_with "Failed to clone snapshots repo!"

function bundle_rust {
    cd $HOME/build/$TRAVIS_REPO_SLUG || die_with "Failed to change to project home!"
    mkdir -p $HOME/temp_rust/RustDedicated_Data/Managed || die_with "Failed to create directory structure!"

    echo "Copying target files to temp directory"
    cp -vf Oxide.Core/bin/Release/Oxide.Core.dll \
    Oxide.Ext.CSharp/bin/Release/Oxide.Ext.CSharp.dll \
    Oxide.Ext.JavaScript/bin/Release/Oxide.Ext.JavaScript.dll \
    Oxide.Ext.Lua/bin/Release/Oxide.Ext.Lua.dll \
    Oxide.Ext.Python/bin/Release/Oxide.Ext.Python.dll \
    Oxide.Ext.Rust/bin/Release/Oxide.Ext.Rust.dll \
    Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll \
    $HOME/temp_rust/RustDedicated_Data/Managed || die_with "Failed to copy core and extension DLLs!"
    cp -vf Oxide.Ext.CSharp/Dependencies/Mono.*.dll \
    Oxide.Ext.JavaScript/Dependencies/Jint.dll \
    Oxide.Ext.Lua/Dependencies/*Lua.dll \
    Oxide.Ext.Python/Dependencies/IronPython.dll \
    Oxide.Ext.Python/Dependencies/Microsoft.Dynamic.dll \
    Oxide.Ext.Python/Dependencies/Microsoft.Scripting*.dll \
    $HOME/temp_rust/RustDedicated_Data/Managed || die_with "Failed to copy dependency DLLs!"
    cp -f Oxide.Ext.Rust/Patched/Assembly-CSharp.dll \
    Oxide.Ext.Rust/Patched/Facepunch.dll \
    $HOME/temp_rust/RustDedicated_Data/Managed || die_with "Failed to copy patched server files!"
    cp -f Oxide.Ext.Rust/Patched/oxide.root.json \
    Oxide.Ext.Lua/Dependencies/lua5*.dll \
    $HOME/temp_rust || die_with "Failed to copy config file and Lua DLLs!"

    echo "Bundling and compressing target files"
    cd $HOME/temp_rust || die_with "Failed to change to temp directory!"
    rm -f $HOME/Snapshots/Rust/OxideRust.zip || die_with "Failed to remove old bundle!"
    zip -vr9 $HOME/Snapshots/Rust/OxideRust.zip . || die_with "Failed to bundle snapshot files!"
} || die_with "Failed to create Rust bundle!"

function bundle_7dtd {
    cd $HOME/build/$TRAVIS_REPO_SLUG || die_with "Failed to change to project home!"
    mkdir -p $HOME/temp_7dtd/7DaysToDie_Data/Managed || die_with "Failed to create directory structure!"

    echo "Copying target files to temp directory"
    cp -vf Oxide.Core/bin/Release/Oxide.Core.dll \
    Oxide.Ext.CSharp/bin/Release/Oxide.Ext.CSharp.dll \
    Oxide.Ext.JavaScript/bin/Release/Oxide.Ext.JavaScript.dll \
    Oxide.Ext.Python/bin/Release/Oxide.Ext.Python.dll \
    Oxide.Ext.SevenDays/bin/Release/Oxide.Ext.SevenDays.dll \
    Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed || die_with "Failed to copy core and extension DLLs!"
    cp -vf Oxide.Ext.CSharp/Dependencies/Mono.*.dll \
    Oxide.Ext.JavaScript/Dependencies/Jint.dll \
    Oxide.Ext.Lua/Dependencies/*Lua.dll \
    Oxide.Ext.Python/Dependencies/IronPython.dll \
    Oxide.Ext.Python/Dependencies/Microsoft.Dynamic.dll \
    Oxide.Ext.Python/Dependencies/Microsoft.Scripting*.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed || die_with "Failed to copy dependency DLLs!"
    cp -f Oxide.Ext.SevenDays/Patched/Assembly-CSharp.dll \
    $HOME/temp_7dtd/7DaysToDie_Data/Managed || die_with "Failed to copy patched server files!"
    cp -vf Oxide.Ext.SevenDays/Patched/oxide.root.json \
    Oxide.Ext.Lua/Dependencies/lua5*.dll \
    $HOME/temp_7dtd || die_with "Failed to copy config file and Lua DLLs!"

    echo "Bundling and compressing target files"
    cd $HOME/temp_7dtd || die_with "Failed to change to temp directory!"
    rm -f $HOME/Snapshots/7DaysToDie/Oxide7DaysToDie.zip || die_with "Failed to remove old bundle!"
    zip -vr9 $HOME/Snapshots/7DaysToDie/Oxide7DaysToDie.zip . || die_with "Failed to bundle snapshot files!"
} || die_with "Failed to create 7 Days to Die bundle!"

bundle_rust; bundle_7dtd

echo "Adding, committing, and pushing to snapshots"
cd $HOME/Snapshots || die_with "Failed to change to snapshots directory!"
git add -f . && git commit -m "Oxide build $TRAVIS_BUILD_NUMBER" || die_with "Failed to add and commit files!"
git push -qf origin master >/dev/null || die_with "Failed to push snapshots to GitHub!"

echo "Deployment cycle completed. Happy developing!"
