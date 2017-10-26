param (
    [Parameter(Mandatory=$true)][string]$project,
    [Parameter(Mandatory=$true)][string]$dotnet,
    [Parameter(Mandatory=$true)][string]$managed,
    [string]$appid = "0",
    [string]$branch = "public",
    [string]$depot = "",
    [string]$access = "anonymous",
    [string]$deobf = ""
)

Clear-Host

# Format game name and set depot ID if provided
$game_name = $project -Replace "Oxide."
if ($depot) { $depot = "-depot $depot" }

# Set directory variables and create directories
$root_dir = $PSScriptRoot
$deps_dir = "$root_dir\Games\Dependencies"
$depot_dir = "$deps_dir\.DepotDownloader"
$project_dir = "$root_dir\Games\$project"
if ("$branch" -ne "public" -and (Test-Path "$project_dir\$game_name-$branch.opj")) {
    $opj_name = "$project_dir\$game_name-$branch"
    $patch_dir = "$deps_dir\$project-$branch"
} else {
    $opj_name = "$project_dir\$game_name"
    $patch_dir = "$deps_dir\$project"
}
$managed_dir = "$patch_dir\$managed"
New-Item "$depot_dir", "$patch_dir", "$managed_dir" -ItemType Directory -Force

function Find-Dependencies {
    # Check if project file exists for game
    if (!(Test-Path "$game_name.csproj")) {
        Write-Host "Could not find a .csproj file for $game_name"
        exit 1
    }

    # Copy any local dependencies
    if (Test-Path "$project_dir\Dependencies") {
        Copy-Item "$project_dir\Dependencies\*" "$managed_dir" -Force
    }

    # Check if Steam is used for game dependencies
    if ($access.ToLower() -ne "nosteam") {
        # Get project information from .csproj file
        $csproj = Get-Item "$game_name.csproj"
        $xml = [xml](Get-Content $csproj)
        Write-Host "Getting references for $branch branch of $appid"
        try {
            # TODO: Exclude dependencies included in repository
            $hint_path = "\.\.\\Dependencies\\\$\(PackageId\)\\\$\(ManagedDir\)\\"
            ($xml.selectNodes("//Reference") | Select-Object HintPath -ExpandProperty HintPath | Out-String) -Replace $hint_path | Out-File "$patch_dir\.references"
        } catch {
            Write-Host "Could not get references or none found in $game_name.csproj"
            Write-Host $_.Exception.Message
            exit 1
        }

        Get-Downloader
    }
}

function Get-Downloader {
    # Get latest release info for DepotDownloader
    Write-Host "Determining latest release of DepotDownloader"
    try {
        $json = (Invoke-WebRequest "https://api.github.com/repos/SteamRE/DepotDownloader/releases" | ConvertFrom-Json)[0]
        $version = $json.tag_name -Replace '\w+(\d+(?:\.\d+)+)', '$1'
        $release_zip = $json.assets[0].name
    } catch {
        Write-Host "Could not get DepotDownloader information from GitHub"
        Write-Host $_.Exception.Message
        exit 1
    }

    # Check if latest DepotDownloader is already downloaded
    if (!(Test-Path "$depot_dir\$release_zip") -or !(Test-Path "$depot_dir\DepotDownloader.exe")) {
        # Download and extract DepotDownloader
        Write-Host "Downloading version $version of DepotDownloader"
        try {
            Invoke-WebRequest $json.assets[0].browser_download_url -Out "$depot_dir\$release_zip"
        } catch {
            Write-Host "Could not download DepotDownloader from GitHub"
            Write-Host $_.Exception.Message
            exit 1
        }

        # TODO: Compare size and hash of .zip vs. what GitHub has via API
        Write-Host "Extracting DepotDownloader release files"
        Expand-Archive "$depot_dir\$release_zip" -DestinationPath "$depot_dir" -Force

        if (!(Test-Path "$depot_dir\DepotDownloader.exe")) {
            Get-Downloader # TODO: Add infinite loop prevention
            return
        }

        # TODO: Cleanup old version .zip file(s)
        #Remove-Item "$depot_dir\depotdownloader-*.zip" -Exclude "$depot_dir\$release_zip" -Verbose –Force
    } else {
        Write-Host "Latest version ($version) of DepotDownloader already downloaded"
    }

    Get-Dependencies
}

