using System.Data;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
namespace MonteOracionApp.Servicios;

public class ServicioAlmacen(ContextoBaseDatos db)
{
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

    public async Task ConfirmarEntradaAsync(int idLote, string ubicacion, string idUsuario, Guid idComando)
    {
        await VerificarComando(idComando);
        ReglasOperacion.Motivo(ubicacion);
        var ub = ubicacion.Trim();
        if (ub.Length > 160) throw new InvalidOperationException("Ubicación demasiado larga.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lote = await db.Lotes.Include(x => x.producto).SingleOrDefaultAsync(x => x.id == idLote) ?? throw new InvalidOperationException("Entrada inexistente.");
        if (!lote.revision_pendiente) throw new InvalidOperationException("Esta entrada ya fue confirmada.");
        var existente = await db.Lotes.FirstOrDefaultAsync(x => !x.revision_pendiente && x.id != lote.id && x.id_producto == lote.id_producto && x.ubicacion == ub);
        if (existente != null)
        {
            existente.cantidad_actual += lote.cantidad_actual;
            existente.confirmado_por = idUsuario; existente.fecha_confirmacion = DateTimeOffset.UtcNow;
            var movimientosPrevios = await db.MovimientosAlmacen.Where(x => x.id_lote == lote.id).ToListAsync();
            foreach (var m in movimientosPrevios) m.lote = existente;
            AgregarMovimiento(existente, TipoMovimientoAlmacen.VistoBueno, 0, idUsuario, $"Visto bueno (+{lote.cantidad_actual:0.##} {lote.producto.unidad}); ubicación: {ub}").id_comando = idComando;
            db.Lotes.Remove(lote);
        }
        else
        {
            lote.ubicacion = ub;
            lote.revision_pendiente = false;
            lote.id_usuario_pendiente = null; lote.fecha_pendiente = null;
            lote.confirmado_por = idUsuario; lote.fecha_confirmacion = DateTimeOffset.UtcNow;
            AgregarMovimiento(lote, TipoMovimientoAlmacen.VistoBueno, 0, idUsuario, $"Visto bueno; ubicación: {ub}").id_comando = idComando;
        }
        await db.GuardarOperacionAsync(); await tx.CommitAsync();
    }

    public async Task RegistrarSalidaAsync(FormularioSalidaAlmacen formulario, string idUsuario)
    {
        await VerificarComando(formulario.id_comando);
        var destino = formulario.destino?.Trim() ?? "";
        if (destino.Length is 0 or > 160) throw new InvalidOperationException("Indica el destino de la salida (hasta 160 caracteres).");
        if (formulario.detalle?.Length > 500) throw new InvalidOperationException("El detalle admite hasta 500 caracteres.");
        var lineas = formulario.lineas.Where(x => x.id_lote != 0 && x.cantidad is > 0).ToList();
        if (lineas.Count is 0 or > 50 || lineas.Select(x => x.id_lote).Distinct().Count() != lineas.Count)
            throw new InvalidOperationException("Elige de 1 a 50 existencias distintas con cantidad positiva.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var primero = true;
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
            if (primero) { movimiento.id_comando = formulario.id_comando; primero = false; }
        }
        try { await db.GuardarOperacionAsync(); await tx.CommitAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Otra persona movió estas existencias al mismo tiempo. Revisa las cantidades e intenta de nuevo."); }
    }

    public async Task UnificarProductosAsync(List<int> idsDuplicados, int idObjetivo, string? nombreCorrecto, string idUsuario)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ids = idsDuplicados.Where(x => x > 0).ToHashSet();
        if (idObjetivo > 0) ids.Add(idObjetivo);
        if (ids.Count == 0) throw new InvalidOperationException("Marca al menos un producto para unificar o corregir.");
        var objetivo = await db.Productos.FindAsync(idObjetivo > 0 ? idObjetivo : ids.First())
            ?? throw new InvalidOperationException("Producto principal no encontrado.");
        if (!objetivo.activo) throw new InvalidOperationException("El producto principal debe estar activo.");
        var nombreLimpio = string.IsNullOrWhiteSpace(nombreCorrecto) ? objetivo.nombre.Trim() : nombreCorrecto.Trim();
        if (nombreLimpio.Length > 160) throw new InvalidOperationException("El nombre correcto admite hasta 160 caracteres.");
        var mismoNombre = await db.Productos.FirstOrDefaultAsync(x => x.activo && x.id != objetivo.id && x.nombre == nombreLimpio && x.unidad == objetivo.unidad);
        if (mismoNombre != null)
        {
            ids.Add(objetivo.id);
            objetivo = mismoNombre;
            ids.Add(objetivo.id);
        }
        var otrosIds = ids.Where(x => x != objetivo.id).ToList();
        if (otrosIds.Count == 0 && objetivo.nombre.Trim() == nombreLimpio)
            throw new InvalidOperationException("Marca al menos un producto mal escrito para unirlo o escribe el nombre corregido.");
        objetivo.nombre = nombreLimpio;
        foreach (var idDup in otrosIds)
        {
            var dup = await db.Productos.FindAsync(idDup);
            if (dup == null || !dup.activo) continue;
            var lotesMover = await db.Lotes.Where(x => x.id_producto == dup.id).ToListAsync();
            foreach (var l in lotesMover) l.id_producto = objetivo.id;
            var itemsMover = await db.Set<DonacionBien>().Where(x => x.id_producto == dup.id).ToListAsync();
            foreach (var it in itemsMover) { it.id_producto = objetivo.id; it.descripcion = nombreLimpio; }
            dup.activo = false;
            dup.nombre = $"{dup.nombre.Trim()} (unificado #{dup.id})";
        }
        var itemsObjetivo = await db.Set<DonacionBien>().Where(x => x.id_producto == objetivo.id).ToListAsync();
        foreach (var it in itemsObjetivo) it.descripcion = nombreLimpio;
        await db.SaveChangesAsync();
        var lotesConfirmados = await db.Lotes.Where(x => !x.revision_pendiente && x.id_producto == objetivo.id).OrderBy(x => x.id).ToListAsync();
        foreach (var grupo in lotesConfirmados.GroupBy(x => x.ubicacion.Trim().ToLowerInvariant()))
        {
            var lista = grupo.ToList();
            if (lista.Count <= 1) continue;
            var principal = lista[0];
            for (var i = 1; i < lista.Count; i++)
            {
                var extra = lista[i];
                principal.cantidad_actual += extra.cantidad_actual;
                var movimientos = await db.MovimientosAlmacen.Where(x => x.id_lote == extra.id).ToListAsync();
                foreach (var m in movimientos) m.id_lote = principal.id;
                db.Lotes.Remove(extra);
            }
        }
        await db.GuardarOperacionAsync(); await tx.CommitAsync();
    }

    private async Task VerificarComando(Guid idComando)
    {
        if (idComando == Guid.Empty) throw new InvalidOperationException("Actualiza el formulario antes de registrar la operación.");
        if (await db.MovimientosAlmacen.AnyAsync(x => x.id_comando == idComando)) throw new InvalidOperationException("Esta operación ya fue registrada. Actualiza la página.");
    }

    private MovimientoAlmacen AgregarMovimiento(Lote lote, TipoMovimientoAlmacen tipo, decimal cantidad, string idUsuario, string motivo)
    {
        var movimiento = new MovimientoAlmacen { lote = lote, tipo = tipo, cantidad = cantidad, id_usuario = idUsuario, motivo = motivo };
        db.MovimientosAlmacen.Add(movimiento); return movimiento;
    }
}
