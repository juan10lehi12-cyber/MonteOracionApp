using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.Administrador)]
public class PublicationsController(ContextoBaseDatos db, IAlmacenamientoArchivos almacenamiento) : Controller
{
    private string? IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Index(string? search, TipoPublicacion? tipo, string? estado, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        var ahora = HoraIglesia.Ahora;
        var q = db.Publicaciones.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.titulo.Contains(search) || x.contenido.Contains(search));
        if (tipo.HasValue)
            q = q.Where(x => x.tipo == tipo.Value);

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (estado.Equals("publicados", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.publicado && !x.cancelado);
            else if (estado.Equals("borradores", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => !x.publicado && !x.cancelado);
            else if (estado.Equals("cancelados", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.cancelado);
            else if (estado.Equals("proximos", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora);
            else if (estado.Equals("finalizados", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) < ahora);
        }

        var total = await q.CountAsync();
        var filas = await q.OrderByDescending(x => x.id).Skip((page - 1) * 10).Take(10).ToListAsync();
        ViewBag.Search = search;
        ViewBag.Tipo = tipo;
        ViewBag.Estado = estado;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = total;
        return View(filas);
    }

    public async Task<IActionResult> Recaudacion(string? filtro, string? q)
    {
        var ahora = HoraIglesia.Ahora;
        var grupos = await db.Donaciones.AsNoTracking()
            .Where(x => x.estado == EstadoDonacion.Aprobada && x.tipo == TipoDonacion.Dinero && x.id_evento != null)
            .GroupBy(x => x.id_evento!.Value)
            .Select(g => new { id_evento = g.Key, total = g.Sum(x => x.monto) ?? 0m, cantidad = g.Count() })
            .ToListAsync();
        var raised = grupos.ToDictionary(g => g.id_evento, g => g.total);
        ViewBag.Raised = raised;

        var eventosBase = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && x.fecha_inicio_evento != null)
            .Select(x => new { x.id, x.cancelado, fechaFin = x.fecha_fin_evento ?? x.fecha_inicio_evento })
            .ToListAsync();

        ViewBag.TotalTodos = raised.Values.Sum();
        ViewBag.TotalProximos = eventosBase
            .Where(x => !x.cancelado && x.fechaFin >= ahora)
            .Sum(x => raised.GetValueOrDefault(x.id));
        ViewBag.TotalFinalizados = eventosBase
            .Where(x => !x.cancelado && x.fechaFin < ahora)
            .Sum(x => raised.GetValueOrDefault(x.id));

        var lotesEvento = await db.Lotes.AsNoTracking()
            .Where(x => !x.revision_pendiente && x.cantidad_actual > 0 && x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null)
            .GroupBy(x => x.donacion_bien!.donacion!.id_evento!.Value)
            .Select(g => new { id_evento = g.Key, cantidad = g.Count() })
            .ToListAsync();
        ViewBag.GoodsByEvent = lotesEvento.ToDictionary(g => g.id_evento, g => g.cantidad);
        ViewBag.Filtro = filtro;
        ViewBag.Query = q?.Trim();

        var qEventos = db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && x.fecha_inicio_evento != null);

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            if (filtro.Equals("proximos", StringComparison.OrdinalIgnoreCase))
                qEventos = qEventos.Where(x => !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora);
            else if (filtro.Equals("finalizados", StringComparison.OrdinalIgnoreCase))
                qEventos = qEventos.Where(x => !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) < ahora);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            qEventos = qEventos.Where(x => x.titulo.Contains(term) || (x.lugar != null && x.lugar.Contains(term)));
        }

        return View(await qEventos.OrderByDescending(x => x.fecha_inicio_evento).Take(60).ToListAsync());
    }

    public async Task<IActionResult> Calendario(int? month, int? year)
    {
        var ahora = HoraIglesia.Ahora;
        var m = Math.Clamp(month ?? ahora.Month, 1, 12);
        var y = year is >= 2000 and <= 2100 ? year.Value : ahora.Year;
        var inicio = new DateTime(y, m, 1);
        ViewBag.Month = inicio; ViewBag.Prev = inicio.AddMonths(-1); ViewBag.Next = inicio.AddMonths(1);
        var eventos = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && x.fecha_inicio_evento != null && x.fecha_inicio_evento >= inicio && x.fecha_inicio_evento < inicio.AddMonths(1))
            .OrderBy(x => x.fecha_inicio_evento).ToListAsync();
        ViewBag.Events = eventos.GroupBy(x => x.fecha_inicio_evento!.Value.Date).ToDictionary(g => g.Key, g => g.ToList());
        return View();
    }

    [HttpGet] public async Task<IActionResult> Edit(int? id) => id == null ? View(new Publicacion { publicado = true }) : await db.Publicaciones.FindAsync(id) is { } p ? View(p) : NotFound();

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, [Bind("tipo,titulo,contenido,fecha_inicio_evento,fecha_fin_evento,lugar,meta_recaudacion,publicado")] Publicacion formulario, IFormFile? image)
    {
        if (!Enum.IsDefined(formulario.tipo)) ModelState.AddModelError("tipo", "Tipo inválido.");
        if (formulario.tipo == TipoPublicacion.Evento)
        {
            if (formulario.fecha_inicio_evento == null)
                ModelState.AddModelError(nameof(formulario.fecha_inicio_evento), "Ingresa la fecha y hora de inicio del evento.");
            if (formulario.fecha_fin_evento == null)
                ModelState.AddModelError(nameof(formulario.fecha_fin_evento), "Ingresa la fecha y hora de fin del evento.");
            else if (formulario.fecha_inicio_evento != null && formulario.fecha_fin_evento <= formulario.fecha_inicio_evento)
                ModelState.AddModelError(nameof(formulario.fecha_fin_evento), "La fecha de fin debe ser posterior a la fecha de inicio.");
            if (string.IsNullOrWhiteSpace(formulario.lugar))
                ModelState.AddModelError(nameof(formulario.lugar), "Indica el lugar donde se realizará el evento.");
        }
        if (!ModelState.IsValid) { formulario.id = id; return View(formulario); }
        var p = id == 0 ? new Publicacion() : await db.Publicaciones.FindAsync(id); if (p == null) return NotFound();
        if (p.cancelado) { TempData["Error"] = "El evento cancelado conserva su información y no puede modificarse."; return RedirectToAction(nameof(Index)); }
        ArchivoGuardado? subido = null; var anterior = p.clave_imagen;
        try { if (image != null) subido = await almacenamiento.GuardarAsync(image, true, "Eventos"); } catch (InvalidOperationException ex) { ModelState.AddModelError("image", ex.Message); formulario.id = id; return View(formulario); }
        if (formulario.tipo == TipoPublicacion.Evento && subido == null && p.clave_imagen == null) { ModelState.AddModelError("image", "Adjunta el afiche o imagen del evento."); formulario.id = id; return View(formulario); }
        p.tipo = formulario.tipo; p.titulo = formulario.titulo.Trim(); p.contenido = formulario.contenido.Trim(); p.publicado = formulario.publicado;
        p.fecha_inicio_evento = formulario.tipo == TipoPublicacion.Evento ? formulario.fecha_inicio_evento : null; p.fecha_fin_evento = formulario.tipo == TipoPublicacion.Evento ? formulario.fecha_fin_evento : null; p.lugar = formulario.tipo == TipoPublicacion.Evento ? formulario.lugar?.Trim() : null;
        p.meta_recaudacion = formulario.tipo == TipoPublicacion.Evento && formulario.meta_recaudacion > 0 ? formulario.meta_recaudacion : null;
        p.id_usuario ??= IdUsuario;
        if (subido != null) { p.clave_imagen = subido.clave; p.tipo_imagen = subido.tipo_contenido; }
        if (id == 0) db.Publicaciones.Add(p);
        try { await db.GuardarOperacionAsync(); } catch { if (subido != null) almacenamiento.Eliminar(subido.clave); throw; }
        if (subido != null && anterior != null) almacenamiento.Eliminar(anterior);
        TempData["Success"] = "Publicación guardada."; return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var p = await db.Publicaciones.FindAsync(id); if (p == null || p.tipo != TipoPublicacion.Evento) return NotFound();
        p.cancelado = true; await db.GuardarOperacionAsync(); return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Image(int id)
    {
        var p = await db.Publicaciones.AsNoTracking().SingleOrDefaultAsync(x => x.id == id && (x.publicado || User.IsInRole(Roles.Administrador)));
        if (p?.clave_imagen == null || !almacenamiento.Existe(p.clave_imagen)) return NotFound(); return File(almacenamiento.Abrir(p.clave_imagen), p.tipo_imagen!);
    }
}