function Get-Dependencies {
    # TODO: Check for and compare Steam buildid before downloading again
    # TODO: Add handling for SteamGuard code entry/use

    # Check if Steam login information is required or not
    if ($access.ToLower() -ne "anonymous") {
        if (Test-Path "$root_dir\.steamlogin") {
            $steam_login = Get-Content "$root_dir\.steamlogin"
            if ($steam_login.Length -ne 2) {
                Write-Host "Steam username AND password not set in .steamlogin file"
                exit 1
            } else {
                $login = "-username $($steam_login[0]) -password $($steam_login[1])"
            }
        } elseif ($env:STEAM_USERNAME -and $env:STEAM_PASSWORD) {
            $login = "-username $env:STEAM_USERNAME -password $env:STEAM_PASSWORD"
        } else {
            Write-Host "No Steam credentials found, skipping build for $game"
            exit 1
        }
    }

    # Cleanup existing game files, else they aren't always the latest
    #Remove-Item $managed_dir -Include *.dll, *.exe -Exclude "Oxide.Core.dll" -Verbose –Force

    # Attempt to run DepotDownloader to get game DLLs
    try {
        Start-Process "$depot_dir\DepotDownloader.exe" -ArgumentList "$login -app $appid -branch $branch $depot -dir $patch_dir -filelist $patch_dir\.references" -NoNewWindow -Wait
    } catch {
        Write-Host "Could not start or complete DepotDownloader process"
        Write-Host $_.Exception.Message
        exit 1
    }

    # TODO: Store Steam buildid somewhere for comparison during next check
    # TODO: Confirm all dependencies were downloaded (no 0kb files), else stop/retry and error with details

    # TODO: Check Oxide.Core.dll version and update if needed
    if (!(Test-Path "Dependencies\$managed\Oxide.Core.dll")) {
        # Grab latest Oxide.Core.dll build
        Write-Host "Grabbing latest build of Oxide.Core.dll"
        #$core_version = Get-ChildItem -Directory $core_path | Where-Object { $_.PSIsContainer } | Sort-Object CreationTime -desc | Select-Object -f 1
        try {
            Copy-Item "..\..\Oxide.Core\bin\Release\$dotnet\Oxide.Core.dll" "$deps_dir" -Force
            Copy-Item "..\..\Oxide.Core\bin\Release\$dotnet\Oxide.Core.dll" "$managed_dir" -Force
        } catch {
            Write-Host "Could not copy Oxide.Core.dll from Dependencies\$managed"
            Write-Host $_.Exception.Message
            exit 1
        }
    }

    # TODO: Copy websocket-csharp.dll to Dependencies\*Managed
}

function Get-Deobfuscators {
    # TODO: Run `de4dot -d -r c:\input` to detect obfuscation type?

    # Check for which deobfuscator to get and use
    if ($deobf.ToLower() -eq "de4dot") {
        $de4dot_dir = "$deps_dir\.de4dot"
        New-Item "$de4dot_dir" -ItemType Directory -Force

        # Check if latest de4dot is already downloaded
        if (!(Test-Path "$de4dot_dir\de4dot.zip") -or !(Test-Path "$de4dot_dir\de4dot.exe")) {
            # Download and extract de4dot
            Write-Host "Downloading version of de4dot" # TODO: Get and show version
            try {
                Invoke-WebRequest "https://ci.appveyor.com/api/projects/0xd4d/de4dot/artifacts/de4dot.zip" -Out "$de4dot_dir\de4dot.zip"
            } catch {
                Write-Host "Could not download de4dot from AppVeyor"
                Write-Host $_.Exception.Message
                exit 1
            }

            # TODO: Compare size and hash of .zip vs. what AppVeyor has via API
            Write-Host "Extracting de4dot release files"
            Expand-Archive "$de4dot_dir\de4dot.zip" -DestinationPath "$de4dot_dir" -Force

            if (!(Test-Path "$de4dot_dir\de4dot.exe")) {
                Get-Deobfuscators # TODO: Add infinite loop prevention
                return
            }
        } else {
            Write-Host "Latest version of de4dot already downloaded"
        }

        Start-Deobfuscator
    }
}

function Start-Deobfuscator {
    if ($deobf.ToLower() -eq "de4dot") {
        # Attempt to deobfuscate game file(s)
        try {
            Start-Process "$deps_dir\.de4dot\de4dot.exe" -WorkingDirectory "$managed_dir" -ArgumentList "-r $managed_dir -ru" -NoNewWindow -Wait
        } catch {
            Write-Host "Could not start or complete de4dot deobufcation process"
            Write-Host $_.Exception.Message
            exit 1
        }

        # Remove obfuscated file(s) and replace with cleaned file(s)
        Get-ChildItem "$managed_dir\*-cleaned.*" -Recurse | ForEach-Object {
            Remove-Item $_.FullName.Replace("-cleaned", "")
            Rename-Item $_ $_.Name.Replace("-cleaned", "")
        }
    }
}

function Get-Patcher {
    # TODO: MD5 comparision of local OxidePatcher.exe and remote header
    if (!(Test-Path "$deps_dir\OxidePatcher.exe")) {
        # Download latest Oxide Patcher build
        Write-Host "Downloading latest build of OxidePatcher"
        $patcher_url = "https://github.com/OxideMod/OxidePatcher/releases/download/latest/OxidePatcher.exe"
        # TODO: Only download patcher once in $patch_dir, then copy to $managed_dir for each game
        try {
            Invoke-WebRequest $patcher_url -Out "$deps_dir\OxidePatcher.exe"
        } catch {
            Write-Host "Could not download OxidePatcher.exe from GitHub"
            Write-Host $_.Exception.Message
            exit 1
        }
    } else {
        Write-Host "Latest build of OxidePatcher already downloaded"
    }

    Start-Patcher
}

function Start-Patcher {
    # Check if we need to get the Oxide patcher
    if (!(Test-Path "$deps_dir\OxidePatcher.exe")) {
        Get-Patcher # TODO: Add infinite loop prevention
        return
    }

    # TODO: Make sure dependencies exist before trying to patch

    # Attempt to patch game using the Oxide patcher
    try {
        Start-Process "$deps_dir\OxidePatcher.exe" -WorkingDirectory "$managed_dir" -ArgumentList "-c -p `"$managed_dir`" $opj_name.opj" -NoNewWindow -Wait
    } catch {
        Write-Host "Could not start or complete OxidePatcher process"
        Write-Host $_.Exception.Message
        exit 1
    }
}

Find-Dependencies
Get-Deobfuscators
Get-Patcher
