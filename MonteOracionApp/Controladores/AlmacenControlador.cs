using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.AccesoAlmacen)]
public class InventoryController(ContextoBaseDatos db, ServicioAlmacen servicio, IServicioDonaciones servicioDonaciones) : Controller
{
    private string IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(string? search, string? destino, string? categoria, bool availableOnly = false, int page = 1)
    {
        await servicio.AsegurarCatalogoBaseAsync();
        page = Math.Clamp(page, 1, 100000);

        // Por defecto separamos Almacén General ("general") de Eventos ("eventos" o ID de evento)
        var destinoNormalizado = string.IsNullOrWhiteSpace(destino) ? "general" : destino.Trim();

        var qBase = db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
                    .ThenInclude(x => x!.evento)
            .AsNoTracking()
            .Where(x => !x.revision_pendiente && !(x.confirmado_por == null && x.cantidad_actual == 0));

        // Contadores globales para las pestañas principales (Iglesia vs Eventos)
        var qConStock = qBase.Where(x => x.cantidad_actual > 0);
        var lotesGeneral = await qConStock.CountAsync(x => x.donacion_bien == null || x.donacion_bien.donacion == null || x.donacion_bien.donacion.id_evento == null);
        var lotesEventos = await qConStock.CountAsync(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null);
        ViewBag.LotesGeneral = lotesGeneral;
        ViewBag.LotesEventos = lotesEventos;
        ViewBag.LotesDisponibles = lotesGeneral + lotesEventos;

        // Filtrar según la pestaña activa (Iglesia vs Eventos)
        var qPestana = qBase;
        if (destinoNormalizado.Equals("general", StringComparison.OrdinalIgnoreCase))
        {
            qPestana = qPestana.Where(x => x.donacion_bien == null || x.donacion_bien.donacion == null || x.donacion_bien.donacion.id_evento == null);
        }
        else if (destinoNormalizado.Equals("eventos", StringComparison.OrdinalIgnoreCase))
        {
            qPestana = qPestana.Where(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null);
        }
        else if (int.TryParse(destinoNormalizado, out var idEv))
        {
            qPestana = qPestana.Where(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento == idEv);
        }

        // Conteo por categorías dentro de la pestaña activa
        var categoriasConConteo = await qPestana
            .Where(x => x.cantidad_actual > 0)
            .GroupBy(x => x.producto.categoria)
            .Select(g => new { Categoria = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Categoria, x => x.Cantidad);
        ViewBag.CategoriasConConteo = categoriasConConteo;
        ViewBag.AgotadosPestana = await qPestana.CountAsync(x => x.cantidad_actual <= 0);
        ViewBag.ConStockPestana = await qPestana.CountAsync(x => x.cantidad_actual > 0);

        var q = qPestana;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(x => x.producto.nombre.Contains(term) || x.ubicacion.Contains(term) || x.producto.categoria.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            q = q.Where(x => x.producto.categoria == categoria);
        }

        if (availableOnly)
        {
            q = q.Where(x => x.cantidad_actual > 0);
        }

        var total = await q.CountAsync();
        var filas = await q
            .OrderByDescending(x => x.cantidad_actual > 0)
            .ThenBy(x => x.producto.categoria)
            .ThenBy(x => x.producto.nombre)
            .ThenBy(x => x.id)
            .Skip((page - 1) * 10)
            .Take(10)
            .ToListAsync();

        // Conteo de productos por cada evento para mostrar en el selector de eventos
        var conteoPorEvento = await qConStock
            .Where(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null)
            .GroupBy(x => x.donacion_bien!.donacion!.id_evento!.Value)
            .Select(g => new { IdEvento = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.IdEvento, x => x.Cantidad);
        ViewBag.ConteoPorEvento = conteoPorEvento;

        ViewBag.Search = search;
        ViewBag.Destino = destinoNormalizado;
        ViewBag.Categoria = categoria;
        ViewBag.AvailableOnly = availableOnly;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = total;
        ViewBag.PendingCount = await db.Lotes.CountAsync(x => x.revision_pendiente);
        var ahora = HoraIglesia.Ahora;
        var eventosConStockIds = conteoPorEvento.Keys.ToHashSet();
        ViewBag.EventosFiltro = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado
                        && ((x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora || eventosConStockIds.Contains(x.id)))
            .OrderBy(x => x.fecha_inicio_evento)
            .ToListAsync();

        await CargarCatalogoYCategoriasAsync();
        return View(filas);
    }

    public async Task<IActionResult> Pending(int page = 1)
    {
        await servicio.AsegurarCatalogoBaseAsync();
        page = Math.Clamp(page, 1, 100000);

        var q = db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
                    .ThenInclude(x => x!.evento)
            .Where(x => x.revision_pendiente);

        var total = await q.CountAsync();
        var pageSize = 5;
        var lotes = await q
            .OrderBy(x => x.fecha_pendiente)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Names = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);
        var ahora = HoraIglesia.Ahora;
        ViewBag.Events = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora)
            .OrderBy(x => x.fecha_inicio_evento)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;

        await CargarCatalogoYCategoriasAsync();
        return View(lotes);
    }

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Confirm(
        int id,
        Guid commandId,
        IFormFile? photo,
        int? eventId,
        List<ItemCatalogacionAlmacen>? items,
        string? location,
        string? productName,
        string? category,
        decimal? quantity,
        string? unit)
    {
        try
        {
            if (photo != null)
            {
                var loteFoto = await db.Lotes.Include(x => x.donacion_bien).ThenInclude(x => x!.donacion).AsNoTracking().SingleOrDefaultAsync(x => x.id == id);
                var don = loteFoto?.donacion_bien?.donacion;
                if (don != null)
                    await servicioDonaciones.ReemplazarFotoBienAsync(don.id, photo, IdUsuario);
            }

            var listaItems = items?.Where(x => !string.IsNullOrWhiteSpace(x.nombre_producto) && x.cantidad is > 0).ToList() ?? [];
            if (listaItems.Count == 0 && !string.IsNullOrWhiteSpace(productName) && quantity is > 0)
            {
                listaItems.Add(new ItemCatalogacionAlmacen
                {
                    nombre_producto = productName,
                    categoria = category,
                    cantidad = quantity,
                    unidad = unit,
                    ubicacion = location
                });
            }

            var loteActual = await db.Lotes.Include(x => x.donacion_bien).ThenInclude(x => x!.donacion).AsNoTracking().SingleOrDefaultAsync(x => x.id == id);
            var idEventoFijado = loteActual?.donacion_bien?.donacion?.id_evento ?? eventId;
            await servicio.ConfirmarEntradaMultipleAsync(id, listaItems, idEventoFijado, IdUsuario, commandId);
            TempData["Success"] = listaItems.Count > 1
                ? $"Visto bueno registrado: se desglosaron y sumaron {listaItems.Count} productos al inventario."
                : "Visto bueno registrado: el producto se catalogó y sumó a las existencias.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (FormatException) { TempData["Error"] = "Verifica las cantidades ingresadas antes de confirmar."; }
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    public async Task<IActionResult> SaveCatalogProduct(
        int? id,
        string nombre,
        string categoria,
        string? nuevaCategoria,
        string unidad,
        string? origenVista)
    {
        try
        {
            var catFinal = categoria == "__nueva__" && !string.IsNullOrWhiteSpace(nuevaCategoria)
                ? nuevaCategoria.Trim()
                : categoria;
            await servicio.GuardarProductoCatalogoAsync(id, nombre, catFinal, unidad);
            TempData["Success"] = id is > 0
                ? $"Producto «{nombre.Trim()}» actualizado en el catálogo."
                : $"Producto «{nombre.Trim()}» agregado al desplegable de «{catFinal}».";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        if (string.Equals(origenVista, "Pending", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Pending));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> EditLot(
        int id,
        string productName,
        string category,
        string? newCategory,
        string unit,
        string location,
        decimal quantity,
        string? reason,
        string? returnDestino)
    {
        try
        {
            var catFinal = category == "__nueva__" && !string.IsNullOrWhiteSpace(newCategory)
                ? newCategory.Trim()
                : category;
            await servicio.EditarExistenciaAsync(id, productName, catFinal, unit, location, quantity, reason, IdUsuario);
            TempData["Success"] = "Existencia actualizada y cuadrada correctamente.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { destino = returnDestino ?? "general" });
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id, string? reason, Guid commandId)
    {
        try { await servicio.RechazarEntradaAsync(id, reason, IdUsuario, commandId); TempData["Success"] = "Entrada anulada; los bienes no ingresaron a las existencias."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (FormatException) { TempData["Error"] = "Actualiza el formulario antes de confirmar."; }
        return RedirectToAction(nameof(Pending));
    }

    [HttpGet]
    public async Task<IActionResult> Exit(int? evento, int? lote)
    {
        await CargarLotes();
        var lotsGeneral = (List<Lote>)ViewBag.LotsGeneral;
        var lotsEventos = (List<Lote>)ViewBag.LotsEventos;
        var eventosSalida = (List<Publicacion>)ViewBag.EventosSalida;
        var formulario = new FormularioSalidaAlmacen();

        // Si viene desde el botón "Retirar" de un lote reservado para evento, abrir directamente ese evento
        if (!evento.HasValue && lote.HasValue)
        {
            var loteEv = lotsEventos.FirstOrDefault(x => x.id == lote.Value);
            if (loteEv?.donacion_bien?.donacion?.id_evento is int idEvLote)
                evento = idEvLote;
        }

        if (evento.HasValue)
        {
            var ev = eventosSalida.FirstOrDefault(x => x.id == evento.Value);
            var lotesEvento = lotsEventos.Where(x => x.donacion_bien?.donacion?.id_evento == evento.Value && x.cantidad_actual > 0).ToList();
            if (ev != null)
            {
                formulario.destino = $"Evento: {ev.titulo}";
                formulario.detalle = "";
            }
            foreach (var l in lotesEvento)
            {
                formulario.lineas.Add(new LineaSalidaAlmacen { id_lote = l.id, cantidad = l.cantidad_actual });
            }
            ViewBag.EventoSeleccionadoId = evento.Value;
        }
        else if (lote.HasValue && lotsGeneral.Any(x => x.id == lote.Value))
        {
            formulario.lineas.Add(new LineaSalidaAlmacen { id_lote = lote.Value });
        }

        if (formulario.lineas.Count == 0)
            formulario.lineas.Add(new LineaSalidaAlmacen());

        return View(formulario);
    }

    [HttpPost]
    public async Task<IActionResult> Exit(FormularioSalidaAlmacen formulario, int? evento)
    {
        if (evento.HasValue && string.IsNullOrWhiteSpace(formulario.destino))
        {
            var ev = await db.Publicaciones.FindAsync(evento.Value);
            if (ev != null) formulario.destino = $"Evento: {ev.titulo}";
        }

        if (ModelState.IsValid)
            try { await servicio.RegistrarSalidaAsync(formulario, IdUsuario); TempData["Success"] = "Salida registrada; las existencias se descontaron."; return RedirectToAction(nameof(Index), new { destino = evento.HasValue ? evento.Value.ToString() : "general" }); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        if (formulario.lineas.Count == 0) formulario.lineas.Add(new LineaSalidaAlmacen());
        await CargarLotes();
        if (evento.HasValue) { ViewBag.EventoSeleccionadoId = evento.Value; ViewBag.EsReintento = true; }
        return View(formulario);
    }

    public async Task<IActionResult> Movements(string? search, string? tipo, string? destino, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);

        // Solo movimientos de existencias confirmadas (sin borradores pendientes ni anulaciones de puerta)
        var qBase = db.MovimientosAlmacen
            .Include(x => x.lote)
                .ThenInclude(x => x.producto)
            .Include(x => x.lote)
                .ThenInclude(x => x.donacion_bien)
                    .ThenInclude(x => x!.donacion)
                        .ThenInclude(x => x!.evento)
            .AsNoTracking()
            .Where(x => !x.lote.revision_pendiente
                        && x.lote.confirmado_por != null
                        && x.tipo != TipoMovimientoAlmacen.VistoBueno
                        && x.destino != ServicioAlmacen.DestinoAnulacion
                        && x.cantidad != 0);

        if (!string.IsNullOrWhiteSpace(destino))
        {
            if (destino.Equals("general", StringComparison.OrdinalIgnoreCase))
                qBase = qBase.Where(x => (x.lote.donacion_bien == null || x.lote.donacion_bien.donacion == null || x.lote.donacion_bien.donacion.id_evento == null) && (x.destino == null || !x.destino.StartsWith("Evento:")));
            else if (destino.Equals("eventos", StringComparison.OrdinalIgnoreCase))
                qBase = qBase.Where(x => (x.lote.donacion_bien != null && x.lote.donacion_bien.donacion != null && x.lote.donacion_bien.donacion.id_evento != null) || (x.destino != null && x.destino.StartsWith("Evento:")));
            else if (int.TryParse(destino, out var idEv))
            {
                var evTitulo = await db.Publicaciones.AsNoTracking().Where(x => x.id == idEv).Select(x => x.titulo).FirstOrDefaultAsync();
                qBase = qBase.Where(x => (x.lote.donacion_bien != null && x.lote.donacion_bien.donacion != null && x.lote.donacion_bien.donacion.id_evento == idEv)
                              || (evTitulo != null && x.destino != null && x.destino.Contains(evTitulo)));
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            qBase = qBase.Where(x => x.lote.producto.nombre.Contains(term) || x.lote.producto.categoria.Contains(term) || (x.destino != null && x.destino.Contains(term)) || x.motivo.Contains(term));
        }

        // Contadores de resumen (KPIs)
        ViewBag.ConteoEntradas = await qBase.CountAsync(x => x.cantidad > 0 && x.destino != ServicioAlmacen.DestinoAjuste);
        ViewBag.ConteoSalidas = await qBase.CountAsync(x => x.cantidad < 0 && x.destino != ServicioAlmacen.DestinoAjuste);
        ViewBag.ConteoAjustes = await qBase.CountAsync(x => x.destino == ServicioAlmacen.DestinoAjuste);

        var q = qBase;
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (tipo.Equals("entrada", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.cantidad > 0 && x.destino != ServicioAlmacen.DestinoAjuste);
            else if (tipo.Equals("salida", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.cantidad < 0 && x.destino != ServicioAlmacen.DestinoAjuste);
            else if (tipo.Equals("ajuste", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.destino == ServicioAlmacen.DestinoAjuste);
        }

        var total = await q.CountAsync();
        var filas = await q.OrderByDescending(x => x.id).Skip((page - 1) * 12).Take(12).ToListAsync();

        var saldosDict = new Dictionary<long, decimal>();
        if (filas.Count > 0)
        {
            var loteIds = filas.Select(x => x.id_lote).Distinct().ToList();
            var movimientosHistoricosLotes = await db.MovimientosAlmacen.AsNoTracking()
                .Where(x => loteIds.Contains(x.id_lote)
                         && x.tipo != TipoMovimientoAlmacen.VistoBueno
                         && x.destino != ServicioAlmacen.DestinoAnulacion)
                .OrderBy(x => x.id)
                .Select(x => new { x.id, x.id_lote, x.cantidad })
                .ToListAsync();

            var acumuladorPorLote = new Dictionary<int, decimal>();
            foreach (var mov in movimientosHistoricosLotes)
            {
                var anterior = acumuladorPorLote.GetValueOrDefault(mov.id_lote, 0m);
                var nuevo = anterior + mov.cantidad;
                acumuladorPorLote[mov.id_lote] = nuevo;
                saldosDict[mov.id] = nuevo;
            }
        }
        ViewBag.SaldosPorMovimiento = saldosDict;
        ViewBag.Names = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);
        ViewBag.EventosFiltro = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento)
            .OrderByDescending(x => x.fecha_inicio_evento)
            .ToListAsync();
        ViewBag.Search = search;
        ViewBag.Tipo = tipo;
        ViewBag.Destino = destino;
        ViewBag.Page = page;
        ViewBag.PageSize = 12;
        ViewBag.Total = total;
        return View(filas);
    }

    private async Task CargarCatalogoYCategoriasAsync()
    {
        var productosLimpios = await db.Productos.AsNoTracking()
            .Where(x => x.activo && x.categoria != "Por Clasificar")
            .OrderBy(x => x.categoria)
            .ThenBy(x => x.nombre)
            .ToListAsync();

        var listaCategorias = new HashSet<string>(ServicioAlmacen.CategoriasBase);
        foreach (var c in productosLimpios.Select(x => x.categoria).Where(c => !string.IsNullOrWhiteSpace(c)))
            listaCategorias.Add(c);

        ViewBag.Products = productosLimpios;
        ViewBag.Categorias = listaCategorias.OrderBy(x => x).ToList();
    }

    private async Task CargarLotes()
    {
        var todosConStock = await db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
                    .ThenInclude(x => x!.evento)
            .AsNoTracking()
            .Where(x => !x.revision_pendiente && x.producto.activo && x.cantidad_actual > 0)
            .OrderBy(x => x.producto.categoria)
            .ThenBy(x => x.producto.nombre)
            .ThenBy(x => x.ubicacion)
            .ToListAsync();

        // Separación estricta: General de la Iglesia vs. Reservados para Eventos
        var lotsGeneral = todosConStock
            .Where(x => x.donacion_bien == null || x.donacion_bien.donacion == null || x.donacion_bien.donacion.id_evento == null)
            .ToList();
        var lotsEventos = todosConStock
            .Where(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null)
            .ToList();

        ViewBag.LotsGeneral = lotsGeneral;
        ViewBag.LotsEventos = lotsEventos;
        ViewBag.Lots = todosConStock;
        ViewBag.CategoriasStockGeneral = lotsGeneral.Select(x => x.producto.categoria).Distinct().OrderBy(x => x).ToList();
        var ahora = HoraIglesia.Ahora;
        var idsEventosConStock = lotsEventos
            .Where(x => x.donacion_bien?.donacion?.id_evento != null)
            .Select(x => x.donacion_bien!.donacion!.id_evento!.Value)
            .Distinct()
            .ToHashSet();

        ViewBag.EventosSalida = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado
                        && ((x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora || idsEventosConStock.Contains(x.id)))
            .OrderBy(x => x.fecha_inicio_evento)
            .ToListAsync();
    }
}
