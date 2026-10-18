# Operación

## SQL Server

El proyecto conserva la cadena LocalDB original para no cambiar silenciosamente el servidor del usuario. Para SQL Server normal, usar secretos locales en Development o `ConnectionStrings__DefaultConnection` en el entorno del servidor.

`dotnet run -- --initialize` aplica las migraciones y crea los dos roles sin agregar personas ni donaciones de ejemplo. `dotnet run -- --create-admin` también inicializa y solicita las credenciales del primer administrador. No usar contraseñas compartidas.

Las migraciones no se ejecutan automáticamente cada vez que arranca el sitio. Aplicarlas durante una ventana de mantenimiento, con una copia de seguridad previa. El SQL de `Data/Scripts/InitialChurchModules.sql` es idempotente según la tabla de historial de EF. Revisarlo y ejecutarlo sobre la base elegida si se prefiere administración manual; la creación de roles/cuenta sigue realizándose con los comandos anteriores.

## Publicación

1. Configurar SQL Server y HTTPS en el alojamiento compatible con .NET 10.
2. Aplicar migraciones y crear administrador en un entorno de confianza.
3. Configurar `ASPNETCORE_ENVIRONMENT=Production`, conexión y rutas persistentes.
4. `dotnet publish -c Release` y desplegar el resultado.
5. Dar acceso de escritura a la identidad de la aplicación sobre el almacenamiento de archivos. `Storage__Path` puede apuntar a una carpeta persistente absoluta fuera de wwwroot.
6. Conservar y proteger las claves de ASP.NET Data Protection del host; múltiples instancias deben compartirlas. No perderlas al reemplazar el despliegue.
7. Configurar límites de carga de al menos 6 MB en el proxy/IIS; cada archivo admitido seguirá limitado a 5 MB por la aplicación.
8. Si hay proxy inverso, configurar encabezados reenviados únicamente para proxies de confianza. El limitador utiliza la IP remota recibida; detrás de un proxy sin esta configuración, varios visitantes comparten la cuota.
9. Revisar logs de ASP.NET y del servidor. Los errores de producción muestran un identificador de solicitud y no exponen la excepción al visitante.

## Copias de seguridad

Plan inicial: diario, retención de siete días. Debe configurarse en el equipo que aloje SQL y los archivos; esta entrega no crea tareas programadas.

- Respaldar la base con BACKUP DATABASE mediante las herramientas de SQL Server.
- Copiar la carpeta definida por Storage:Path junto al respaldo. Coordinarlo en mantenimiento para evitar referencias a archivos que aún no estén en la copia.
- Guardar copias en una ubicación distinta y con acceso restringido.
- Probar la restauración en otra base y carpeta antes de depender del procedimiento.
- Conservar secretos y claves de Data Protection por separado con controles adecuados.

Objetivo inicial propuesto: pérdida máxima de un día de información, recuperación en cuatro horas; validar con la infraestructura real. Los objetivos de tres segundos/20 usuarios y 99 % mensual requieren medición y monitoreo después del despliegue.

## Problemas frecuentes

- Tablas inexistentes: aplicar `--initialize` a la misma base que utiliza el sitio.
- LocalDB no inicia: comprobar `SqlLocalDB info MSSQLLocalDB` desde la sesión de Windows del usuario. Alternativamente configurar la instancia SQL Server instalada.
- Error SSPI: la autenticación de Windows no pudo crear el contexto de seguridad. Verificar la sesión, servidor y permisos con SSMS. No reemplazarlo por credenciales incrustadas en código.
- No se pueden subir archivos: revisar permisos y espacio del almacenamiento, límite de tamaño y formato.
- No se puede registrar una donación digital: configurar al menos un medio de pago activo.
- Congregante sin historial: verificar que la recepción se vinculó a su registro de congregante y que su cuenta está asociada a ese mismo registro.
