using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        q = period == "realizados" ? q.Where(x => x.tipo == TipoPublicacion.Evento && !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) < ahora) : q.Where(x => x.tipo == TipoPublicacion.Aviso || (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora);
        ViewBag.PageSize = 12; ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await q.CountAsync();
        ViewBag.RaisedByEvent = await db.Donaciones.Where(x => x.estado == EstadoDonacion.Aprobada && x.id_evento != null)
            .GroupBy(x => x.id_evento!.Value).Select(g => new { id_evento = g.Key, total = g.Sum(x => x.monto) })
            .ToDictionaryAsync(g => g.id_evento, g => g.total ?? 0m);
        var ordenado = period == "realizados" ? q.OrderByDescending(x => x.fecha_inicio_evento).ThenByDescending(x => x.fecha_creacion) : q.OrderBy(x => x.fecha_inicio_evento == null).ThenBy(x => x.fecha_inicio_evento).ThenByDescending(x => x.fecha_creacion);
        return View(await ordenado.Skip((Math.Clamp(page, 1, 100000) - 1) * 12).Take(12).ToListAsync());
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ModeloErrorVista { id_solicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
