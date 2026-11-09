param(
    [Parameter(Mandatory)]
    [ValidatePattern('^v?\d+(?:\.\d+){1,3}(?:-[0-9A-Za-z.-]+)?$')]
    [string] $Tag,
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-f]{7,64}$')]
    [string] $Commit,
    [string] $ArtifactsDirectory = 'artifacts'
)

$ErrorActionPreference = 'Stop'
$repo = Join-Path $PSScriptRoot '..'
$project = Join-Path $repo 'BassNoteFinder/BassNoteFinder.csproj'
$version = $Tag -creplace '^v', ''
$shortCommit = $Commit.Substring(0, 7)
$ArtifactsDirectory = [IO.Path]::GetFullPath($ArtifactsDirectory, (Get-Location).Path)
$buildDirectory = Join-Path $ArtifactsDirectory "build/$Tag"
$publishDirectory = Join-Path $ArtifactsDirectory "publish/$Tag/win-x64-framework-dependent"
$zipPath = Join-Path $ArtifactsDirectory "BassNoteFinder-$Tag-win-x64-framework-dependent.zip"

if ((Test-Path -LiteralPath $buildDirectory) -or (Test-Path -LiteralPath $publishDirectory)) {
    throw "Build and publish directories must be fresh for $Tag."
}

$buildProperties = @(
    '-p:Configuration=Release'
    '-p:RuntimeIdentifier=win-x64'
    '-p:SelfContained=false'
    '-p:PublishSingleFile=true'
    "-p:ArtifactsPath=$buildDirectory"
)

dotnet publish $project @buildProperties -p:DebugType=None -p:DebugSymbols=false `
    "-p:Version=$version" "-p:InformationalVersion=$version+sha.$shortCommit" `
    --output $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw 'The framework-dependent publish failed.'
}

$buildJson = dotnet msbuild $project @buildProperties -nologo -verbosity:quiet -t:ResolveFrameworkReferences `
    -getItem:ResolvedAppHostPack -getProperty:ProjectRuntimeConfigFilePath
if ($LASTEXITCODE -ne 0) {
    throw 'Could not resolve the .NET executable host package.'
}

$build = ($buildJson -join "`n") | ConvertFrom-Json
$runtime = (Get-Content -Raw -LiteralPath $build.Properties.ProjectRuntimeConfigFilePath | ConvertFrom-Json).runtimeOptions
$otherBinaries = Get-ChildItem -LiteralPath $publishDirectory -File -Recurse |
    Where-Object { $_.Extension -in '.dll', '.exe' -and $_.Name -ne 'BassNoteFinder.exe' }
if ($runtime.includedFrameworks -or 'Microsoft.WindowsDesktop.App' -notin $runtime.frameworks.name -or $otherBinaries) {
    throw 'Expected a single-file app that uses the installed Desktop Runtime.'
}

$hostPacks = @($build.Items.ResolvedAppHostPack)
if ($hostPacks.Count -ne 1 -or !$hostPacks[0].PackageDirectory) {
    throw 'Expected one resolved .NET executable host package.'
}

# The SDK installation may contain the host binaries without the NuGet notice files.
$hostPackVersion = Split-Path $hostPacks[0].PackageDirectory -Leaf
$packageId = 'microsoft.netcore.app.host.win-x64'
$packageFile = Join-Path $ArtifactsDirectory "$packageId.$hostPackVersion.nupkg"
$packageUrl = "https://api.nuget.org/v3-flatcontainer/$packageId/$hostPackVersion/$packageId.$hostPackVersion.nupkg"
Invoke-WebRequest -Uri $packageUrl -OutFile $packageFile -TimeoutSec 60

$licenseDirectory = New-Item -ItemType Directory -Path (Join-Path $publishDirectory 'licenses')
Copy-Item -LiteralPath (Join-Path $repo 'licenses/NAudio-2.3.0.txt') -Destination $licenseDirectory.FullName
$hostPackage = [IO.Compression.ZipFile]::OpenRead($packageFile)
try {
    foreach ($file in 'LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT') {
        $entry = $hostPackage.GetEntry($file)
        if ($null -eq $entry -or $entry.Length -eq 0) {
            throw "Missing $file in the .NET host package."
        }
        $destination = Join-Path $licenseDirectory.FullName "DotNet-AppHost-$file"
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination)
    }
}
finally {
    $hostPackage.Dispose()
}

Copy-Item -LiteralPath (Join-Path $repo 'THIRD-PARTY-NOTICES.txt') -Destination $publishDirectory
Copy-Item -LiteralPath (Join-Path $repo 'RELEASE_README.md') -Destination (Join-Path $publishDirectory 'README.md')
@'
Bass Note Finder requires the Microsoft .NET Desktop Runtime 10.x (Windows x64).
If it is missing, open BassNoteFinder.exe and follow the .NET download prompt,
or download the Desktop Runtime from https://dotnet.microsoft.com/download/dotnet/10.0.
After installing it, open BassNoteFinder.exe again.
'@ | Set-Content -LiteralPath (Join-Path $publishDirectory 'RUNTIME_REQUIRED.txt')

Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $zipPath -Force
