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
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await db.Publicaciones.CountAsync();
        return View(await db.Publicaciones.AsNoTracking().OrderByDescending(x => x.id).Skip((Math.Clamp(page, 1, 100000) - 1) * 30).Take(30).ToListAsync());
    }

    public async Task<IActionResult> Recaudacion()
    {
        var grupos = await db.Donaciones.AsNoTracking()
            .Where(x => x.estado == EstadoDonacion.Aprobada && x.id_evento != null)
            .GroupBy(x => x.id_evento!.Value)
            .Select(g => new { id_evento = g.Key, total = g.Sum(x => x.monto) ?? 0m, cantidad = g.Count() })
            .ToListAsync();
        ViewBag.Raised = grupos.ToDictionary(g => g.id_evento, g => g.total);
        ViewBag.General = await db.Donaciones.AsNoTracking()
            .Where(x => x.estado == EstadoDonacion.Aprobada && x.id_evento == null)
            .SumAsync(x => x.monto) ?? 0m;
        return View(await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && x.fecha_inicio_evento != null)
            .OrderByDescending(x => x.fecha_inicio_evento).Take(60).ToListAsync());
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

    [HttpGet] public async Task<IActionResult> Edit(int? id) => id == null ? View(new Publicacion()) : await db.Publicaciones.FindAsync(id) is { } p ? View(p) : NotFound();

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, [Bind("tipo,titulo,contenido,fecha_inicio_evento,fecha_fin_evento,lugar,meta_recaudacion,publicado")] Publicacion formulario, IFormFile? image)
    {
        if (!Enum.IsDefined(formulario.tipo)) ModelState.AddModelError("tipo", "Tipo inválido.");
        if (formulario.tipo == TipoPublicacion.Evento && (formulario.fecha_inicio_evento == null || string.IsNullOrWhiteSpace(formulario.lugar) || formulario.fecha_fin_evento == null || formulario.fecha_fin_evento <= formulario.fecha_inicio_evento)) ModelState.AddModelError("", "Los eventos requieren inicio, fin posterior y lugar.");
        if (!ModelState.IsValid) { formulario.id = id; return View(formulario); }
        var p = id == 0 ? new Publicacion() : await db.Publicaciones.FindAsync(id); if (p == null) return NotFound();
        if (p.cancelado) { TempData["Error"] = "El evento cancelado conserva su información."; return RedirectToAction(nameof(Index)); }
        ArchivoGuardado? subido = null; var anterior = p.clave_imagen;
        try { if (image != null) subido = await almacenamiento.GuardarAsync(image, true); } catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); formulario.id = id; return View(formulario); }
        if (formulario.tipo == TipoPublicacion.Evento && subido == null && p.clave_imagen == null) { ModelState.AddModelError("", "Adjunta la imagen del evento."); formulario.id = id; return View(formulario); }
        p.tipo = formulario.tipo; p.titulo = formulario.titulo.Trim(); p.contenido = formulario.contenido.Trim(); p.publicado = formulario.publicado;
        p.fecha_inicio_evento = formulario.tipo == TipoPublicacion.Evento ? formulario.fecha_inicio_evento : null; p.fecha_fin_evento = formulario.tipo == TipoPublicacion.Evento ? formulario.fecha_fin_evento : null; p.lugar = formulario.tipo == TipoPublicacion.Evento ? formulario.lugar?.Trim() : null;
        p.meta_recaudacion = formulario.tipo == TipoPublicacion.Evento && formulario.meta_recaudacion > 0 ? formulario.meta_recaudacion : null;
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
        if (p?.clave_imagen == null) return NotFound(); return File(almacenamiento.Abrir(p.clave_imagen), p.tipo_imagen!);
    }
}
