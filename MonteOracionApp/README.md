# Monte de Oración

Aplicación ASP.NET Core MVC (.NET 10), Identity, EF Core y SQL Server para donaciones, distribución, tesorería y eventos. Las funciones se agrupan por módulos; los permisos pertenecen a cuentas personales del equipo.

## Ejecutar

Abrir la solución `../MonteOracionApp.slnx` o este proyecto. Se necesita SQL Server y .NET 10. La conexión local está en `appsettings.json`; se puede reemplazar con secretos de desarrollo o `ConnectionStrings__DefaultConnection`.

Desde esta carpeta, con Development:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run -- --initialize
dotnet run -- --create-admin
dotnet run
```

`--initialize` aplica migraciones y crea Administrador, Tesorero y Almacenero. `--create-admin` solicita correo y contraseña sin mostrarla; no reemplaza cuentas existentes. No hay credenciales predeterminadas. Si ya hay administrador, inicia sesión con su acceso anterior.

Antes de actualizar una instalación existente, respalda SQL Server y `App_Data/attachments`. Las migraciones conservan donaciones y usuarios. Los congregantes anteriores quedan sin DNI hasta que el administrador lo complete; sus antiguas cuentas dejan de servir para acceder al sistema.

## Primer uso

1. Administración → Personal y permisos: completar el nombre del administrador y crear las cuentas del equipo. Se necesitan dos personas distintas para solicitar y aprobar una operación.
2. Configurar iglesia, medios de pago y fondos. Yape y transferencias se verifican manualmente.
3. Completar DNI de congregantes. La consulta pública muestra donaciones y próximos eventos asignados; omite comprobantes, datos de contacto y asistencia.
4. Almacén → Productos: definir presentación y unidad base. Registrar beneficiarios.
5. Registrar recepciones de efectivo/bienes. Las recepciones de bienes crean lotes y entradas de inventario.
6. Incorporar bienes históricos únicamente después de comprobar su existencia física.
7. Solicitar y autorizar bienes/dinero; registrar entrega o pago efectivo; rendir anticipos y reponer préstamos.
8. Cerrar caja y conciliar el saldo digital agregado. Descargar PDF por día, mes, año o rango.

## Módulos

- Accounts / Staff: acceso personal, nombres, cargos, múltiples roles y desactivación.
- Congregants / Portal: fichas, DNI único y consulta sin cuenta.
- Donations: recepción, revisión, comprobantes privados y operaciones digitales sin duplicación.
- Inventory: productos, beneficiarios, lotes, solicitudes, prioridades, reservas, entregas parciales, préstamos, reposiciones, condonaciones y ajustes auditados.
- Treasury: fondos, gastos, anticipos, reservas de dinero, pagos, rendiciones, cierres y conciliación.
- Publications: eventos/avisos, imágenes, modal público, asignaciones y asistencia privada.
- Reports / Audit: PDF paginado y trazabilidad del personal.
- Dashboard: acciones y avisos según los roles.

## Reglas principales

Una aprobación reserva recursos. Una entrega o pago confirmado cambia el saldo real. Una persona no aprueba su propia solicitud aunque tenga varios roles. Los bienes reservados no pueden salir por otro propósito. Las correcciones agregan movimientos y motivos; no se borran las operaciones. Los cierres bloquean las fechas de ese medio; una diferencia se ajusta en fecha abierta tras autorización.

La migración incorpora las donaciones monetarias aprobadas anteriores al libro de movimientos. Ese saldo histórico debe compararse con el dinero real: no conoce gastos previos que nunca se registraron. Los fondos específicos conservan su destino; una donación al fondo general se rinde por fondo, sin atribuir arbitrariamente un gasto a un donante.

## Verificación

`Verification/Checks.csproj` contiene comprobaciones de migración, reservas, concurrencia, entregas repetidas, préstamos, gastos, anticipos, cierres y PDF en una base SQL temporal. Nunca utiliza la base real para insertar pruebas. Requiere permiso para crear y eliminar una base con prefijo `MonteOracionApp_Verification_`.

```powershell
dotnet run --project Verification/Checks.csproj
```

Las comprobaciones originales del proyecto hermano `MonteOracionApp.Checks` siguen disponibles. Las salidas de compilación, adjuntos y credenciales temporales de prueba se excluyen de Git.

Consulta `docs/GUIA_PRUEBAS.md`, `docs/DISENO_MODULAR.md`, `docs/OPERACION.md` y `docs/VERIFICACION.md`.

## Límites de esta versión

La consulta por DNI reproduce el flujo solicitado para el proyecto: conocer un DNI permite consultar los resultados asociados. El límite por IP y la reducción de datos no verifican la identidad del visitante.

No hay integración bancaria, mensajería externa, reservas automáticas por prioridad ni conversión entre unidades. La conciliación digital compara el saldo agregado de cuentas/billeteras con el registrado, no se conecta a sus proveedores. Las copias de seguridad deben programarse en la infraestructura del servidor.
