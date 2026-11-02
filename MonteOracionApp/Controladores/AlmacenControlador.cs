using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.Equipo)]
public class InventoryController(ContextoBaseDatos db, ServicioAlmacen servicio, IServicioDonaciones servicioDonaciones) : Controller
{
    private string IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(string? search, string? destino, bool availableOnly = false, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        var q = db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
                    .ThenInclude(x => x!.evento)
            .AsNoTracking()
            .Where(x => !x.revision_pendiente);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.producto.nombre.Contains(search) || x.ubicacion.Contains(search));

        if (!string.IsNullOrWhiteSpace(destino))
        {
            if (destino.Equals("general", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.donacion_bien == null || x.donacion_bien.donacion == null || x.donacion_bien.donacion.id_evento == null);
            else if (destino.Equals("eventos", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null);
            else if (int.TryParse(destino, out var idEv))
                q = q.Where(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento == idEv);
        }

        if (availableOnly) q = q.Where(x => x.cantidad_actual > 0);

        var qDisponibles = q.Where(x => x.cantidad_actual > 0);
        ViewBag.LotesGeneral = await qDisponibles.CountAsync(x => x.donacion_bien == null || x.donacion_bien.donacion == null || x.donacion_bien.donacion.id_evento == null);
        ViewBag.LotesEventos = await qDisponibles.CountAsync(x => x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null);
        ViewBag.LotesDisponibles = (int)ViewBag.LotesGeneral + (int)ViewBag.LotesEventos;

        var total = await q.CountAsync();
        var filas = await q
            .OrderByDescending(x => x.cantidad_actual > 0)
            .ThenBy(x => x.producto.nombre)
            .ThenBy(x => x.id)
            .Skip((page - 1) * 10)
            .Take(10)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Destino = destino;
        ViewBag.AvailableOnly = availableOnly;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = total;
        ViewBag.PendingCount = await db.Lotes.CountAsync(x => x.revision_pendiente);
        ViewBag.EventosFiltro = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento)
            .OrderByDescending(x => x.fecha_inicio_evento)
            .ToListAsync();
        if (User.IsInRole(Roles.Administrador))
            ViewBag.Products = await db.Productos.AsNoTracking().Where(x => x.activo).OrderBy(x => x.nombre).ToListAsync();
        return View(filas);
    }

