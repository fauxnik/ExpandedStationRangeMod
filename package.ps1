param (
	[switch]$NoArchive,
	[string]$OutputDirectory = $PSScriptRoot
)

Set-Location "$PSScriptRoot"
$FilesToInclude = "info.json","build/*","LICENSE"

$modInfo = Get-Content -Raw -Path "info.json" | ConvertFrom-Json
$modName = $modInfo.DisplayName -replace "\s",""
$modVersion = $modInfo.Version

$DistDir = "$OutputDirectory/dist"
if ($NoArchive) {
	$ZipWorkDir = "$OutputDirectory"
} else {
	$ZipWorkDir = "$DistDir/tmp"
}
$ZipOutDir = "$ZipWorkDir/$modName"

New-Item "$ZipOutDir" -ItemType Directory -Force
Copy-Item -Force -Path $FilesToInclude -Destination "$ZipOutDir"

if (!$NoArchive)
{
	$FILE_NAME = "$DistDir/${modName}_v$modVersion.zip"
	Compress-Archive -Update -CompressionLevel Fastest -Path "$ZipOutDir/*" -DestinationPath "$FILE_NAME"
}
