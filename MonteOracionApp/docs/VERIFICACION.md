# Verificación de Gestión Integral

Fecha: 18 de octubre de 2026.

## Resultados

- Compilación principal: cero errores y cero advertencias.
- Modelo EF y migraciones: sin cambios pendientes.
- 28 comprobaciones originales de reglas de donación y almacenamiento privado: aprobadas.
- 20 comprobaciones integradas en SQL Server: aprobadas.
- 74 comprobaciones HTTP de páginas, permisos, formularios, consulta y PDF: aprobadas.
- Total: 122 comprobaciones.
- Modal de evento revisado en navegador con horario y lugar.
- PDF de dinero y documento largo renderizados; acentos españoles y paginación comprobados.

## Integración SQL

Se creó una base independiente con nombre aleatorio MonteOracionApp_Verification_…. Se aplicó primero el esquema anterior, se insertó una donación/ficha de prueba y después se aplicó la actualización. Se comprobó conservación del registro, creación de perfil de administrador y traslado del saldo al libro.

Las pruebas cubrieron recepción y vencimiento de bienes, autoaprobación rechazada, reservas insuficientes, entregas parciales, doble envío, cancelación, préstamos, reposiciones, condonación, ajustes que afectan reservas, reservas de dinero, pago único, rendición de anticipos, dos aprobaciones concurrentes, operación digital duplicada, anulación, caja, conciliación digital y copia histórica del destinatario.

Las bases de prueba se eliminaron al terminar la revisión. No se insertaron datos de demostración en la base real.

## HTTP y formularios

Se recorrieron páginas públicas y administrativas, tres roles, consulta por DNI sin notas/archivos privados, antifalsificación, PDF, creación y desactivación de personal, solicitud/aprobación/entrega parcial con constancia, repetición de una entrega, retiro/restablecimiento de participantes, imagen pública, cambio de horario y asistencia privada.

La pantalla antigua de asistencia se rechaza con su versión previa; el evento realizado no publica las notas internas. Las pantallas de producto y beneficiario se comprobaron después de incorporar las opciones de corrección.

## Actualización local

Antes de aplicar se creó un respaldo COPY_ONLY con CHECKSUM y RESTORE VERIFYONLY confirmó su validez:

`C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\MonteOracionApp_antes_integral_20261018_1854.bak`

Se aplicaron GestionIntegral y HistorialParticipacionYDestinos mediante --initialize. Se conservaron una donación y dos congregantes, y se habilitaron los tres roles del personal. No se cambiaron contraseñas existentes.

## Alcance y límites

Las pruebas cubren la primera versión funcional; no constituyen auditoría bancaria, prueba de carga ni aprobación de despliegue público. Se necesita completar DNI reales, catálogo, beneficiarios y conciliación de saldos antes del uso operativo.

La consulta por DNI funciona por el identificador conocido; no autentica a la persona. Las operaciones digitales se verifican manualmente. Las imágenes se validan por extensión y firmas básicas, sin escaneo antivirus.

NuGet no pudo consultar su fuente de auditoría de vulnerabilidades desde este entorno (NU1900 en los proyectos de comprobaciones). Se utilizaron paquetes ya disponibles; el proyecto principal compila sin advertencias.
