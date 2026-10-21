# Diseño modular y permisos

## Cuentas y accesos

Los congregantes tienen ficha y DNI, sin cuenta de acceso. El equipo utiliza cuentas personales con nombre y cargo. El cargo es descriptivo; un usuario puede tener varios roles. Los registros antiguos de Identity se conservan por sus referencias históricas, pero se permite iniciar sesión solamente al personal activo con algún rol de trabajo. Desactivar personal invalida su acceso en la siguiente solicitud.

| Función | Administrador | Tesorero | Almacenero |
|---|---|---|---|
| Usuarios, permisos, configuración, congregantes, eventos, asistencia | Sí | No | No |
| Solicitar bienes/dinero | Sí | Sí | Sí |
| Aprobar solicitudes | Sí, de otra persona | No | No |
| Recibir/verificar dinero y ejecutar pagos autorizados | Sí | Sí | No |
| Recibir bienes, preparar entregas y recibir reposiciones | Sí | No | Sí |
| Consultar existencias y catálogo para solicitudes | Sí | Sí | Sí |
| Ajustes de inventario y condonación | Sí, con motivo | No | No |
| Caja y conciliación digital | Sí | Sí | No |
| Revisar cierre preparado | Sí, de otra persona | No | No |
| Reportes de dinero | Sí | Sí | No |
| Reportes de almacén | Sí | No | Sí |

## Donaciones

Dinero digital: medio activo, fondo, operación, fecha declarada, importe positivo con dos decimales y comprobante JPG/PNG. La combinación medio-fecha-operación no se repite. Se permite vinculación por DNI o durante revisión. El anonimato elimina la vinculación y los datos identificadores del formulario. Una captura no aprueba el pago: el personal debe comprobar el ingreso.

Una donación aprobada produce una entrada contable en la fecha de confirmación (Lima). La fecha declarada del pago permanece en la donación. Una anulación produce un movimiento inverso en fecha abierta y verifica que no consuma reservas. Los bienes recibidos entran por producto/unidad del catálogo, vencimiento cuando aplique y ubicación; crean lotes y entradas atómicas.

## Inventario y solicitudes

Existencia física = entradas + reposiciones + ajustes positivos - entregas - mermas - ajustes negativos. Disponible = físico - reservado. La unidad del producto es la base de conteo; por ejemplo, un saco de 50 kg se registra como 50 cuando la unidad es kg. Se crean presentaciones separadas para unidades distintas; el sistema no hace conversiones.

Una solicitud captura destinatario, dirección y contacto como fotografía histórica de esos datos, más propósito, prioridad, fecha necesaria, tipo y líneas por lote. La prioridad ordena la revisión; un administrador decide y justifica las excepciones.

Estados: Pendiente → Aprobada / Rechazada / Cancelada; Aprobada → Parcial / Entregada / Cancelada; Parcial → Entregada / Cancelada. Aprobar reserva todas las líneas en una transacción. Entregar requiere cantidad, receptor y constancia; permite varias entregas parciales. Cancelar libera lo que no se entregó; conserva salidas y obligaciones de préstamos.

Los préstamos registran beneficiario responsable y vencimiento. La reposición recibida crea un lote nuevo del mismo producto con vencimiento real. Pendiente por reponer = entregado - repuesto - condonado. Una condonación queda autorizada por una persona diferente del solicitante; no incrementa existencias.

Cada salida parcial, reposición, ajuste o condonación utiliza una clave de operación para evitar duplicados al reenviar el formulario. Los saldos usan restricciones SQL, transacciones serializables y versiones de fila.

## Tesorería

Los fondos declaran propósito y restricciones; cada uno tiene saldo y reserva independientes en efectivo y digital. Los movimientos de dinero son inmutables desde la interfaz.

Solicitud: Gasto, Anticipo, SaldoInicial, AjusteIngreso o AjusteSalida. El personal solicita gastos/anticipos; administración solicita saldos/ajustes. Otra persona administradora autoriza. Los gastos/anticipos aprobados reservan dinero. Registrar operación real exige constancia y genera el movimiento; la aprobación sola no produce ingreso ni gasto real.

Un anticipo exige fecha de rendición. Al rendir se registra gasto sustentado y devolución físicamente recibida del sobrante. El gasto no se descuenta otra vez. Si hace falta más dinero se solicita una operación adicional.

Cierre por día y medio: saldo previo + entradas - salidas = esperado; se compara con contado/verificado. Una diferencia exige explicación y no altera el libro automáticamente. El cierre bloquea esa fecha y las anteriores para el medio seleccionado. Administración revisa cierres de otra persona. Digital exige estado de cuenta/constancia y representa saldo agregado de cuentas/billeteras.

## Eventos y consulta

Portada: franja institucional compacta, medios para donar, próximos eventos/avisos y eventos realizados. Las tarjetas abren un modal con imagen, descripción, inicio, fin y lugar. Los borradores no son públicos; los cancelados están identificados.

Administración asigna congregantes a próximos eventos. Puede retirar una asignación con motivo y conservar el historial. El DNI muestra solo las asignaciones activas a próximos eventos publicados. Asistencia, justificación y notas pertenecen exclusivamente a administración; los cambios conservan versión e historial.

## Auditoría y correcciones

Operaciones registran identificador de usuario y fecha UTC; las fechas operativas y la visualización utilizan Lima. Personal desactivado sigue identificado en los reportes. Productos utilizados conservan nombre, unidad y regla de vencimiento. Cambios de datos de beneficiario no reescriben destinos históricos.

Los PDF distinguen fecha de pago/recepción declarada, fecha de movimiento real y existencias actuales. No se presentan existencias actuales como si fueran históricas.
