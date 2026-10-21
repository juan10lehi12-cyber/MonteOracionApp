# Operación de la gestión integral

## Base y acceso inicial

La conexión configurada en appsettings.json puede reemplazarse con secretos en Development o ConnectionStrings__DefaultConnection. El usuario de SQL debe poder aplicar el esquema durante mantenimiento; la aplicación diaria requiere permisos de datos.

Antes de actualizar, respaldar la base y el almacenamiento de archivos. --initialize aplica las migraciones pendientes y crea Administrador, Tesorero y Almacenero. --create-admin solicita credenciales y crea una cuenta nueva; no modifica una existente.

Tras migrar una instalación anterior:
- Los administradores existentes reciben perfil de personal y mantienen su contraseña.
- Los usuarios antiguos de congregantes se conservan por referencias históricas, pero no pueden entrar al sistema.
- Completar el DNI real de las fichas para habilitar consulta. No se inventan DNI.
- Las donaciones económicas aprobadas se incorporan al libro como saldo histórico. Compararlo con caja y bancos; registrar gastos anteriores ausentes mediante el procedimiento de ajuste autorizado, sin duplicar donaciones.
- Incorporar bienes anteriores después de verificar físicamente el inventario; cada recepción se incorpora una sola vez.
- Completar nombre/cargo del administrador y crear cuentas de personas diferentes para autorización.

## Publicación y almacenamiento

Configurar Production, SQL, HTTPS y una carpeta persistente Storage__Path fuera de wwwroot. El valor predeterminado es App_Data/attachments. Respaldar SQL y archivos juntos. Las claves de Data Protection deben conservarse entre despliegues; varias instancias necesitan claves compartidas.

Los archivos tienen máximo 5 MB y los formularios admiten 6 MB; configurar también esos límites en IIS/proxy. Si existe proxy, configurar IP reenviada solo desde proxies confiables: los límites de consultas e inicio de sesión dependen de la IP remota.

No se integra banca ni se envían mensajes. Tesorería verifica manualmente las operaciones y adjunta sustento de pagos, anticipos y conciliaciones.

## Fechas y cierres

Las fechas operativas usan Lima y la auditoría almacena UTC. La donación conserva la fecha declarada del pago/recepción; el movimiento real se registra al confirmarlo. No se cambia retrospectivamente un cierre: ajustes y anulaciones monetarias requieren fecha abierta.

Efectivo y digital cierran por separado. La conciliación digital compara el total registrado de cuentas/billeteras con su saldo agregado verificado y requiere constancia. No sustituye la revisión de cada operación bancaria.

Una diferencia de cierre queda visible; solicitar ajuste con motivo, aprobación de otra persona y comprobante para corregir el saldo en fecha abierta. Un cierre conserva su número y puede descargarse/revisarse.

## Respaldo y recuperación

Programar respaldos diarios con retención según las necesidades de la iglesia. Esta versión no instala tareas programadas. Mantener copias en otra ubicación, proteger secretos y probar una restauración separada.

Para una actualización, usar BACKUP DATABASE con COPY_ONLY y CHECKSUM y validar con RESTORE VERIFYONLY. Una verificación del archivo no reemplaza una prueba completa de restauración. Conservar adjuntos y claves de protección junto al plan de recuperación.

## Diagnóstico

- Tablas/campos inexistentes: ejecutar --initialize sobre la misma base configurada.
- No ingresa un administrador antiguo: confirmar que se aplicó GestionIntegral y existe su perfil activo.
- No aparecen resultados por DNI: completar la ficha y verificar que la donación esté vinculada al congregante correcto.
- No se puede aprobar una solicitud: revisar persona solicitante, saldo disponible, reservas y actividad del beneficiario.
- No se registra dinero: comprobar que la fecha no esté cerrada para ese medio.
- No hay medios para donar: activar uno desde configuración.
- Producto vencido: queda visible pero no puede reservarse ni entregarse; documentar su tratamiento mediante merma autorizada.
- Error de conexión Windows/SSPI: revisar permisos y conexión SQL desde la sesión Windows; no incrustar contraseñas en el código.
