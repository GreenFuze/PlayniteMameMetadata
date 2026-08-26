param(
    [Parameter(Mandatory = $true)]
    [string]$ToolboxPath
)

$ErrorActionPreference = "Stop"
$repositoryRoot = $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "src\PlayniteMameMetadata\PlayniteMameMetadata.csproj"
$buildOutput = Join-Path $repositoryRoot "src\PlayniteMameMetadata\bin\Release\net462"
$artifactsDirectory = Join-Path $repositoryRoot "artifacts"
$stagingDirectory = Join-Path $artifactsDirectory "MameDatMetadata_0_1_0"
$artifactsFullPath = [System.IO.Path]::GetFullPath($artifactsDirectory).TrimEnd('\') + '\'
$stagingFullPath = [System.IO.Path]::GetFullPath($stagingDirectory)

if (-not $stagingFullPath.StartsWith($artifactsFullPath, [System.StringComparison]::OrdinalIgnoreCase))
{
    throw "Package staging must remain below the repository artifacts directory."
}

if (-not (Test-Path -LiteralPath $ToolboxPath -PathType Leaf))
{
    throw "Playnite Toolbox was not found at '$ToolboxPath'."
}

dotnet build $projectPath --configuration Release
if ($LASTEXITCODE -ne 0)
{
    throw "Release build failed."
}

if (Test-Path -LiteralPath $stagingDirectory)
{
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stagingDirectory "Resources") | Out-Null

Copy-Item -LiteralPath (Join-Path $buildOutput "PlayniteMameMetadata.dll") -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $buildOutput "extension.yaml") -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $buildOutput "LICENSE") -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $buildOutput "NOTICE") -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $buildOutput "Resources\mame-metadata.png") -Destination (Join-Path $stagingDirectory "Resources")

& $ToolboxPath pack $stagingDirectory $artifactsDirectory
if ($LASTEXITCODE -ne 0)
{
    throw "Playnite Toolbox packaging failed."
}

$toolboxPackage = Join-Path $artifactsDirectory "MameMetadata_0d873564-ca47-40b3-a77d-fb8b2afe2fdd_0_1_0.pext"
$releasePackage = Join-Path $artifactsDirectory "MameDatMetadata_0_1_0.pext"
if (-not (Test-Path -LiteralPath $toolboxPackage -PathType Leaf))
{
    throw "Playnite Toolbox did not create the expected package."
}

Move-Item -LiteralPath $toolboxPackage -Destination $releasePackage -Force
Write-Host "Release package created at $releasePackage"
