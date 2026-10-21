# Guía para probar la aplicación

## Preparación

1. Abrir la solución en Visual Studio y arrancar MonteOracionApp.
2. Iniciar sesión con el administrador existente. Si es una instalación vacía, usar --initialize y --create-admin desde consola.
3. En Personal y permisos, completar nombres y crear dos cuentas de personas diferentes para solicitar/aprobar. No se comparten accesos.
4. Completar DNI de congregantes anteriores, crear un producto (ejemplo: arroz, unidad kg, vencimiento obligatorio) y un beneficiario.
5. Verificar saldos históricos con dinero real antes de ejecutar operaciones de producción. En una instalación vacía se solicita un saldo inicial, otra persona lo aprueba y tesorería confirma la entrada con constancia.

## Flujos del congregante

- Registrar pago digital con número de operación, fecha, fondo e imagen.
- Verificar que permanece pendiente hasta la revisión de tesorería.
- Consultar DNI y ver solo fecha, dinero/bienes y estado. No hay inicio de sesión del congregante.
- Asignar un próximo evento publicado desde administración: aparece el aviso en la consulta. Sus notas de asistencia permanecen privadas.

## Almacén

- Recibir 10 kg de arroz y verificar entrada, ubicación, vencimiento y lote.
- Crear dos solicitudes de 6 kg. Autorizar una con otro administrador: la segunda no debe poder reservar los mismos recursos.
- Entregar 2 kg con receptor y constancia: físico 8, reservado 4.
- Cancelar el resto con motivo: físico 8, reservado 0. La salida de 2 permanece.
- Crear un préstamo desde stock libre, entregar parcialmente y registrar reposición con vencimiento real. Comprobar saldo por reponer.
- Comprobar que una baja no afecta reservas existentes.
- Bienes anteriores: incorporar cada recepción una sola vez tras verificación física. Si una parte ya se consumió, documentar el ajuste autorizado correspondiente antes de comprometerla.

## Dinero y cierres

- Solicitar gasto de 30 y autorizar con otra persona. El saldo real no cambia; el disponible disminuye.
- Registrar pago con comprobante: cambia el saldo real y se libera la reserva.
- Entregar anticipo de 20; rendir gasto de 5 y recibir sobrante de 15. Verificar una salida de 20 y entrada de 15, sin duplicar gasto.
- Comparar caja real con esperada. Una diferencia exige observación.
- Cerrar caja; intentar registrar un movimiento en ese medio/fecha cerrada: debe rechazarse.
- Conciliar digital con estado de cuenta y saldo agregado de las cuentas/billeteras.
- Descargar donaciones, movimientos, entregas, inventario y cierres en PDF con filtro día, mes, año o rango.

## Permisos

- Almacenero no entra a caja ni al personal.
- Tesorero no registra asistencia ni autoriza sus solicitudes.
- Desactivar una cuenta y comprobar que pierde su sesión.
- Intentar reenviar una entrega parcial: la clave del formulario impide duplicar la salida.

## Pruebas automatizadas aisladas

Desde el proyecto, ejecutar dotnet run --project Verification/Checks.csproj. Crea una base SQL nueva con nombre aleatorio y la elimina al finalizar. MONTE_TEST_SERVER puede cambiar la instancia. MONTE_KEEP_TEST_DB=1 conserva la base temporal únicamente para inspección y pruebas HTTP.

Verification/http_checks.py utiliza el fixture aleatorio generado en artifacts/checks y el servidor de pruebas en 127.0.0.1:5158. Prueba formularios con datos ficticios; no apuntarlo a una base real. Los archivos de artifacts se excluyen de Git.
