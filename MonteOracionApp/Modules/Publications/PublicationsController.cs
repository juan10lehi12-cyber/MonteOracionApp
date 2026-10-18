using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Publications;
[Authorize(Roles = Roles.Administrator)]
public class PublicationsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await db.Publications.CountAsync();
        return View(await db.Publications.AsNoTracking().OrderByDescending(x => x.Id).Skip((Math.Clamp(page, 1, 100000) - 1) * 30).Take(30).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Edit(int? id) { if (id == null) return View(new Publication()); var p = await db.Publications.FindAsync(id); return p == null ? NotFound() : View(p); }
    [HttpPost] public async Task<IActionResult> Edit(int id, [Bind("Kind,Title,Body,EventAt,Location,IsPublished")] Publication form)
    {
        if (!Enum.IsDefined(form.Kind)) ModelState.AddModelError("Kind", "Tipo inválido.");
        if (form.Kind == PublicationKind.Evento && (form.EventAt == null || string.IsNullOrWhiteSpace(form.Location))) ModelState.AddModelError("", "Los eventos necesitan fecha, hora y lugar.");
        if (!ModelState.IsValid) { form.Id = id; return View(form); }
        var p = id == 0 ? new Publication() : await db.Publications.FindAsync(id);
        if (p == null) return NotFound();
        if (p.IsCancelled) { TempData["Error"] = "Un evento cancelado se conserva sin modificaciones."; return RedirectToAction(nameof(Index)); }
        p.Kind = form.Kind; p.Title = form.Title.Trim(); p.Body = form.Body.Trim(); p.EventAt = form.Kind == PublicationKind.Evento ? form.EventAt : null;
        p.Location = form.Kind == PublicationKind.Evento ? form.Location?.Trim() : null; p.IsPublished = form.IsPublished;
        if (id == 0) db.Publications.Add(p); await db.SaveChangesAsync(); TempData["Success"] = "Publicación guardada."; return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Cancel(int id)
    {
        var p = await db.Publications.FindAsync(id); if (p == null || p.Kind != PublicationKind.Evento) return NotFound();
        p.IsCancelled = true; await db.SaveChangesAsync(); TempData["Success"] = "Evento cancelado. Se conserva para consulta."; return RedirectToAction(nameof(Index));
    }
}
