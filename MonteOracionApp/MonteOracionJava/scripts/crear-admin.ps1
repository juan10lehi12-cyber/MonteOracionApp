param([string]$Config)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
if (-not $Config) { $Config = Join-Path $projectPath 'config\database.properties' }
if (-not (Test-Path -LiteralPath $Config)) { throw 'Copia config/database.properties.example a database.properties y configura tu usuario de MariaDB.' }
if (-not (Test-Path -LiteralPath (Join-Path $projectPath 'target\classes'))) { throw 'Primero ejecuta scripts/compilar.ps1.' }
$configPath = (Resolve-Path -LiteralPath $Config).Path
$classPath = "$projectPath\target\classes;$projectPath\target\MonteOracionJava\WEB-INF\lib\*"
& java "-Dmonte.config=$configPath" -cp $classPath pe.monteoracion.AdminCli
if ($LASTEXITCODE -ne 0) { throw 'No se creó la cuenta. Revisa la conexión y que el correo no exista.' }