    public async Task<IActionResult> Pending()
    {
        var lotes = await db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
                    .ThenInclude(x => x!.evento)
            .Where(x => x.revision_pendiente)
            .OrderBy(x => x.fecha_pendiente)
            .ToListAsync();
        ViewBag.Names = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);
        return View(lotes);
    }

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Confirm(int id, string location, Guid commandId, IFormFile? photo)
    {
        try
        {
            if (photo != null)
            {
                var loteFoto = await db.Lotes.Include(x => x.donacion_bien).ThenInclude(x => x!.donacion).AsNoTracking().SingleOrDefaultAsync(x => x.id == id);
                var don = loteFoto?.donacion_bien?.donacion;
                if (don != null)
                    await servicioDonaciones.ReemplazarFotoBienAsync(don.id, photo, don.version, IdUsuario);
            }
            await servicio.ConfirmarEntradaAsync(id, location, IdUsuario, commandId);
            TempData["Success"] = "Visto bueno registrado; ya se sumó en existencias.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (FormatException) { TempData["Error"] = "Actualiza el formulario antes de confirmar."; }
        return RedirectToAction(nameof(Pending));
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
    public async Task<IActionResult> Exit(int? evento)
    {
        await CargarLotes();
        var lots = (List<Lote>)ViewBag.Lots;
        var eventosSalida = (List<Publicacion>)ViewBag.EventosSalida;
        var formulario = new FormularioSalidaAlmacen();

        if (evento.HasValue)
        {
            var ev = eventosSalida.FirstOrDefault(x => x.id == evento.Value);
            var lotesEvento = lots.Where(x => x.donacion_bien?.donacion?.id_evento == evento.Value && x.cantidad_actual > 0).ToList();
            if (ev != null)
            {
                formulario.destino = $"Evento: {ev.titulo}";
                formulario.detalle = $"Salida de víveres y bienes acumulados para el evento {ev.titulo}";
            }
            foreach (var l in lotesEvento)
            {
                formulario.lineas.Add(new LineaSalidaAlmacen { id_lote = l.id, cantidad = l.cantidad_actual });
            }
            ViewBag.EventoSeleccionadoId = evento.Value;
        }

        while (formulario.lineas.Count < 5)
            formulario.lineas.Add(new LineaSalidaAlmacen());

        return View(formulario);
    }

    [HttpPost]
    public async Task<IActionResult> Exit(FormularioSalidaAlmacen formulario)
    {
        if (ModelState.IsValid)
            try { await servicio.RegistrarSalidaAsync(formulario, IdUsuario); TempData["Success"] = "Salida registrada; las existencias se descontaron."; return RedirectToAction(nameof(Index)); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        while (formulario.lineas.Count < 5) formulario.lineas.Add(new LineaSalidaAlmacen());
        await CargarLotes(); return View(formulario);
    }

    public async Task<IActionResult> Movements(string? search, string? tipo, string? destino, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        var q = db.MovimientosAlmacen
            .Include(x => x.lote)
                .ThenInclude(x => x.producto)
            .Include(x => x.lote)
                .ThenInclude(x => x.donacion_bien)
                    .ThenInclude(x => x!.donacion)
                        .ThenInclude(x => x!.evento)
            .AsNoTracking()
            .Where(x => x.tipo != TipoMovimientoAlmacen.VistoBueno && x.cantidad != 0);

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (tipo.Equals("entrada", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.cantidad > 0);
            else if (tipo.Equals("salida", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.cantidad < 0);
        }

        if (!string.IsNullOrWhiteSpace(destino))
        {
            if (destino.Equals("general", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => (x.lote.donacion_bien == null || x.lote.donacion_bien.donacion == null || x.lote.donacion_bien.donacion.id_evento == null) && (x.destino == null || !x.destino.StartsWith("Evento:")));
            else if (destino.Equals("eventos", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => (x.lote.donacion_bien != null && x.lote.donacion_bien.donacion != null && x.lote.donacion_bien.donacion.id_evento != null) || (x.destino != null && x.destino.StartsWith("Evento:")));
            else if (int.TryParse(destino, out var idEv))
            {
                var evTitulo = await db.Publicaciones.AsNoTracking().Where(x => x.id == idEv).Select(x => x.titulo).FirstOrDefaultAsync();
                q = q.Where(x => (x.lote.donacion_bien != null && x.lote.donacion_bien.donacion != null && x.lote.donacion_bien.donacion.id_evento == idEv)
                              || (evTitulo != null && x.destino != null && x.destino.Contains(evTitulo)));
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.lote.producto.nombre.Contains(search) || (x.destino != null && x.destino.Contains(search)) || x.motivo.Contains(search));

        var total = await q.CountAsync();
        var filas = await q.OrderByDescending(x => x.id).Skip((page - 1) * 10).Take(10).ToListAsync();
        ViewBag.Names = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);
        ViewBag.EventosFiltro = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento)
            .OrderByDescending(x => x.fecha_inicio_evento)
            .ToListAsync();
        ViewBag.Search = search;
        ViewBag.Tipo = tipo;
        ViewBag.Destino = destino;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = total;
        return View(filas);
    }

    [HttpPost, Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ProductUnify(List<int>? duplicateIds, int targetId, string? correctName)
    {
        try
        {
            await servicio.UnificarProductosAsync(duplicateIds ?? [], targetId, correctName, IdUsuario);
            TempData["Success"] = "Productos unificados y existencias sumadas bajo el nombre correcto.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarLotes()
    {
        ViewBag.Lots = await db.Lotes
            .Include(x => x.producto)
            .Include(x => x.donacion_bien)
                .ThenInclude(x => x!.donacion)
                    .ThenInclude(x => x!.evento)
            .AsNoTracking()
            .Where(x => !x.revision_pendiente && x.producto.activo && x.cantidad_actual > 0)
            .OrderBy(x => x.producto.nombre)
            .ThenBy(x => x.ubicacion)
            .ToListAsync();
        ViewBag.EventosSalida = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado)
            .OrderByDescending(x => x.fecha_inicio_evento)
            .ToListAsync();
    }
}
