<#
SYNOPSIS
    Buils the optionally pushes the uSync packages.
#>
param (

    [Parameter(Mandatory)]
    [string]
    [Alias("v")]  $version, #version to build

    [Parameter()]
    [string]
    $suffix, # optional suffix to append to version (for pre-releases)

    [Parameter()]
    [string]
    $env = 'release', #build environment to use when packing

    [Parameter()]
    [switch]
    $push=$false #push to devops nightly feed
)

if ($version.IndexOf('-') -ne -1) {
    Write-Host "Version shouldn't contain a - (remember version and suffix are seperate)"
    exit
}

$fullVersion = $version;

if (![string]::IsNullOrWhiteSpace($suffix)) {
   $fullVersion = -join($version, '-', $suffix)
}

$majorFolder = $version.Substring(0, $version.LastIndexOf('.'))

$outFolder = ".\$majorFolder\$version\$fullVersion"
if (![string]::IsNullOrWhiteSpace($suffix)) {
    $suffixFolder = $suffix;
    if ($suffix.IndexOf('.') -ne -1) {
        $suffixFolder = $suffix.substring(0, $suffix.indexOf('.'))
    }
    $outFolder = ".\$majorFolder\$version\$version-$suffixFolder\$fullVersion"
}

"----------------------------------"
Write-Host "Version  :" $fullVersion
Write-Host "Config   :" $env
Write-Host "Folder   :" $outFolder
"----------------------------------"; ""

$buildParams = "ContinuousIntegrationBuild=true,version=$fullVersion"

# the Complete project references the main one, so this builds both.
dotnet build ..\src\uSync.Umbraco.Commerce.Complete\uSync.Umbraco.Commerce.Complete.csproj -c $env

""; "##### Packaging"; "----------------------------------" ; ""

dotnet pack ..\src\uSync.Umbraco.Commerce\uSync.Umbraco.Commerce.csproj --no-restore -c $env -o $outFolder /p:$buildParams
dotnet pack ..\src\uSync.Umbraco.Commerce.Complete\uSync.Umbraco.Commerce.Complete.csproj --no-restore -c $env -o $outFolder /p:$buildParams

""; "##### Copying to LocalGit folder"; "----------------------------------" ; ""
XCOPY "$outFolder\*.nupkg" "C:\Source\localgit" /Q /Y 

if ($push) {
    ""; "##### Pushing to our nighly package feed"; "----------------------------------" ; ""
    &nuget.exe push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}

Write-Host "uSync.Umbraco.Commerce Packaged : $fullVersion"