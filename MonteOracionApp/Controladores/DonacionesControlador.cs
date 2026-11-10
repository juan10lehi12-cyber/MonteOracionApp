using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

public class DonationsController(ContextoBaseDatos db, IServicioDonaciones servicio, IAlmacenamientoArchivos almacenamiento) : Controller
{
    private string IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task CargarMediosPago()
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        ViewBag.Payments = await db.MediosPago.AsNoTracking().Where(x => x.activo).OrderBy(x => x.nombre).ToListAsync();
        var hoyLima = DateTime.UtcNow.AddHours(-5).Date;
        ViewBag.Events = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && x.publicado && !x.cancelado && x.fecha_inicio_evento != null && x.fecha_inicio_evento.Value.Date >= hoyLima)
            .OrderBy(x => x.fecha_inicio_evento).ToListAsync();
    }

    private async Task CargarCatalogos()
    {
        ViewBag.Congregants = await db.Congregantes.AsNoTracking().Where(x => x.activo).OrderBy(x => x.apellidos).ToListAsync();
        ViewBag.Products = await db.Productos.Where(x => x.activo).OrderBy(x => x.nombre).ToListAsync();
        var hoyLima = DateTime.UtcNow.AddHours(-5).Date;
        ViewBag.Events = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && x.publicado && !x.cancelado && x.fecha_inicio_evento != null && x.fecha_inicio_evento.Value.Date >= hoyLima)
            .OrderBy(x => x.fecha_inicio_evento).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? evento)
    {
        await CargarMediosPago();
        var formulario = new FormularioDonacionWeb();
        if (evento.HasValue) formulario.id_evento = evento;
        return View(formulario);
    }

    [HttpPost, EnableRateLimiting("forms"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(FormularioDonacionWeb formulario)
    {
        if (!formulario.anonimo && string.IsNullOrWhiteSpace(formulario.dni))
            ModelState.AddModelError("dni", "Ingresa tu DNI o marca la casilla de donación anónima.");
        if (!ModelState.IsValid) { await CargarMediosPago(); return View(formulario); }
        try
        {
            var d = await servicio.RegistrarWebAsync(formulario, null);
            TempData["DonationCode"] = d.codigo_seguimiento.ToString("N"); return RedirectToAction(nameof(Submitted));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); await CargarMediosPago(); return View(formulario); }
    }

    [HttpGet] public IActionResult Submitted() { var codigo = TempData["DonationCode"] as string; return codigo == null ? RedirectToAction(nameof(Create)) : View((object)codigo); }

    [Authorize(Roles = Roles.AccesoDonaciones)]
    public async Task<IActionResult> Index(FiltroDonacion filtro)
    {
        filtro.pagina = Math.Clamp(filtro.pagina, 1, 100000);
        ViewBag.EventosFiltro = await db.Publicaciones.AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento)
            .OrderByDescending(x => x.fecha_inicio_evento)
            .ToListAsync();

        var q = db.Donaciones.Include(x => x.congregante).Include(x => x.evento).Include(x => x.bienes).AsSplitQuery().AsNoTracking();
        if (filtro.estado.HasValue) q = q.Where(x => x.estado == filtro.estado);
        if (filtro.tipo.HasValue) q = q.Where(x => x.tipo == filtro.tipo);
        if (filtro.tipo_donante.HasValue) q = q.Where(x => x.tipo_donante == filtro.tipo_donante);
        if (filtro.origen.HasValue) q = q.Where(x => x.digital == (filtro.origen == OrigenDonacion.Digital));

        if (filtro.id_evento.HasValue && string.IsNullOrWhiteSpace(filtro.destino))
            filtro.destino = filtro.id_evento.Value.ToString();

        if (!string.IsNullOrWhiteSpace(filtro.destino))
        {
            if (filtro.destino.Equals("general", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.id_evento == null);
            else if (filtro.destino.Equals("eventos", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.id_evento != null);
            else if (int.TryParse(filtro.destino, out var idEv))
                q = q.Where(x => x.id_evento == idEv);
        }

        if (filtro.desde.HasValue) q = q.Where(x => x.fecha_recepcion_pago >= filtro.desde.Value.Date);
        if (filtro.hasta.HasValue) q = q.Where(x => x.fecha_recepcion_pago <= filtro.hasta.Value.Date);
        if (!string.IsNullOrWhiteSpace(filtro.donante))
        {
            var termino = filtro.donante.Trim();
            if (Guid.TryParse(termino, out var codigo)) q = q.Where(x => x.codigo_seguimiento == codigo);
            else if (int.TryParse(termino.TrimStart('#'), out var numero)) q = q.Where(x => x.id == numero || (x.numero_operacion != null && x.numero_operacion.Contains(termino)));
            else q = q.Where(x => (x.numero_operacion != null && x.numero_operacion.Contains(termino)) || (x.nombre_externo != null && x.nombre_externo.Contains(termino)) || (x.congregante != null && (x.congregante.nombres + " " + x.congregante.apellidos).Contains(termino)));
        }
        var total = await q.CountAsync();
        var qDineroAprobado = q.Where(x => x.estado == EstadoDonacion.Aprobada && x.tipo == TipoDonacion.Dinero);
        var dineroGeneral = await qDineroAprobado.Where(x => x.id_evento == null).SumAsync(x => x.monto) ?? 0m;
        var dineroEventos = await qDineroAprobado.Where(x => x.id_evento != null).SumAsync(x => x.monto) ?? 0m;
        var dineroTotal = dineroGeneral + dineroEventos;
        ViewBag.PageSize = 10;
        ViewBag.PendientesCount = await db.Donaciones.CountAsync(x => x.estado == EstadoDonacion.Pendiente);
        ViewBag.MontoPendiente = await q.Where(x => x.estado == EstadoDonacion.Pendiente && x.tipo == TipoDonacion.Dinero).SumAsync(x => x.monto) ?? 0m;
        var filas = await q.OrderByDescending(x => x.id).Skip((filtro.pagina - 1) * 10).Take(10).ToListAsync();
        return View(new ListaDonaciones(filas, filtro, total, dineroTotal, dineroGeneral, dineroEventos));
    }

    [Authorize(Roles = Roles.AccesoDonaciones), HttpGet] public async Task<IActionResult> Receive() { await CargarCatalogos(); return View(new FormularioRecepcionDonacion()); }

    [Authorize(Roles = Roles.AccesoDonaciones), HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Receive(FormularioRecepcionDonacion formulario)
    {
        if (!ModelState.IsValid) { await CargarCatalogos(); return View(formulario); }
        try { var d = await servicio.RegistrarRecepcionAsync(formulario, IdUsuario); TempData["Success"] = "Donación recibida y aprobada."; return RedirectToAction(nameof(Details), new { id = d.id }); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); await CargarCatalogos(); return View(formulario); }
    }

    [Authorize(Roles = Roles.AccesoDonaciones)]
    public async Task<IActionResult> Details(int id)
    {
        var d = await db.Donaciones.Include(x => x.congregante).Include(x => x.evento).Include(x => x.bienes).Include(x => x.decisiones).AsSplitQuery().AsNoTracking().SingleOrDefaultAsync(x => x.id == id);
        if (d != null && User.IsInRole(Roles.Administrador) && d.estado == EstadoDonacion.Pendiente && d.id_congregante == null)
            await CargarCatalogos();
        return d == null ? NotFound() : View(d);
    }

    [Authorize(Roles = Roles.Equipo)]
    public async Task<IActionResult> Receipt(int id)
    {
        var d = await db.Donaciones.AsNoTracking().SingleOrDefaultAsync(x => x.id == id);
        if (d == null) return NotFound();
        if (!Roles.PuedeDonaciones(User) && d.tipo != TipoDonacion.Bienes) return Forbid();
        if (d.clave_comprobante == null || !almacenamiento.Existe(d.clave_comprobante)) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return File(almacenamiento.Abrir(d.clave_comprobante), d.tipo_contenido_comprobante!);
    }

    [Authorize(Roles = Roles.Equipo), HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ReplaceItemPhoto(int id, IFormFile? photo)
    {
        try
        {
            if (photo == null) throw new InvalidOperationException("Selecciona una foto JPG o PNG.");
            await servicio.ReemplazarFotoBienAsync(id, photo, IdUsuario);
            TempData["Success"] = "Foto del bien actualizada.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Roles.Administrador), HttpPost]
    public async Task<IActionResult> Decide(int id, EstadoDonacion status, string? reason, bool confirmReceived, int? congregantId)
    {
        try
        {
            if (status == EstadoDonacion.Aprobada && !confirmReceived) throw new InvalidOperationException("Confirma la recepción efectiva antes de aprobar.");
            await servicio.DecidirAsync(id, status, reason, IdUsuario, congregantId);
            TempData["Success"] = "Decisión registrada.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
