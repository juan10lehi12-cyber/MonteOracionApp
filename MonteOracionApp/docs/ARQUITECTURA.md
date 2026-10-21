# Arquitectura

Versión de gestión integral, 18 de octubre de 2026.

ASP.NET Core MVC organiza cada módulo en entidades, formularios, controladores y servicios cuando hay operaciones de varios pasos. Las vistas siguen las convenciones MVC y utilizan un menú por módulos para el personal. La portada y la consulta por DNI tienen navegación pública.

Flujo: navegador → autorización y validación MVC → servicio → transacción de SQL Server / almacenamiento privado → vista o PDF.

Los servicios InventoryService y TreasuryService concentran las reglas de recursos. DonationService integra la recepción con sus movimientos para evitar donaciones aprobadas sin entrada contable o de stock. La recepción, decisión y movimiento correspondiente se guardan atómicamente.

Las reservas y salidas utilizan transacciones serializables y versiones de fila. SQL restringe los saldos del lote, cantidades entregadas/devueltas y claves únicas de operaciones. Los conflictos concurrentes se informan para que el usuario revise y vuelva a intentar. Las entregas y reposiciones tienen claves de formulario para evitar duplicación.

Las cuentas pertenecen al personal activo. Los roles autorizan cada acción en servidor y determinan los módulos visibles. Congregantes conserva UserId únicamente como vínculo histórico de la versión anterior; la consulta actual usa DNI y no permite el inicio de sesión de esos usuarios.

La auditoría registra persona, acción, referencia, motivo y fecha. Las correcciones de inventario y dinero son nuevos movimientos. Los eventos conservan asignaciones retiradas y una versión de fila para cambios de asistencia.

Los archivos se guardan fuera de wwwroot. Se validan tamaño, extensión y firmas básicas; esa comprobación no es un escaneo antivirus. Las descargas de comprobantes verifican rol. Las imágenes de eventos y QR solo se sirven públicamente cuando corresponde a una publicación o medio activo.

PdfReport genera documentos paginados con fuentes estándar y texto español; no requiere un ejecutable externo en el servidor. Los filtros se aplican en SQL y los PDF distinguen fecha declarada, movimiento real y stock actual.

Las migraciones versionan tablas con prefijo tb_. Se generan scripts idempotentes y no se aplican automáticamente al abrir el sitio. El comando --initialize aplica las pendientes y crea los roles.

Consultar DISENO_MODULAR.md para campos, permisos y estados; GUIA_PRUEBAS.md para los recorridos del usuario.
