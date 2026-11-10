using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;

namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.AccesoMensajes)]
public class MessagesController(ContextoBaseDatos db) : Controller
{
    private string IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(FiltroMensajeContacto filtro)
    {
        filtro.pagina = Math.Clamp(filtro.pagina, 1, 100000);
        var q = db.MensajesContacto.AsNoTracking();

        if (filtro.estado.HasValue)
            q = q.Where(x => x.estado == filtro.estado.Value);

        if (filtro.motivo.HasValue)
            q = q.Where(x => x.motivo == filtro.motivo.Value);

        if (!string.IsNullOrWhiteSpace(filtro.buscar))
        {
            var b = filtro.buscar.Trim();
            q = q.Where(x => x.nombre.Contains(b) || x.telefono.Contains(b) || (x.correo != null && x.correo.Contains(b)) || x.mensaje.Contains(b));
        }

        if (filtro.desde.HasValue)
            q = q.Where(x => x.fecha_creacion >= filtro.desde.Value.Date);

        if (filtro.hasta.HasValue)
            q = q.Where(x => x.fecha_creacion < filtro.hasta.Value.Date.AddDays(1));

        var total = await q.CountAsync();
        var filas = await q.OrderByDescending(x => x.id)
            .Skip((filtro.pagina - 1) * 10)
            .Take(10)
            .ToListAsync();

        ViewBag.Filtro = filtro;
        ViewBag.Total = total;
        ViewBag.Page = filtro.pagina;
        ViewBag.PageSize = 10;
        ViewBag.PendientesCount = await db.MensajesContacto.CountAsync(x => x.estado == EstadoMensajeContacto.Pendiente);
        ViewBag.AtendidosCount = await db.MensajesContacto.CountAsync(x => x.estado == EstadoMensajeContacto.Atendido);
        ViewBag.ArchivadosCount = await db.MensajesContacto.CountAsync(x => x.estado == EstadoMensajeContacto.Archivado);
        ViewBag.StaffNames = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);

        return View(filas);
    }

    public async Task<IActionResult> Details(int id)
    {
        var mensaje = await db.MensajesContacto.SingleOrDefaultAsync(x => x.id == id);
        if (mensaje == null) return NotFound();

        ViewBag.StaffNames = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);
        return View(mensaje);
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, EstadoMensajeContacto nuevoEstado, string? notas)
    {
        var mensaje = await db.MensajesContacto.SingleOrDefaultAsync(x => x.id == id);
        if (mensaje == null) return NotFound();

        mensaje.estado = nuevoEstado;
        mensaje.notas_atencion = notas?.Trim();

        if (nuevoEstado == EstadoMensajeContacto.Atendido)
        {
            mensaje.fecha_atencion = DateTimeOffset.UtcNow;
            mensaje.id_usuario = IdUsuario;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = nuevoEstado switch
        {
            EstadoMensajeContacto.Atendido => "Mensaje marcado como ATENDIDO.",
            EstadoMensajeContacto.Archivado => "Mensaje ARCHIVADO.",
            _ => "Estado actualizado a PENDIENTE."
        };

        return RedirectToAction(nameof(Details), new { id });
    }
}
