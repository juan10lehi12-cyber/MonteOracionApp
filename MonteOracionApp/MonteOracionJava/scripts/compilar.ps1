param([switch]$SinPruebas)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$mavenCommand = Get-Command mvn.cmd -ErrorAction SilentlyContinue
$mavenPath = if ($mavenCommand) { $mavenCommand.Source } else { 'C:\Program Files\Apache NetBeans\java\maven\bin\mvn.cmd' }
if (-not (Test-Path -LiteralPath $mavenPath)) { throw 'No se encontró Maven. Configura Maven en PATH o compila desde NetBeans.' }
Push-Location $projectPath
try {
    $mavenArgs = @('-B', 'package')
    if ($SinPruebas) { $mavenArgs += '-DskipTests' }
    & $mavenPath @mavenArgs
    if ($LASTEXITCODE -ne 0) { throw 'La compilación falló. Revisa la salida de Maven.' }
    Write-Host "WAR: $projectPath\target\MonteOracionJava.war"
} finally { Pop-Location }
