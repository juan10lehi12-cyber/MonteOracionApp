using System.Data;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
namespace MonteOracionApp.Servicios;

public class ServicioAlmacen(ContextoBaseDatos db)
{
    /// <summary>Destino con el que se marcan las anulaciones de entradas que nunca llegaron.</summary>
    public const string DestinoAnulacion = "Anulación de entrada pendiente";
    /// <summary>Destino con el que se marcan los ajustes por conteo físico o merma.</summary>
    public const string DestinoAjuste = "Ajuste de inventario";

    public static readonly string[] CategoriasBase =
    [
        "Alimentos y Víveres",
        "Aseo y Limpieza",
        "Ropa y Abrigo",
        "Mobiliario y Enseres",
        "Instrumentos y Sonido",
        "Varios / Otros"
    ];

    public async Task AsegurarCatalogoBaseAsync()
    {
        var cantidadCatalogados = await db.Productos.CountAsync(p => p.activo && p.categoria != "Por Clasificar");
        if (cantidadCatalogados >= 5) return;

        (string nombre, string categoria, string unidad)[] iniciales =
        [
            ("Arroz", "Alimentos y Víveres", "kg"),
            ("Azúcar", "Alimentos y Víveres", "kg"),
            ("Aceite vegetal", "Alimentos y Víveres", "litro"),
            ("Leche evaporada", "Alimentos y Víveres", "lata"),
            ("Fideos", "Alimentos y Víveres", "paquete"),
            ("Avena", "Alimentos y Víveres", "kg"),
            ("Menestras / Lentejas", "Alimentos y Víveres", "kg"),
            ("Conserva de atún", "Alimentos y Víveres", "lata"),
            ("Chocolate para taza", "Alimentos y Víveres", "tableta"),
            ("Panetón", "Alimentos y Víveres", "unidad"),
            ("Jabón de tocador", "Aseo y Limpieza", "unidad"),
            ("Papel higiénico", "Aseo y Limpieza", "paquete"),
            ("Detergente", "Aseo y Limpieza", "kg"),
            ("Lejía / Desinfectante", "Aseo y Limpieza", "litro"),
            ("Frazada / Manta", "Ropa y Abrigo", "unidad"),
            ("Prenda de abrigo", "Ropa y Abrigo", "unidad"),
            ("Silla plástica", "Mobiliario y Enseres", "unidad"),
            ("Mesa plegable", "Mobiliario y Enseres", "unidad"),
            ("Olla / Utensilio de cocina", "Mobiliario y Enseres", "unidad"),
            ("Micrófono", "Instrumentos y Sonido", "unidad"),
            ("Guitarra acústica", "Instrumentos y Sonido", "unidad"),
            ("Parlante / Altavoz", "Instrumentos y Sonido", "unidad")
        ];

        var existentes = await db.Productos.Where(p => p.activo).Select(p => p.nombre.ToLower()).ToListAsync();
        var setExistentes = new HashSet<string>(existentes);
        var agregados = false;

        foreach (var item in iniciales)
        {
            if (setExistentes.Contains(item.nombre.ToLower())) continue;
            db.Productos.Add(new Producto
            {
                nombre = item.nombre,
                categoria = item.categoria,
                unidad = item.unidad,
                activo = true
            });
            agregados = true;
        }

        if (agregados) await db.SaveChangesAsync();
    }

    public async Task RecibirDonacionAsync(Donacion donacion, string idUsuario)
    {
        foreach (var item in donacion.bienes)
        {
            var producto = await db.Productos.SingleOrDefaultAsync(x => x.id == item.id_producto && x.activo)
                ?? throw new InvalidOperationException("Selecciona un producto para cada bien.");
            item.descripcion = producto.nombre; item.unidad = producto.unidad;
            var lote = new Lote
            {
                id_producto = producto.id, donacion_bien = item, cantidad_actual = item.cantidad,
                ubicacion = "", origen = "Donación recibida",
                revision_pendiente = true, id_usuario_pendiente = idUsuario, fecha_pendiente = DateTimeOffset.UtcNow
            };
            db.Lotes.Add(lote);
            AgregarMovimiento(lote, TipoMovimientoAlmacen.Donacion, item.cantidad, idUsuario, "Ingreso de donación recibida");
        }
    }

