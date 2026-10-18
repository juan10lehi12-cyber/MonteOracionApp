# Arquitectura y reglas

## Flujo de una solicitud

Navegador → controlador (acceso y validación del formulario) → servicio (reglas y operación) → ApplicationDbContext / almacenamiento → vista.

La aplicación usa un único proyecto organizado por módulos para mantener sencillo el primer despliegue. Cada módulo agrupa su entidad y controlador, sus formularios y su servicio cuando la operación requiere reglas o varios pasos. Los CRUD simples de publicaciones y configuración usan el contexto desde su controlador; no necesitan una capa adicional sin lógica. Las vistas siguen la convención MVC por nombre de controlador.

## Identidad

- Dos roles con cuenta: Administrador y Congregante. Externo es una condición del donante, no un rol autenticado.
- No hay registro público. El administrador vincula una cuenta a un congregante existente.
- Congregante y cuenta tienen una relación opcional uno a uno, protegida por un índice único.
- Desactivar un congregante invalida las sesiones de su cuenta y bloquea nuevos ingresos. Se conserva su historial.
- Las sesiones duran 30 minutos con renovación por actividad. El sello de seguridad se comprueba en cada solicitud autenticada.
- Cinco contraseñas fallidas bloquean temporalmente la cuenta durante 15 minutos.
- Todos los POST MVC requieren token antifalsificación. El inicio de sesión y la recepción del formulario público tienen límite por IP.
- Identity cifra mediante hash las contraseñas. No se guardan contraseñas en texto plano.

## Donaciones

- Digital: solo dinero por QR/billetera o transferencia; JPG/PNG obligatorio; medio activo; cualquier visitante puede enviarla sin cuenta; comienza Pendiente. Un pago enviado sin iniciar sesión puede vincularse a un congregante durante la revisión.
- Presencial: el administrador registra bienes recibidos, su detalle y una foto obligatoria; identifica al donante como congregante, externo o anónimo; requiere confirmación del administrador y comienza Aprobada. El portal muestra las donaciones económicas y los bienes vinculados al congregante.
- Miembro autenticado: siempre vinculado al congregante de la cuenta, nunca al ID enviado por el navegador.
- Externo: nombre obligatorio salvo que seleccione Anónimo; contacto opcional.
- Anónimo: no se guarda nombre ni contacto. El comprobante puede contener datos personales y permanece restringido.
- Transiciones: Pendiente → Aprobada / Rechazada / Anulada; Aprobada → Anulada. Rechazada y Anulada son finales.
- Rechazo y anulación requieren motivo. No existe borrado ni edición de una donación aprobada; se anula y se registra la corrección.
- Una columna rowversion evita que una pantalla antigua sobrescriba una decisión concurrente. Estado e historial se guardan en una sola transacción de SaveChanges.
- Solo Aprobada integra los totales. Bienes y dinero se muestran por separado.
- La fecha de pago/recepción es fecha local de Lima; las fechas de auditoría son UTC y se muestran con UTC-5.
- Al registrar el pago se conserva el nombre del medio utilizado aunque posteriormente se renombre.

## Archivos

Los nombres se generan con UUID y se almacenan fuera de wwwroot. Se comprueban tamaño (máximo 5 MB), extensión y firmas básicas del formato. No se usa el nombre del cliente como ruta. El comprobante se entrega como descarga luego de verificar el rol o propietario. El QR se publica solamente si el medio está activo y admite JPG/PNG.

Esta validación no equivale a un antivirus ni a un análisis completo de PDF. Si se añade un escáner, integrarlo detrás de IFileStorage antes de persistir archivos. No publicar App_Data mediante el servidor web. La base guarda la referencia del archivo; respaldar ambos.

## Publicaciones

Eventos y avisos comparten entidad con un discriminador. El evento requiere fecha, hora y lugar. Las publicaciones no publicadas quedan fuera de la portada. Un evento publicado y luego cancelado conserva su contenido y aparece como Cancelado. Los textos se muestran codificados por Razor; no se acepta HTML arbitrario.

## Evolución

Añadir módulos sin trasladar las reglas de donaciones a vistas o JavaScript. Las validaciones del navegador ayudan a la interacción; el servidor mantiene la decisión final. Para incorporar medios de pago automáticos, introducir una integración independiente que invoque servicios de dominio y conserve auditoría e idempotencia.

