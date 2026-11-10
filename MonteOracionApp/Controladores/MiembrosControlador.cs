using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.AccesoMiembros)]
public class CongregantsController(ContextoBaseDatos db) : Controller
{
    public async Task<IActionResult> Index(string? search, bool? active, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        var q = db.Congregantes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.nombres.Contains(search) || x.apellidos.Contains(search) || x.dni == search);
        if (active.HasValue)
            q = q.Where(x => x.activo == active.Value);

        var total = await q.CountAsync();
        var filas = await q.OrderBy(x => x.apellidos).ThenBy(x => x.id).Skip((page - 1) * 10).Take(10).ToListAsync();
        ViewBag.Search = search;
        ViewBag.Active = active;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = total;
        return View(filas);
    }

    [HttpGet] public async Task<IActionResult> Edit(int? id) => id == null ? View(new Congregante()) : await db.Congregantes.FindAsync(id) is { } c ? View(c) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind("nombres,apellidos,telefono,dni,fecha_nacimiento,direccion")] Congregante formulario)
    {
        if (string.IsNullOrWhiteSpace(formulario.dni)) ModelState.AddModelError("dni", "Ingresa el DNI del miembro.");
        if (formulario.dni != null && await db.Congregantes.AnyAsync(x => x.id != id && x.dni == formulario.dni)) ModelState.AddModelError("dni", "Este DNI ya está registrado.");
        if (formulario.fecha_nacimiento.HasValue && (formulario.fecha_nacimiento.Value.Date > DateTime.Today || formulario.fecha_nacimiento.Value.Date < DateTime.Today.AddYears(-120)))
            ModelState.AddModelError("fecha_nacimiento", "Revisa la fecha de nacimiento.");
        if (!ModelState.IsValid) { formulario.id = id; return View(formulario); }
        var c = id == 0 ? new Congregante() : await db.Congregantes.FindAsync(id);
        if (c == null) return NotFound();
        c.nombres = formulario.nombres.Trim(); c.apellidos = formulario.apellidos.Trim(); c.telefono = formulario.telefono?.Trim(); c.dni = string.IsNullOrWhiteSpace(formulario.dni) ? null : formulario.dni.Trim();
        c.fecha_nacimiento = formulario.fecha_nacimiento?.Date; c.direccion = formulario.direccion?.Trim();
        if (id == 0) db.Congregantes.Add(c);
        try { await db.GuardarOperacionAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("dni", "El DNI ya se registró en otra operación. Revisa la ficha."); formulario.id = id; return View(formulario); }
        TempData["Success"] = "Miembro guardado."; return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Administrador), HttpPost]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        var c = await db.Congregantes.FindAsync(id); if (c == null) return NotFound();
        c.activo = active;
        await db.GuardarOperacionAsync(); TempData["Success"] = "Estado actualizado."; return RedirectToAction(nameof(Index));
    }
}
