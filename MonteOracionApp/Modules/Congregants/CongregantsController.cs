using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Congregants;
[Authorize(Roles = Roles.Administrator)]
public class CongregantsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var q = db.Congregants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.FirstName.Contains(search) || x.LastName.Contains(search) || x.Dni == search);
        ViewBag.Search = search; ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await q.CountAsync();
        return View(await q.OrderBy(x => x.LastName).ThenBy(x => x.Id).Skip((Math.Clamp(page, 1, 100000) - 1) * 30).Take(30).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Edit(int? id) => id == null ? View(new Congregant()) : await db.Congregants.FindAsync(id) is { } c ? View(c) : NotFound();
    [HttpPost] public async Task<IActionResult> Edit(int id, [Bind("FirstName,LastName,Phone,Dni,BirthDate,Address")] Congregant form)
    {
        if (string.IsNullOrWhiteSpace(form.Dni)) ModelState.AddModelError("Dni", "El DNI es obligatorio para habilitar la consulta.");
        if (form.Dni != null && await db.Congregants.AnyAsync(x => x.Id != id && x.Dni == form.Dni)) ModelState.AddModelError("Dni", "Este DNI ya está registrado.");
        if (form.BirthDate.HasValue && (form.BirthDate.Value.Date > DateTime.Today.AddYears(-1) || form.BirthDate.Value.Date < DateTime.Today.AddYears(-120)))
            ModelState.AddModelError("BirthDate", "Revisa la fecha de nacimiento.");
        if (!ModelState.IsValid) { form.Id = id; return View(form); }
        var c = id == 0 ? new Congregant() : await db.Congregants.FindAsync(id);
        if (c == null) return NotFound();
        c.FirstName = form.FirstName.Trim(); c.LastName = form.LastName.Trim(); c.Phone = form.Phone?.Trim(); c.Dni = form.Dni;
        c.BirthDate = form.BirthDate?.Date; c.Address = form.Address?.Trim();
        if (id == 0) db.Congregants.Add(c);
        db.Audit(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, "Ficha de miembro guardada", $"Miembro #{id}");
        try { await db.SaveOperationAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("Dni", "El DNI ya se registró en otra operación. Revisa la ficha."); form.Id = id; return View(form); }
        TempData["Success"] = "Miembro guardado."; return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> SetActive(int id, bool active)
    {
        var c = await db.Congregants.FindAsync(id); if (c == null) return NotFound();
        c.IsActive = active; db.Audit(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, "Estado de miembro", $"Miembro #{id}", active.ToString());
        await db.SaveOperationAsync(); TempData["Success"] = "Estado actualizado."; return RedirectToAction(nameof(Index));
    }
}
