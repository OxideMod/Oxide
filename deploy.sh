if [ "$TRAVIS_PULL_REQUEST" == "false" ]; then
    echo -e "Changing directory to $HOME and configuring git"
    cd $HOME
    git config --global user.email "travis@travis-ci.org"
    git config --global user.name "Travis"

    echo -e "Cloning snapshots branch using token..."
    git clone --quiet --branch=snapshots https://$GITHUB_TOKEN@github.com/$TRAVIS_REPO_SLUG.git snapshots > /dev/null

    echo -e "Copying target files to temp directory..."
    mkdir -p $HOME/temp/RustDedicated_Data/Managed
    cd $HOME/build/$TRAVIS_REPO_SLUG
    cp -vf Oxide.Core/bin/Release/Oxide.Core.dll $HOME/temp/RustDedicated_Data/Managed/Oxide.Core.dll
    cp -vf Oxide.Ext.Lua/bin/Release/Oxide.Ext.Lua.dll $HOME/temp/RustDedicated_Data/Managed/Oxide.Ext.Lua.dll
    cp -vf Oxide.Ext.Rust/bin/Release/Oxide.Ext.Rust.dll $HOME/temp/RustDedicated_Data/Managed/Oxide.Ext.Rust.dll
    cp -vf Oxide.Ext.Unity/bin/Release/Oxide.Ext.Unity.dll $HOME/temp/RustDedicated_Data/Managed/Oxide.Ext.Unity.dll
    cp -vf Dependencies/lua52.dll $HOME/temp/lua52.dll
    cp -vf Dependencies/KeraLua.dll $HOME/temp/RustDedicated_Data/Managed/KeraLua.dll
    cp -vf Dependencies/KopiLua.dll $HOME/temp/RustDedicated_Data/Managed/KopiLua.dll
    cp -vf Dependencies/Newtonsoft.Json.dll $HOME/temp/RustDedicated_Data/Managed/Newtonsoft.Json.dll
    cp -vf Dependencies/NLua.dll $HOME/temp/RustDedicated_Data/Managed/NLua.dll
    cp -vf Patched/Assembly-CSharp.dll $HOME/temp/RustDedicated_Data/Managed/Assembly-CSharp.dll
    cp -vf Patched/Facepunch.dll $HOME/temp/RustDedicated_Data/Managed/Facepunch.dll
    cp -vf oxide.root.json $HOME/temp/oxide.root.json

    RUST_VERSION=`cat Patched/version.txt`
    echo "Oxide 2 build $TRAVIS_BUILD_NUMBER for Rust server $RUST_VERSION, built on `date +%Y%d%m` via Travis-CI" >> $HOME/temp/version.txt

    echo -e "Archiving and compressing target files..."
    cd $HOME/temp
    mkdir -p $HOME/snapshots/${RUST_VERSION}
    zip -vr9 $HOME/snapshots/${RUST_VERSION}/oxide-2_b${TRAVIS_BUILD_NUMBER}-`date +%Y%d%m`.zip .

    echo -e "Adding, committing, and pushing to snapshots branch..."
    cd $HOME/snapshots
    git add -vf .
    git commit -m "Oxide 2 build $TRAVIS_BUILD_NUMBER for Rust server $RUST_VERSION"
    git push -q origin snapshots > /dev/null
fi
