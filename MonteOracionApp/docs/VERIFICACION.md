# Verificación de la primera versión

Fecha: 11 de octubre de 2026.

## Comprobado

- Compilación de la aplicación .NET 10: sin errores ni advertencias.
- Migración EF Core generada y revisada: mantiene las claves de Identity en 128 caracteres e incorpora los módulos nuevos. Script SQL idempotente generado sin conectar al servidor.
- 28 comprobaciones de reglas y almacenamiento: montos, fechas, cuatro transiciones válidas, estados finales, motivos obligatorios, precisión SQL, extensión/firma, tamaño, PNG truncado, QR sin PDF y protección de rutas.
- Servidor local iniciado correctamente.
- HTTP: login y privacidad responden 200; administración, congregantes, recepción y comprobantes sin sesión redirigen al acceso (302); registro público antiguo devuelve 404; POST sin token antifalsificación devuelve 400.

Las comprobaciones están en el proyecto hermano MonteOracionApp.Checks, incluido en la solución. Se ejecutan normalmente desde la carpeta de la solución:

```powershell
dotnet run --project MonteOracionApp.Checks
```

En esta sesión, NuGet no pudo leer su archivo de configuración del usuario incluso después de conceder acceso. Las 28 comprobaciones se compilaron y ejecutaron con el compilador Roslyn del SDK, los ensamblados de referencia y la aplicación ya compilada, sin descargar paquetes. El proyecto de comprobaciones no agrega librerías externas de pruebas.

## Actualización del flujo de donaciones (11 de octubre de 2026)

- Portada y formulario público: muestran únicamente medios activos y un aviso cuando no hay ninguno.
- La revisión permite vincular un aporte pendiente sin congregante (externo o anónimo) al aprobarlo o rechazarlo, conservando el motivo de la decisión. El aporte se incluye en el historial privado del congregante.
- El administrador puede reemplazar la foto de bienes aprobados desde el detalle. Se valida JPG/PNG y 5 MB, se usa almacenamiento privado, se registra el cambio en el historial y se controla la concurrencia. Si falla el guardado, se elimina la nueva imagen; tras guardar se elimina la anterior.
- Compilación verificada en `artifacts/verification`, sin errores ni advertencias. El ejecutable habitual estaba en uso por una aplicación abierta.
- Las 28 comprobaciones existentes de reglas de donaciones y almacenamiento privado pasaron con el ensamblado actualizado; no cubren las nuevas operaciones de vinculación y reemplazo de foto contra la base.
- `--initialize` ejecutado con acceso al SQL Server configurado: EF confirmó que no hay migraciones pendientes y pudo consultar `tb_rol`. La migración de renombrado ya estaba aplicada.

Queda pendiente probar en navegador el flujo completo con pagos reales, dos cuentas y decisiones simultáneas, incluyendo la sustitución de fotos y la vinculación al rechazar.

## Limitaciones de la verificación inicial

En la verificación inicial, LocalDB falló al acceder a la configuración de la instancia y SQL Server respondió con un error de contexto SSPI. La comprobación posterior descrita arriba sí pudo conectar y confirmar el estado de las migraciones. No se insertaron donaciones de demostración en la base del usuario.

Después de resolver la conexión e inicializar, comprobar en este orden:

1. Crear administrador e iniciar sesión.
2. Guardar información institucional y un medio de pago con QR válido.
3. Registrar dos congregantes y sus cuentas; verificar creación y cambio de contraseña.
4. Publicar un evento y un aviso, y comprobar que el borrador no aparece en la portada.
5. Registrar una donación pública con comprobante y localizarla por su código desde administración.
6. Registrar una donación desde el primer congregante; comprobar que el segundo no puede ver el detalle ni descargar el comprobante, incluso con la URL directa.
7. Aprobar tras verificar recepción y comprobar su estado en el historial del congregante.
8. Rechazar otra con motivo; comprobar que no incrementa los totales.
9. Recibir bienes con varias líneas y efectivo, vinculados al congregante; comprobar su aparición en su cuenta.
10. Anular un registro aprobado, comprobar su historial y la reducción del total confirmado.
11. Abrir una pendiente en dos sesiones administrativas, decidir en una y verificar que la otra no sobrescribe el cambio.
12. Desactivar el congregante y comprobar que pierde la sesión y no puede volver a ingresar; su historial debe conservarse.
13. Cancelar el evento y comprobar el estado Cancelado en la portada.
14. Probar respaldo y restauración de SQL y archivos en una ubicación de prueba.

No se han medido aún carga de 20 usuarios, tiempos de respuesta, disponibilidad mensual ni recuperación. Tampoco se ha realizado una revisión visual completa de todas las pantallas con datos reales.
