using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.Equipo)]
public class DashboardController(ContextoBaseDatos db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var ahora = HoraIglesia.Ahora;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var perfil = userId != null
            ? await db.PerfilesPersonal.AsNoTracking().SingleOrDefaultAsync(x => x.id_usuario == userId)
            : null;
        ViewBag.NombrePersonal = !string.IsNullOrWhiteSpace(perfil?.nombre_completo)
            ? perfil.nombre_completo
            : (User.FindFirstValue("staff_name") ?? User.Identity?.Name ?? "Equipo");
        ViewBag.CargoPersonal = perfil?.cargo;

        ViewBag.Pending = await db.Donaciones.CountAsync(x => x.estado == EstadoDonacion.Pendiente);
        ViewBag.PendingStock = await db.Lotes.CountAsync(x => x.revision_pendiente);
        ViewBag.PendingMessages = await db.MensajesContacto.CountAsync(x => x.estado == EstadoMensajeContacto.Pendiente);
        ViewBag.DineroGeneral = await db.Donaciones
            .Where(x => x.estado == EstadoDonacion.Aprobada && x.tipo == TipoDonacion.Dinero && x.id_evento == null)
            .SumAsync(x => x.monto) ?? 0m;
        ViewBag.DineroEventos = await db.Donaciones
            .Where(x => x.estado == EstadoDonacion.Aprobada && x.tipo == TipoDonacion.Dinero && x.id_evento != null)
            .SumAsync(x => x.monto) ?? 0m;
        ViewBag.StockDisponible = await db.Lotes.CountAsync(x => !x.revision_pendiente && x.cantidad_actual > 0);

        var eventosActivos = await db.Publicaciones
            .AsNoTracking()
            .Where(x => x.tipo == TipoPublicacion.Evento && x.publicado && !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora)
            .OrderBy(x => x.fecha_inicio_evento)
            .Take(4)
            .ToListAsync();

        ViewBag.ProximosEventos = await db.Publicaciones.CountAsync(x => x.tipo == TipoPublicacion.Evento && x.publicado && !x.cancelado && (x.fecha_fin_evento ?? x.fecha_inicio_evento) >= ahora);
        ViewBag.ListaProximosEventos = eventosActivos;

        var idsEventos = eventosActivos.Select(e => e.id).ToList();
        var recaudadoLista = await db.Donaciones
            .AsNoTracking()
            .Where(x => x.id_evento != null && idsEventos.Contains(x.id_evento.Value) && x.estado == EstadoDonacion.Aprobada && x.tipo == TipoDonacion.Dinero)
            .GroupBy(x => x.id_evento!.Value)
            .Select(g => new { Id = g.Key, Total = g.Sum(x => x.monto) ?? 0m })
            .ToListAsync();
        ViewBag.RecaudadoPorEvento = recaudadoLista.ToDictionary(x => x.Id, x => x.Total);

        var lotesLista = await db.Lotes
            .AsNoTracking()
            .Where(x => !x.revision_pendiente && x.cantidad_actual > 0 && x.donacion_bien != null && x.donacion_bien.donacion != null && x.donacion_bien.donacion.id_evento != null && idsEventos.Contains(x.donacion_bien.donacion.id_evento.Value))
            .GroupBy(x => x.donacion_bien!.donacion!.id_evento!.Value)
            .Select(g => new { Id = g.Key, Cantidad = g.Count() })
            .ToListAsync();
        ViewBag.LotesPorEvento = lotesLista.ToDictionary(x => x.Id, x => x.Cantidad);

        return View();
    }
}
