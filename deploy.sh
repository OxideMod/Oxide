#!/bin/bash

function die_with() { echo "$*" >&2; exit 1; }

echo "Checking if commit is a pull request"
if [ $TRAVIS_PULL_REQUEST == true ]; then die_with "Skipping deployment for pull request!"; fi

echo "Configuring git credentials"
git config --global user.email "travis@travis-ci.org" && git config --global user.name "Travis" || die_with "Failed to configure git credentials!"

echo "Changing directory to $HOME and configuring git"
cd $HOME || die_with "Failed to change to home directory!"

echo "Cloning snapshots branch using token"
git clone -q --branch=snapshots https://$GITHUB_TOKEN@github.com/$TRAVIS_REPO_SLUG.git snapshots >/dev/null || die_with "Failed to clone existing snapshots branch!"

echo "Copying target files to temp directory"
mkdir -p $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to create directories!"
cd $HOME/build/$TRAVIS_REPO_SLUG || die_with "Failed to change to build directory!"
cp -f Oxide.Core/bin/Release/Oxide.Core.dll Oxide.Ext.Rust/bin/Release/Oxide.Ext.Rust.dll Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to copy Oxide, Rust, and Unity DLLs!"
cp -f Oxide.Ext.JavaScript/bin/Release/Oxide.Ext.JavaScript.dll Oxide.Ext.Lua/bin/Release/Oxide.Ext.Lua.dll Oxide.Ext.Python/bin/Release/Oxide.Ext.Python.dll $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to copy plugin extension DLLs!"
cp -f Dependencies/IronPython.dll Dependencies/Jint.dll Dependencies/KeraLua.dll Dependencies/Microsoft.Dynamic.dll Dependencies/Microsoft.Scripting.Core.dll Dependencies/Microsoft.Scripting.dll Dependencies/NLua.dll $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to copy dependency DLLs!"
cp -f Patched/Assembly-CSharp.dll Patched/Facepunch.dll $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to copy patched Rust server files!"
cp -f oxide.root.json Dependencies/lua51.dll Dependencies/lua52.dll $HOME/temp || die_with "Failed to copy oxide.root.json, lua51.dll, and lua52.dll!"

RUST_VERSION=`cat Patched/version.txt` && echo "Oxide 2 build $TRAVIS_BUILD_NUMBER for Rust server $RUST_VERSION" >>$HOME/temp/version.txt || die_with "Failed to update version file!"

echo "Archiving and compressing target files"
cd $HOME/temp || die_with "Failed to change to temp directory!"
mkdir -p $HOME/snapshots/public/$RUST_VERSION || die_with "Failed to create snapshot version directory!"
zip -vr9 $HOME/snapshots/public/$RUST_VERSION/oxide-2.0.$TRAVIS_BUILD_NUMBER-$RUST_VERSION.zip . || die_with "Failed to archive snapshot files!"
cp -f $HOME/snapshots/public/$RUST_VERSION/oxide-2.0.$TRAVIS_BUILD_NUMBER-$RUST_VERSION.zip $HOME/snapshots/public/latest.zip || die_with "Failed to create latest archive copy!"

echo "Adding, committing, and pushing to snapshots branch"
cd $HOME/snapshots || die_with "Failed to change to snapshots directory!"
git add -f . && git commit -m "Oxide 2 build $TRAVIS_BUILD_NUMBER for Rust server $RUST_VERSION" || die_with "Failed to add and commit files with git!"
git push -qf origin snapshots >/dev/null || die_with "Failed to push snapshot to GitHub!"

echo "Deployment cycle completed. Project is now at version $RUST_VERSION. Happy developing!"
