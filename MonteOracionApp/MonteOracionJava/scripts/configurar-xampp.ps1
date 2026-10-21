param(
    [Parameter(Mandatory=$true)][string]$TomcatHome,
    [string]$MysqlHome = 'C:\xampp\mysql',
    [int]$Puerto = 3306
)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$mysqlPath = Join-Path $MysqlHome 'bin\mysql.exe'
$configPath = Join-Path $projectPath 'config\database.properties'
$serverConfigFolder = Join-Path $TomcatHome 'conf\MonteOracionJava'
if (-not (Test-Path -LiteralPath $mysqlPath)) { throw 'No se encontró mysql.exe de XAMPP.' }
if (-not (Test-Path -LiteralPath (Join-Path $TomcatHome 'conf\server.xml'))) { throw 'Indica la carpeta del Tomcat que utiliza NetBeans.' }
if (Test-Path -LiteralPath $configPath) { throw 'Ya existe config/database.properties. Usa esa configuración; no se reemplazará su contraseña.' }
# Solo para XAMPP local con administración root sin contraseña. No modifica cuentas existentes.
$checksSql = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='monte_oracion_java' AND TABLE_NAME IN ('iglesia','personal','donacion'); SELECT COUNT(*) FROM mysql.user WHERE User='monte_app';"
$checks = @($checksSql | & $mysqlPath --protocol=tcp --host=127.0.0.1 "--port=$Puerto" --user=root --batch --skip-column-names)
if ($LASTEXITCODE -ne 0) { throw 'No se pudo acceder a XAMPP local. Configura la conexión manualmente si root tiene contraseña.' }
if ($checks.Count -ne 2 -or $checks[0].Trim() -ne '3') { throw 'Primero importa sql/01_esquema.sql en phpMyAdmin.' }
if ($checks[1].Trim() -ne '0') { throw 'monte_app ya existe. Configura su contraseña actual; este script no reemplaza cuentas.' }
$passwordBytes = New-Object byte[] 32
$randomGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $randomGenerator.GetBytes($passwordBytes) } finally { $randomGenerator.Dispose() }
$generatedPassword = -join ($passwordBytes | ForEach-Object { $_.ToString('X2') })
$setupSql = @"
CREATE USER 'monte_app'@'127.0.0.1' IDENTIFIED BY '$generatedPassword';
GRANT SELECT, INSERT, UPDATE ON monte_oracion_java.* TO 'monte_app'@'127.0.0.1';
GRANT DELETE ON monte_oracion_java.personal_rol TO 'monte_app'@'127.0.0.1';
"@
$setupSql | & $mysqlPath --protocol=tcp --host=127.0.0.1 "--port=$Puerto" --user=root --batch
if ($LASTEXITCODE -ne 0) { throw 'No se pudo completar el alta de monte_app. Revisa los permisos de administración.' }
$storagePath = (Join-Path $projectPath 'data\adjuntos').Replace('\','/')
$configText = "db.url=jdbc:mariadb://127.0.0.1:$Puerto/monte_oracion_java`ndb.user=monte_app`ndb.password=$generatedPassword`nstorage.path=$storagePath`n"
[System.IO.File]::WriteAllText($configPath,$configText,[System.Text.UTF8Encoding]::new($false))
New-Item -ItemType Directory -Path $serverConfigFolder -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $serverConfigFolder 'database.properties'),$configText,[System.Text.UTF8Encoding]::new($false))
$generatedPassword = $null
$setupSql = $null
$configText = $null
Write-Host 'Usuario monte_app creado con permisos sobre monte_oracion_java.'
Write-Host 'Conexión guardada en config/database.properties y en conf/MonteOracionJava/database.properties de Tomcat.'
Write-Host 'No se cambiaron las cuentas existentes ni los datos de la iglesia.'
