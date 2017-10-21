param (
    [Parameter(Mandatory=$true)][string]$project,
    [Parameter(Mandatory=$true)][string]$dotnet,
    [Parameter(Mandatory=$true)][string]$appid,
    [Parameter(Mandatory=$true)][string]$managed,
    [string]$branch = "public",
    [string]$depot = "",
    [string]$access = "anonymous"
)

Clear-Host
$game_name = $project -Replace "Oxide."
if ($depot) { $depot = "-depot $depot" }
$depot_dir = "$PSScriptRoot\Games\Dependencies\.DepotDownloader"
New-Item $depot_dir -ItemType Directory -Force
$patch_dir = "$PSScriptRoot\Games\Dependencies\$project"
if ($branch -ne "public") { $patch_dir = "$patch_dir-$branch" }
New-Item $patch_dir -ItemType Directory -Force
$managed_dir = "$patch_dir\$managed"
New-Item $managed_dir -ItemType Directory -Force

function Find-Dependencies {
    if (!(Test-Path "$game_name.csproj")) {
        Write-Host "Could not find a .csproj file for $game_name"
        exit 1
    }

    # Get project information from .csproj file
    $csproj = Get-Item "$game_name.csproj"
    $xml = [xml](Get-Content $csproj)
    Write-Host "Getting references for $branch branch of $appid"
    try {
        # TODO: Exclude dependencies included in repository
        $hint_path = "\.\.\\Dependencies\\\$\(PackageId\)\\\$\(ManagedDir\)\\"
        ($xml.selectNodes("//Reference") | Select-Object HintPath -ExpandProperty HintPath | Out-String) -Replace $hint_path | Out-File "$patch_dir\.references"
    } catch {
        Write-Host "Failed to get references or none found in $game_name.csproj"
        Write-Host $_.Exception.Message
        exit 1
    }
}

function Get-Downloader {
    # Get latest release info for DepotDownloader
    Write-Host "Determining latest release of DepotDownloader"
    $json = (Invoke-WebRequest "https://api.github.com/repos/SteamRE/DepotDownloader/releases" | ConvertFrom-Json)[0]
    $version = $json.tag_name -Replace '\w+(\d+(?:\.\d+)+)', '$1'
    $release_zip = $json.assets[0].name

    if (!(Test-Path "$depot_dir\$release_zip") -or !(Test-Path "$depot_dir\DepotDownloader.exe")) {
        # Download and extract DepotDownloader
        Write-Host "Dowloading version $version of DepotDownloader"
        Invoke-WebRequest $json.assets[0].browser_download_url -Out "$depot_dir\$release_zip"
        Write-Host "Extracting DepotDownloader release files"
        Expand-Archive "$depot_dir\$release_zip" -DestinationPath $depot_dir -Force
        # TODO: Cleanup old version .zip file(s)
        #Remove-Item "$depot_dir\depotdownloader-*.zip" -Exclude "$depot_dir\$release_zip" -Verbose –Force
    } else {
        Write-Host "Latest version ($version) of DepotDownloader already downloaded"
    }
}

function Get-Dependencies {
    # TODO: Check for and compare Steam buildid before downloading again

    if ($access.ToLower() -ne "anonymous") {
        $steam_login = Get-Content "$PSScriptRoot\.steamlogin"
        if ($steam_login.Length -ne 2) {
            Write-Host "Steam username AND password not set in .steamlogin file"
            exit 1
        }

        $login = "-username $($steam_login[0]) -password $($steam_login[1])"
    }

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
        Copy-Item "..\..\Oxide.Core\bin\Release\$dotnet\Oxide.Core.dll" $managed_dir -Force
        # TODO: Copy websocket-csharp.dll to Dependencies\*Managed
    }
    # TODO: Return and error if Oxide.Core.dll still doesn't exist
}

function Get-Patcher {
    # TODO: MD5 comparision of local OxidePatcher.exe and remote header
    if (!(Test-Path "$managed_dir\OxidePatcher.exe")) {
        # Download latest Oxide Patcher build
        Write-Host "Dowloading latest build of OxidePatcher"
        $patcher_url = "https://github.com/OxideMod/OxidePatcher/releases/download/latest/OxidePatcher.exe"
        # TODO: Only download patcher once in $patch_dir, then copy to $managed_dir for each game
        Invoke-WebRequest $patcher_url -Out "$managed_dir\OxidePatcher.exe"
    } else {
        Write-Host "Latest build of OxidePatcher already downloaded"
    }
}

function Start-Patcher {
    if (!(Test-Path "$managed_dir\OxidePatcher.exe")) {
        Get-Patcher
        return
    }

    # Patch game using OxidePatcher.exe
    try {
        $opj_name = "$PSScriptRoot\Games\$project\$game_name"
        if ($branch -ne "public") { $opj_name = "$opj_name-$branch" }
        Start-Process "$managed_dir\OxidePatcher.exe" -WorkingDirectory $managed_dir -ArgumentList "-c -p $managed_dir $opj_name.opj" -NoNewWindow -Wait
    } catch {
        Write-Host "Could not start or complete OxidePatcher process"
        Write-Host $_.Exception.Message
        exit 1
    }
}

Find-Dependencies
Get-Downloader
Get-Dependencies
Get-Patcher
Start-Patcher
