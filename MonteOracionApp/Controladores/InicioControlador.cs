using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

public class HomeController(ContextoBaseDatos db) : Controller
{
    public async Task<IActionResult> Index(int page = 1, string period = "proximos")
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        ViewBag.PaymentMethods = await db.MediosPago.AsNoTracking().Where(x => x.activo).OrderBy(x => x.nombre).ToListAsync();
        period = period == "realizados" ? "realizados" : "proximos"; ViewBag.Period = period;
        var ahora = HoraIglesia.Ahora; var q = db.Publicaciones.AsNoTracking().Where(x => x.publicado);
        var enCurso = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Evento && x.fecha_inicio_evento <= ahora && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora)
            .OrderBy(x => x.fecha_inicio_evento)
            .FirstOrDefaultAsync();
        var proximoDestacado = enCurso ?? await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Evento && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora)
            .OrderBy(x => x.fecha_inicio_evento)
            .FirstOrDefaultAsync() ?? await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Evento)
            .OrderByDescending(x => x.fecha_inicio_evento)
            .FirstOrDefaultAsync();
        ViewBag.FeaturedEvent = proximoDestacado;
        ViewBag.IsLiveEvent = enCurso != null;
        ViewBag.BannerEvents = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Evento && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora)
            .OrderBy(x => x.fecha_inicio_evento)
            .Take(5)
            .ToListAsync();
        ViewBag.Announcements = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Aviso)
            .OrderByDescending(x => x.fecha_creacion)
            .Take(8)
            .ToListAsync();
        q = period == "realizados"
            ? q.Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) < ahora)
            : q.Where(x => !x.cancelado && (x.tipo == TipoPublicacion.Aviso || (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora));
        ViewBag.PageSize = 12; ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await q.CountAsync();
        ViewBag.RaisedByEvent = await db.Donaciones.Where(x => x.estado == EstadoDonacion.Aprobada && x.id_evento != null)
            .GroupBy(x => x.id_evento!.Value).Select(g => new { id_evento = g.Key, total = g.Sum(x => x.monto) })
            .ToDictionaryAsync(g => g.id_evento, g => g.total ?? 0m);
        var ordenado = period == "realizados" ? q.OrderByDescending(x => x.fecha_inicio_evento).ThenByDescending(x => x.fecha_creacion) : q.OrderBy(x => x.fecha_inicio_evento == null).ThenBy(x => x.fecha_inicio_evento).ThenByDescending(x => x.fecha_creacion);
        return View(await ordenado.Skip((Math.Clamp(page, 1, 100000) - 1) * 12).Take(12).ToListAsync());
    }

    [Route("quienes-somos")]
    [Route("la-iglesia")]
    public async Task<IActionResult> QuienesSomos()
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        return View();
    }

    [Route("ministerios")]
    public async Task<IActionResult> Ministerios()
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        return View();
    }

    [Route("eventos")]
    public async Task<IActionResult> Eventos(string tab = "proximos", int? month = null, int? year = null)
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        var ahora = HoraIglesia.Ahora;

        var proximos = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Evento && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora)
            .OrderBy(x => x.fecha_inicio_evento)
            .ToListAsync();

        ViewBag.Podio = proximos.FirstOrDefault();
        ViewBag.Siguientes = proximos.Skip(1).ToList();

        ViewBag.Finalizados = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Evento && (x.fecha_fin_evento ?? x.fecha_inicio_evento) < ahora)
            .OrderByDescending(x => x.fecha_inicio_evento)
            .ToListAsync();

        var m = Math.Clamp(month ?? ahora.Month, 1, 12);
        var y = year is >= 2000 and <= 2100 ? year.Value : ahora.Year;
        var inicioMes = new DateTime(y, m, 1);
        ViewBag.Month = inicioMes;
        ViewBag.Prev = inicioMes.AddMonths(-1);
        ViewBag.Next = inicioMes.AddMonths(1);

        var eventosMes = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Evento && x.fecha_inicio_evento != null && x.fecha_inicio_evento >= inicioMes && x.fecha_inicio_evento < inicioMes.AddMonths(1))
            .OrderBy(x => x.fecha_inicio_evento)
            .ToListAsync();

        ViewBag.CalendarEvents = eventosMes.GroupBy(x => x.fecha_inicio_evento!.Value.Date).ToDictionary(g => g.Key, g => g.ToList());
        ViewBag.AllMonthEvents = eventosMes;

        ViewBag.AvisosDelMes = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Aviso)
            .OrderByDescending(x => x.fecha_creacion)
            .Take(5)
            .ToListAsync();

        ViewBag.ActiveTab = tab.ToLowerInvariant() switch
        {
            "calendario" or "agenda" => "calendario",
            "finalizados" or "recuerdos" => "finalizados",
            _ => "proximos"
        };

        return View();
    }

    [Route("avisos")]
    public async Task<IActionResult> Avisos()
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        ViewBag.Avisos = await db.Publicaciones.AsNoTracking()
            .Where(x => x.publicado && !x.cancelado && x.tipo == TipoPublicacion.Aviso)
            .OrderByDescending(x => x.fecha_creacion)
            .ToListAsync();
        return View();
    }

    [Route("obra-social")]
    public IActionResult ObraSocial() => RedirectToAction("Create", "Donations");

    [Route("horarios-y-mapa")]
    [Route("visitanos")]
    public async Task<IActionResult> Horarios()
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        return View();
    }

    [HttpGet]
    [Route("contacto")]
    [Route("peticion-oracion")]
    public async Task<IActionResult> Contacto(TipoMotivoContacto? motivo)
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        var formulario = new FormularioContacto();
        if (motivo.HasValue) formulario.motivo = motivo.Value;
        return View(formulario);
    }

    [HttpPost]
    [Route("contacto")]
    [Route("peticion-oracion")]
    [EnableRateLimiting("forms")]
    public async Task<IActionResult> Contacto(FormularioContacto formulario)
    {
        NormalizarContacto(formulario);
        ModelState.Clear();
        if (!TryValidateModel(formulario))
        {
            ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
            return View(formulario);
        }

        var soloDigitos = System.Text.RegularExpressions.Regex.Replace(formulario.telefono, "[^0-9]", "");
        var celular9 = soloDigitos.Length == 11 && soloDigitos.StartsWith("519") ? soloDigitos[2..] : soloDigitos;
        int? idCongregante = null;
        if (celular9.Length == 9)
        {
            idCongregante = await db.Congregantes.AsNoTracking()
                .Where(x => x.telefono == celular9)
                .Select(x => (int?)x.id)
                .FirstOrDefaultAsync();
        }

        var entidad = new MensajeContacto
        {
            id_congregante = idCongregante,
            nombre = formulario.nombre,
            telefono = formulario.telefono,
            correo = formulario.correo,
            motivo = formulario.motivo,
            preferencia = formulario.preferencia,
            mensaje = formulario.mensaje,
            estado = EstadoMensajeContacto.Pendiente
        };

        db.MensajesContacto.Add(entidad);
        await db.SaveChangesAsync();

        TempData["ContactoNombre"] = entidad.nombre;
        TempData["ContactoTelefono"] = entidad.telefono;
        return RedirectToAction(nameof(ContactoEnviado));
    }

    [HttpGet]
    [Route("contacto-enviado")]
    public async Task<IActionResult> ContactoEnviado()
    {
        ViewBag.Church = await db.ConfiguracionesIglesia.AsNoTracking().SingleOrDefaultAsync() ?? new ConfiguracionIglesia();
        ViewBag.Nombre = TempData["ContactoNombre"] as string;
        ViewBag.Telefono = TempData["ContactoTelefono"] as string;
        return View();
    }

    [Route("no-encontrado")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult NoEncontrado(int code = 404)
    {
        var estado = code is >= 400 and <= 599 ? code : 404;
        Response.StatusCode = estado;
        ViewData["Codigo"] = estado;
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ModeloErrorVista { id_solicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private static void NormalizarContacto(FormularioContacto formulario)
    {
        formulario.nombre = (formulario.nombre ?? "").Trim();
        formulario.telefono = (formulario.telefono ?? "").Trim();
        formulario.correo = string.IsNullOrWhiteSpace(formulario.correo) ? null : formulario.correo.Trim();
        formulario.mensaje = (formulario.mensaje ?? "").Trim();
    }
}
