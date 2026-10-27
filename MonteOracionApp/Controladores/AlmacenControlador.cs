using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.Equipo)]
public class InventoryController(ContextoBaseDatos db, ServicioAlmacen servicio) : Controller
{
    private string IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(string? search, bool availableOnly = false)
    {
        var q = db.Lotes.Include(x => x.producto).AsNoTracking().Where(x => !x.revision_pendiente);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.producto.nombre.Contains(search) || x.ubicacion.Contains(search));
        if (availableOnly) q = q.Where(x => x.cantidad_actual > 0);
        ViewBag.Search = search; ViewBag.AvailableOnly = availableOnly;
        ViewBag.PendingCount = await db.Lotes.CountAsync(x => x.revision_pendiente);
        if (User.IsInRole(Roles.Administrador))
            ViewBag.Products = await db.Productos.AsNoTracking().Where(x => x.activo).OrderBy(x => x.nombre).ToListAsync();
        return View(await q.OrderBy(x => x.producto.nombre).ThenBy(x => x.id).ToListAsync());
    }

    public async Task<IActionResult> Pending()
    {
        var lotes = await db.Lotes.Include(x => x.producto).Where(x => x.revision_pendiente).OrderBy(x => x.fecha_pendiente).ToListAsync();
        ViewBag.Names = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);
        return View(lotes);
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(int id, string location, Guid commandId)
    {
        try { await servicio.ConfirmarEntradaAsync(id, location, IdUsuario, commandId); TempData["Success"] = "Visto bueno registrado; ya se sumó en existencias."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (FormatException) { TempData["Error"] = "Actualiza el formulario antes de confirmar."; }
        return RedirectToAction(nameof(Pending));
    }

    [HttpGet]
    public async Task<IActionResult> Exit()
    {
        await CargarLotes();
        return View(new FormularioSalidaAlmacen { lineas = Enumerable.Range(0, 5).Select(_ => new LineaSalidaAlmacen()).ToList() });
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

    public async Task<IActionResult> Movements(string? search, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        var q = db.MovimientosAlmacen.Include(x => x.lote).ThenInclude(x => x.producto).AsNoTracking()
            .Where(x => x.tipo != TipoMovimientoAlmacen.VistoBueno);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.lote.producto.nombre.Contains(search) || (x.destino != null && x.destino.Contains(search)) || x.motivo.Contains(search));
        var filas = await q.OrderByDescending(x => x.id).Skip((page - 1) * 50).Take(50).ToListAsync();
        ViewBag.Names = await db.PerfilesPersonal.AsNoTracking().ToDictionaryAsync(x => x.id_usuario, x => x.nombre_completo);
        ViewBag.Search = search; ViewBag.Page = page; ViewBag.PageSize = 50; ViewBag.Total = await q.CountAsync();
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

    private async Task CargarLotes() =>
        ViewBag.Lots = await db.Lotes.Include(x => x.producto).AsNoTracking()
            .Where(x => !x.revision_pendiente && x.producto.activo && x.cantidad_actual > 0).OrderBy(x => x.producto.nombre).ThenBy(x => x.ubicacion).ToListAsync();
}
