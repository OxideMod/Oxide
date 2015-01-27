#!/bin/bash

function die_with() { echo "$*" >&2; exit 1; }

echo "Checking if commit is a pull request"
if [ $TRAVIS_PULL_REQUEST == true ]; then die_with "Skipping deployment for pull request!"; fi

echo "Configuring git credentials"
git config --global user.email "travis@travis-ci.org" && git config --global user.name "Travis" || die_with "Failed to configure git credentials!"

echo "Cloning snapshots branch using token"
git clone -q --branch=snapshots https://$GITHUB_TOKEN@github.com/$TRAVIS_REPO_SLUG.git $HOME/snapshots >/dev/null || die_with "Failed to clone existing snapshots branch!"

echo "Changing directory to $HOME creating directories"
cd $HOME/build/$TRAVIS_REPO_SLUG || die_with "Failed to change to project home!"
mkdir -p $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to create directories!"

echo "Copying target files to temp directory"
cp -f Oxide.Core/bin/Release/Oxide.Core.dll \
Oxide.Ext.CSharp/bin/Release/Oxide.Ext.CSharp.dll \
Oxide.Ext.JavaScript/bin/Release/Oxide.Ext.JavaScript.dll \
Oxide.Ext.Lua/bin/Release/Oxide.Ext.Lua.dll \
Oxide.Ext.Python/bin/Release/Oxide.Ext.Python.dll \
Oxide.Ext.Rust/bin/Release/Oxide.Ext.Rust.dll \
Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll \
$HOME/temp/RustDedicated_Data/Managed || die_with "Failed to copy core and extension DLLs!"

cd Dependencies || die_with "Failed to change to dependencies directory!"
cp -f IronPython.dll Jint.dll *Lua.dll Microsoft.Dynamic.dll Microsoft.Scripting*.dll Mono.*.dll $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to copy dependency DLLs!"

cd ../ || die_with "Failed to change to project home!"
cp -f Patched/Assembly-CSharp.dll Patched/Facepunch.dll $HOME/temp/RustDedicated_Data/Managed || die_with "Failed to copy patched Rust server files!"
cp -f oxide.root.json Dependencies/lua5*.dll $HOME/temp || die_with "Failed to copy config file and lua DLLs!"

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
