# Baut Monokel als einzelne, eigenständige EXE nach publish/<Runtime>/ (Aufruf: pwsh ./publish.ps1).
param(
    [string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src\Monokel\Monokel.csproj'
$output = Join-Path $PSScriptRoot "publish\$Runtime"

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $output
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish fehlgeschlagen (Exit-Code $LASTEXITCODE)"
}

Get-Item (Join-Path $output 'Monokel.exe') | Select-Object FullName, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
