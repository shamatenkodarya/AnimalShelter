$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command javac -ErrorAction SilentlyContinue)) {
    throw 'JDK 17 or newer is required. Add javac to PATH.'
}

$source = Join-Path $PSScriptRoot 'java/src/ru/dobrydrug/Main.java'
$seedSource = Join-Path $PSScriptRoot 'java/src/ru/dobrydrug/SeedData.java'
$output = Join-Path $PSScriptRoot 'java/out'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& javac -encoding UTF-8 -d $output $source $seedSource
if ($LASTEXITCODE -ne 0) { throw 'Java compilation failed.' }

& java -cp $output ru.dobrydrug.Main
