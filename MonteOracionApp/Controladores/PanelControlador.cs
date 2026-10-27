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
        ViewBag.Pending = await db.Donaciones.CountAsync(x => x.estado == EstadoDonacion.Pendiente);
        ViewBag.PendingStock = await db.Lotes.CountAsync(x => x.revision_pendiente);
        return View();
    }
}
