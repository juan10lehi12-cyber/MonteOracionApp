# MonteOracionApp

## Contexto

Aplicación ASP.NET Core MVC con Identity, Entity Framework Core y SQL Server.
El código actual es la fuente de verdad. No analizar la historia del repositorio
ni comparar con versiones anteriores.

## Archivos que se deben leer

- `MonteOracionApp/Controladores/`
- `MonteOracionApp/Modelos/`
- `MonteOracionApp/Servicios/`
- `MonteOracionApp/Datos/`
- `MonteOracionApp/Vistas/`
- `MonteOracionApp/wwwroot/` (solo código y estilos propios)
- `MonteOracionApp/Program.cs`
- `MonteOracionApp/MonteOracionApp.csproj`
- `MonteOracionApp/appsettings.json`
- `MonteOracionApp/appsettings.Development.json`
- `base_datos.sql`
- `README.md`

## Archivos y carpetas que se deben ignorar

- `.git/`
- `.vs/`
- `**/bin/`
- `**/obj/`
- `**/artifacts/`
- `**/App_Data/`
- `MonteOracionApp/artifacts/`
- `MonteOracionApp/Verification/`
- `MonteOracionApp.Checks/`
- Archivos `.csproj.user`
- Zips, respaldos, copias, previews y carpetas de verificación antigua

`Verification/` y `MonteOracionApp.Checks/` solo se leen si el usuario pide
explícitamente revisar pruebas o verificaciones.

## Forma de trabajar

- No usar Git para decidir qué archivos leer.
- No analizar archivos eliminados ni cambios anteriores.
- Si se pide revisar un módulo, leer solo sus controladores, modelos, servicios
  y vistas relacionadas.
- Si se reporta un fallo concreto, ir directo a los archivos implicados.
- No proponer refactorizaciones generales si el usuario solo pidió entender o
  corregir un punto específico.
- Reportar problemas indicando `archivo:línea`.

## Excepción de alcance

- Si el usuario pide revisar todo el proyecto actual, o si un error no se puede
  explicar con los archivos del módulo, se permite ampliar la lectura a todo el
  código fuente actual.
- La ampliación nunca incluye `.git/`, `bin/`, `obj/`, `artifacts/`, `App_Data/`,
  respaldos, copias antiguas ni historial de Git.