    public async Task ConfirmarEntradaMultipleAsync(
        int idLote,
        List<ItemCatalogacionAlmacen> items,
        int? idEvento,
        string idUsuario,
        Guid idComando = default)
    {
        var filasValidas = items
            .Where(x => !string.IsNullOrWhiteSpace(x.nombre_producto) && x.cantidad is > 0)
            .ToList();

        if (filasValidas.Count is 0 or > 30)
            throw new InvalidOperationException("Registra al menos un producto válido con cantidad mayor a cero.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lote = await db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
            .SingleOrDefaultAsync(x => x.id == idLote) ?? throw new InvalidOperationException("Entrada inexistente.");

        if (!lote.revision_pendiente)
            throw new InvalidOperationException("Esta entrada ya fue confirmada.");

        var donacion = lote.donacion_bien?.donacion;
        // El destino queda estrictamente fijado según lo registrado en la recepción por el donante
        var idEventoFijado = donacion?.id_evento ?? idEvento;

        string? tituloEvento = null;
        if (idEventoFijado.HasValue)
        {
            tituloEvento = await db.Publicaciones.AsNoTracking()
                .Where(x => x.id == idEventoFijado.Value)
                .Select(x => x.titulo)
                .FirstOrDefaultAsync();
        }
        var destinoTexto = tituloEvento != null ? $"Evento: {tituloEvento}" : "Almacén General de la Iglesia";

        var idProductoTemporal = lote.id_producto;
        var notaRecepcion = lote.donacion_bien?.descripcion ?? lote.origen;

        // Limpiamos el movimiento preliminar de puerta para reemplazarlo por los ingresos verificados exactos
        var movimientosPreliminares = await db.MovimientosAlmacen.Where(x => x.id_lote == lote.id).ToListAsync();
        if (movimientosPreliminares.Count > 0)
            db.MovimientosAlmacen.RemoveRange(movimientosPreliminares);



        for (var i = 0; i < filasValidas.Count; i++)
        {
            var fila = filasValidas[i];
            var nombreLimpio = fila.nombre_producto!.Trim();
            if (nombreLimpio.Length > 160) throw new InvalidOperationException("El nombre del producto admite hasta 160 caracteres.");

            var categoriaLimpia = !string.IsNullOrWhiteSpace(fila.categoria) ? fila.categoria.Trim() : "Alimentos y Víveres";
            if (categoriaLimpia.Length > 80) throw new InvalidOperationException("La categoría admite hasta 80 caracteres.");

            var unidadLimpia = !string.IsNullOrWhiteSpace(fila.unidad) ? fila.unidad.Trim() : "unidad";
            if (unidadLimpia.Length > 40) throw new InvalidOperationException("La unidad admite hasta 40 caracteres.");

            var cantItem = fila.cantidad!.Value;
            ReglasOperacion.Cantidad(cantItem);

            var ub = !string.IsNullOrWhiteSpace(fila.ubicacion) ? fila.ubicacion.Trim() : "Almacén Central";
            if (ub.Length > 160) throw new InvalidOperationException("La ubicación admite hasta 160 caracteres.");

            // 1. Buscar o crear producto limpio en el catálogo (sin duplicar por mayúsculas/minúsculas)
            var nombreLower = nombreLimpio.ToLower();
            var unidadLower = unidadLimpia.ToLower();
            var productoDestino = await db.Productos.FirstOrDefaultAsync(p =>
                p.activo && p.categoria != "Por Clasificar" && p.nombre.ToLower() == nombreLower && p.unidad.ToLower() == unidadLower);

            if (productoDestino == null)
            {
                productoDestino = new Producto
                {
                    nombre = nombreLimpio,
                    categoria = categoriaLimpia,
                    unidad = unidadLimpia,
                    activo = true
                };
                db.Productos.Add(productoDestino);
                await db.SaveChangesAsync();
            }
            else if (productoDestino.categoria != categoriaLimpia)
            {
                productoDestino.categoria = categoriaLimpia;
            }

            // 2. Buscar si ya existe un lote confirmado para este mismo (Producto + Ubicación + Destino [Iglesia o Evento])
            var ubLower = ub.ToLower();
            var existente = await db.Lotes
                .Include(x => x.donacion_bien)
                    .ThenInclude(x => x!.donacion)
                .FirstOrDefaultAsync(x =>
                    !x.revision_pendiente
                    && x.confirmado_por != null
                    && x.id != lote.id
                    && x.id_producto == productoDestino.id
                    && x.ubicacion.ToLower() == ubLower
                    && (x.donacion_bien != null && x.donacion_bien.donacion != null ? x.donacion_bien.donacion.id_evento : null) == idEventoFijado);

            var motivoIngreso = filasValidas.Count > 1
                ? $"Ingreso verificado (desglosado de «{notaRecepcion}»); ubicación: {ub}"
                : $"Ingreso verificado («{notaRecepcion}»); ubicación: {ub}";

            if (i == 0)
            {
                if (lote.donacion_bien != null)
                {
                    lote.donacion_bien.id_producto = productoDestino.id;
                    lote.donacion_bien.descripcion = productoDestino.nombre;
                    lote.donacion_bien.cantidad = cantItem;
                    lote.donacion_bien.unidad = unidadLimpia;
                }

                if (existente != null)
                {
                    existente.cantidad_actual += cantItem;
                    existente.confirmado_por = idUsuario;
                    existente.fecha_confirmacion = DateTimeOffset.UtcNow;
                    var mov = AgregarMovimiento(existente, TipoMovimientoAlmacen.Donacion, cantItem, idUsuario, motivoIngreso);
                    mov.destino = destinoTexto;

                    db.Lotes.Remove(lote);
                }
                else
                {
                    lote.id_producto = productoDestino.id;
                    lote.cantidad_actual = cantItem;
                    lote.ubicacion = ub;
                    lote.revision_pendiente = false;
                    lote.id_usuario_pendiente = null;
                    lote.fecha_pendiente = null;
                    lote.confirmado_por = idUsuario;
                    lote.fecha_confirmacion = DateTimeOffset.UtcNow;
                    var mov = AgregarMovimiento(lote, TipoMovimientoAlmacen.Donacion, cantItem, idUsuario, motivoIngreso);
                    mov.destino = destinoTexto;

                }
            }
            else
            {
                DonacionBien? nuevoBien = null;
                if (donacion != null)
                {
                    nuevoBien = new DonacionBien
                    {
                        id_donacion = donacion.id,
                        id_producto = productoDestino.id,
                        descripcion = productoDestino.nombre,
                        cantidad = cantItem,
                        unidad = unidadLimpia
                    };
                    db.Set<DonacionBien>().Add(nuevoBien);
                    await db.SaveChangesAsync();
                }

                if (existente != null)
                {
                    existente.cantidad_actual += cantItem;
                    existente.confirmado_por = idUsuario;
                    existente.fecha_confirmacion = DateTimeOffset.UtcNow;
                    var mov = AgregarMovimiento(existente, TipoMovimientoAlmacen.Donacion, cantItem, idUsuario, motivoIngreso);
                    mov.destino = destinoTexto;

                }
                else
                {
                    var nuevoLote = new Lote
                    {
                        id_producto = productoDestino.id,
                        id_donacion_bien = nuevoBien?.id,
                        ubicacion = ub,
                        origen = lote.origen,
                        cantidad_actual = cantItem,
                        revision_pendiente = false,
                        confirmado_por = idUsuario,
                        fecha_confirmacion = DateTimeOffset.UtcNow
                    };
                    db.Lotes.Add(nuevoLote);
                    var mov = AgregarMovimiento(nuevoLote, TipoMovimientoAlmacen.Donacion, cantItem, idUsuario, motivoIngreso);
                    mov.destino = destinoTexto;

                }
            }

            await db.SaveChangesAsync();
        }

        // Desactivar el producto temporal "Por Clasificar" si quedó sin referencias activas
        var prodTemp = await db.Productos.FindAsync(idProductoTemporal);
        if (prodTemp != null && prodTemp.categoria == "Por Clasificar")
        {
            var enUso = await db.Lotes.AnyAsync(x => x.id_producto == prodTemp.id);
            if (!enUso) prodTemp.activo = false;
        }

        await db.GuardarOperacionAsync();
        await tx.CommitAsync();
    }

    public async Task GuardarProductoCatalogoAsync(int? idProducto, string nombre, string categoria, string unidad)
    {
        var nombreLimpio = nombre?.Trim() ?? "";
        var categoriaLimpia = categoria?.Trim() ?? "";
        var unidadLimpia = unidad?.Trim() ?? "unidad";

        if (nombreLimpio.Length is 0 or > 160)
            throw new InvalidOperationException("Indica el nombre del producto (hasta 160 caracteres).");
        if (categoriaLimpia.Length is 0 or > 80 || categoriaLimpia.Equals("Por Clasificar", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Indica una categoría válida (hasta 80 caracteres).");
        if (unidadLimpia.Length is 0 or > 40)
            throw new InvalidOperationException("Indica una unidad de medida válida.");

        var nombreLower = nombreLimpio.ToLower();
        var unidadLower = unidadLimpia.ToLower();

        if (idProducto is > 0)
        {
            var prod = await db.Productos.FindAsync(idProducto.Value)
                ?? throw new InvalidOperationException("Producto no encontrado.");
            var duplicado = await db.Productos.AnyAsync(p =>
                p.activo && p.id != prod.id && p.categoria != "Por Clasificar"
                && p.nombre.ToLower() == nombreLower && p.unidad.ToLower() == unidadLower);
            if (duplicado)
                throw new InvalidOperationException($"Ya existe el producto «{nombreLimpio} ({unidadLimpia})» en el catálogo.");

            prod.nombre = nombreLimpio;
            prod.categoria = categoriaLimpia;
            prod.unidad = unidadLimpia;
            prod.activo = true;
            await db.SaveChangesAsync();
            return;
        }

        var existente = await db.Productos.FirstOrDefaultAsync(p =>
            p.activo && p.nombre.ToLower() == nombreLower && p.unidad.ToLower() == unidadLower);

        if (existente != null)
        {
            existente.nombre = nombreLimpio;
            existente.categoria = categoriaLimpia;
        }
        else
        {
            db.Productos.Add(new Producto
            {
                nombre = nombreLimpio,
                categoria = categoriaLimpia,
                unidad = unidadLimpia,
                activo = true
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task EditarExistenciaAsync(
        int idLote,
        string nombreProducto,
        string categoria,
        string unidad,
        string ubicacion,
        decimal nuevaCantidad,
        string? motivoAjuste,
        string idUsuario)
    {
        var nombreLimpio = nombreProducto?.Trim() ?? "";
        var categoriaLimpia = categoria?.Trim() ?? "";
        var unidadLimpia = unidad?.Trim() ?? "";
        var ubLimpia = string.IsNullOrWhiteSpace(ubicacion) ? "Almacén Central" : ubicacion.Trim();

        if (nombreLimpio.Length is 0 or > 160) throw new InvalidOperationException("Indica el nombre del producto.");
        if (categoriaLimpia.Length is 0 or > 80) throw new InvalidOperationException("Indica la categoría del producto.");
        if (unidadLimpia.Length is 0 or > 40) throw new InvalidOperationException("Indica la unidad de medida.");
        if (ubLimpia.Length > 160) throw new InvalidOperationException("La ubicación admite hasta 160 caracteres.");
        if (nuevaCantidad < 0 || nuevaCantidad > 9999999999.99m)
            throw new InvalidOperationException("La cantidad actual no puede ser negativa.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lote = await db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
            .SingleOrDefaultAsync(x => x.id == idLote && !x.revision_pendiente)
            ?? throw new InvalidOperationException("Existencia no encontrada.");

        // Actualizar o reasignar el producto
        var nombreLower = nombreLimpio.ToLower();
        var unidadLower = unidadLimpia.ToLower();
        var otroProducto = await db.Productos.FirstOrDefaultAsync(p =>
            p.activo && p.id != lote.id_producto && p.categoria != "Por Clasificar"
            && p.nombre.ToLower() == nombreLower && p.unidad.ToLower() == unidadLower);

        if (otroProducto != null)
        {
            otroProducto.categoria = categoriaLimpia;
            lote.id_producto = otroProducto.id;
            lote.producto = otroProducto;
        }
        else
        {
            lote.producto.nombre = nombreLimpio;
            lote.producto.categoria = categoriaLimpia;
            lote.producto.unidad = unidadLimpia;
        }

        lote.ubicacion = ubLimpia;

        // Si cambió la cantidad física, registrar movimiento de cuadre / ajuste
        var diferencia = nuevaCantidad - lote.cantidad_actual;
        if (diferencia != 0)
        {
            var motivoLimpio = string.IsNullOrWhiteSpace(motivoAjuste)
                ? "Cuadre por conteo físico en almacén"
                : motivoAjuste.Trim();
            if (motivoLimpio.Length > 500) throw new InvalidOperationException("El motivo admite hasta 500 caracteres.");

            var cantAnterior = lote.cantidad_actual;
            lote.cantidad_actual = nuevaCantidad;

            var mov = AgregarMovimiento(
                lote,
                diferencia > 0 ? TipoMovimientoAlmacen.Donacion : TipoMovimientoAlmacen.Salida,
                diferencia,
                idUsuario,
                $"Cuadre de stock ({cantAnterior:0.##} → {nuevaCantidad:0.##} {unidadLimpia}): {motivoLimpio}");
            mov.destino = DestinoAjuste;
        }

        await db.SaveChangesAsync();

        // Si al editar nombre o ubicación coincide con otro lote del mismo destino (Iglesia o mismo Evento), sumarlos
        var idEventoLote = lote.donacion_bien?.donacion?.id_evento;
        var ubLower = ubLimpia.ToLower();
        var loteGemelo = await db.Lotes
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
            .FirstOrDefaultAsync(x =>
                !x.revision_pendiente
                && x.confirmado_por != null
                && x.id != lote.id
                && x.id_producto == lote.id_producto
                && x.ubicacion.ToLower() == ubLower
                && (x.donacion_bien != null && x.donacion_bien.donacion != null ? x.donacion_bien.donacion.id_evento : null) == idEventoLote);

        if (loteGemelo != null)
        {
            loteGemelo.cantidad_actual += lote.cantidad_actual;
            var movs = await db.MovimientosAlmacen.Where(x => x.id_lote == lote.id).ToListAsync();
            foreach (var m in movs) m.id_lote = loteGemelo.id;
            db.Lotes.Remove(lote);
        }

        await db.GuardarOperacionAsync();
        await tx.CommitAsync();
    }

    public async Task RechazarEntradaAsync(int idLote, string? motivo, string idUsuario, Guid idComando = default)
    {
        var motivoLimpio = string.IsNullOrWhiteSpace(motivo) ? "Los bienes no llegaron físicamente al almacén." : motivo.Trim();
        if (motivoLimpio.Length > 500) throw new InvalidOperationException("El motivo admite hasta 500 caracteres.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lote = await db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
            .SingleOrDefaultAsync(x => x.id == idLote) ?? throw new InvalidOperationException("Entrada inexistente.");
        if (!lote.revision_pendiente) throw new InvalidOperationException("Esta entrada ya fue procesada.");

        var mov = AgregarMovimiento(lote, TipoMovimientoAlmacen.Salida, -lote.cantidad_actual, idUsuario, $"Entrada anulada (no ingresó a almacén): {motivoLimpio}");
        mov.destino = DestinoAnulacion;

        lote.cantidad_actual = 0;
        lote.revision_pendiente = false;
        lote.id_usuario_pendiente = null;
        lote.fecha_pendiente = null;

        var donacion = lote.donacion_bien?.donacion;
        if (donacion != null && donacion.estado == EstadoDonacion.Aprobada)
        {
            var otrosLotesActivos = await db.Lotes.AnyAsync(x =>
                x.id != lote.id
                && x.donacion_bien != null
                && x.donacion_bien.id_donacion == donacion.id
                && x.cantidad_actual > 0);
            if (!otrosLotesActivos)
            {
                donacion.estado = EstadoDonacion.Anulada;
                db.Set<DonacionHistorial>().Add(new DonacionHistorial
                {
                    id_donacion = donacion.id,
                    estado = EstadoDonacion.Anulada,
                    id_administrador = idUsuario,
                    motivo = $"Anulada desde almacén: {motivoLimpio}"
                });
            }
        }

        await db.GuardarOperacionAsync();
        await tx.CommitAsync();
    }

    public async Task RegistrarSalidaAsync(FormularioSalidaAlmacen formulario, string idUsuario)
    {
        var destino = formulario.destino?.Trim() ?? "";
        if (destino.Length is 0 or > 160) throw new InvalidOperationException("Indica el destino de la salida (hasta 160 caracteres).");
        if (formulario.detalle?.Length > 500) throw new InvalidOperationException("El detalle admite hasta 500 caracteres.");
        var conProdSinCant = formulario.lineas.Where(x => x.id_lote != 0 && (x.cantidad is null or <= 0)).ToList();
        if (conProdSinCant.Count > 0) throw new InvalidOperationException("Indica una cantidad positiva para cada producto seleccionado.");
        var conCantSinProd = formulario.lineas.Where(x => x.id_lote == 0 && x.cantidad is > 0).ToList();
        if (conCantSinProd.Count > 0) throw new InvalidOperationException("Selecciona el producto correspondiente para la cantidad indicada.");
        var lineas = formulario.lineas.Where(x => x.id_lote != 0 && x.cantidad is > 0).ToList();
        if (lineas.Count is 0 or > 50 || lineas.Select(x => x.id_lote).Distinct().Count() != lineas.Count)
            throw new InvalidOperationException("Elige de 1 a 50 existencias distintas con cantidad positiva.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        foreach (var fila in lineas)
        {
            var cantidad = fila.cantidad!.Value; ReglasOperacion.Cantidad(cantidad);
            var lote = await db.Lotes.Include(x => x.producto).SingleOrDefaultAsync(x => x.id == fila.id_lote)
                ?? throw new InvalidOperationException("Existencia no encontrada.");
            if (lote.revision_pendiente) throw new InvalidOperationException($"{lote.producto.nombre} todavía no tiene visto bueno del almacén.");
            if (cantidad > lote.cantidad_actual) throw new InvalidOperationException($"No hay suficiente {lote.producto.nombre}: quedan {lote.cantidad_actual:0.##} {lote.producto.unidad}.");
            lote.cantidad_actual -= cantidad;
            var movimiento = AgregarMovimiento(lote, TipoMovimientoAlmacen.Salida, -cantidad, idUsuario, string.IsNullOrWhiteSpace(formulario.detalle) ? "Salida de almacén" : formulario.detalle.Trim());
            movimiento.destino = destino;
        }
        try { await db.GuardarOperacionAsync(); await tx.CommitAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Otra persona movió estas existencias al mismo tiempo. Revisa las cantidades e intenta de nuevo."); }
    }

    private MovimientoAlmacen AgregarMovimiento(Lote lote, TipoMovimientoAlmacen tipo, decimal cantidad, string idUsuario, string motivo)
    {
        var movimiento = new MovimientoAlmacen { lote = lote, tipo = tipo, cantidad = cantidad, id_usuario = idUsuario, motivo = motivo };
        db.MovimientosAlmacen.Add(movimiento); return movimiento;
    }
}
