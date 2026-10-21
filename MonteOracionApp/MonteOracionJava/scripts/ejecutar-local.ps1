param([string]$TomcatHome = $env:MONTE_TOMCAT_HOME, [string]$Config, [int]$Puerto = 8080)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
if (-not $Config) { $Config = Join-Path $projectPath 'config\database.properties' }
if (-not (Test-Path -LiteralPath $Config)) { throw 'Configura config/database.properties primero.' }
$warPath = Join-Path $projectPath 'target\MonteOracionJava.war'
if (-not (Test-Path -LiteralPath $warPath)) { throw 'Primero compila el proyecto.' }
if (-not $TomcatHome -or -not (Test-Path -LiteralPath (Join-Path $TomcatHome 'bin\bootstrap.jar'))) { throw 'Indica la carpeta de Apache Tomcat 10.1 con -TomcatHome o MONTE_TOMCAT_HOME.' }
$releaseNotesPath = Join-Path $TomcatHome 'RELEASE-NOTES'
if (-not (Test-Path -LiteralPath $releaseNotesPath) -or (Get-Content -LiteralPath $releaseNotesPath -Raw) -notmatch 'Apache Tomcat Version 10\.1\.') { throw 'Este proyecto utiliza Jakarta Servlet 6.0. Selecciona Apache Tomcat 10.1.' }
if ($Puerto -lt 1024 -or $Puerto -gt 65535) { throw 'Puerto inválido.' }
$runtimePath = Join-Path $projectPath 'data\tomcat10'
foreach ($folderName in @('conf','logs','temp','work','webapps')) { New-Item -ItemType Directory -Path (Join-Path $runtimePath $folderName) -Force | Out-Null }
$serverXml = @"
<Server port="-1"><Service name="Catalina"><Connector address="127.0.0.1" port="$Puerto" protocol="HTTP/1.1" connectionTimeout="20000" URIEncoding="UTF-8" maxPostSize="6291456"/><Engine name="Catalina" defaultHost="localhost"><Host name="localhost" appBase="webapps" unpackWARs="true" autoDeploy="false"/></Engine></Service></Server>
"@
Set-Content -LiteralPath (Join-Path $runtimePath 'conf\server.xml') -Value $serverXml -Encoding utf8
Copy-Item -LiteralPath (Join-Path $TomcatHome 'conf\web.xml') -Destination (Join-Path $runtimePath 'conf\web.xml') -Force
Copy-Item -LiteralPath $warPath -Destination (Join-Path $runtimePath 'webapps\MonteOracionJava.war') -Force
$priorConfig = $env:MONTE_CONFIG
$env:MONTE_CONFIG = (Resolve-Path -LiteralPath $Config).Path
try {
    Write-Host "Abre http://localhost:$Puerto/MonteOracionJava/"
    Write-Host 'Ctrl+C para detener. Este proceso usa una instancia local dentro de data/tomcat10.'
    & java "-Dcatalina.home=$TomcatHome" "-Dcatalina.base=$runtimePath" "-Djava.io.tmpdir=$runtimePath\temp" -cp "$TomcatHome\bin\bootstrap.jar;$TomcatHome\bin\tomcat-juli.jar" org.apache.catalina.startup.Bootstrap start
} finally { $env:MONTE_CONFIG = $priorConfig }
