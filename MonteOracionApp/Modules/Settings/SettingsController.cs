using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Settings;
[Authorize(Roles = Roles.Administrator)]
public class SettingsController(ApplicationDbContext db, IFileStorage storage) : Controller
{
    [HttpGet] public async Task<IActionResult> Index() => View(await db.ChurchSettings.SingleOrDefaultAsync() ?? new ChurchSettings());
    [HttpPost] public async Task<IActionResult> Index([Bind("Name,About,Address,Contact,Schedules")] ChurchSettings form)
    {
        if (!ModelState.IsValid) return View(form);
        var s = await db.ChurchSettings.SingleOrDefaultAsync(); if (s == null) { s = new(); db.ChurchSettings.Add(s); }
        s.Name = form.Name.Trim(); s.About = form.About.Trim(); s.Address = form.Address?.Trim(); s.Contact = form.Contact?.Trim(); s.Schedules = form.Schedules?.Trim();
        await db.SaveChangesAsync(); TempData["Success"] = "Información actualizada."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Payments() => View(await db.PaymentMethods.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
    [HttpGet] public async Task<IActionResult> Payment(int? id) { if (id == null) return View(new PaymentMethod()); var p = await db.PaymentMethods.FindAsync(id); return p == null ? NotFound() : View(p); }
    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Payment(int id, [Bind("Name,Instructions,AccountNumber,IsActive")] PaymentMethod form, IFormFile? qr)
    {
        form.Id = id; if (!ModelState.IsValid) return View(form);
        var p = id == 0 ? new PaymentMethod() : await db.PaymentMethods.FindAsync(id); if (p == null) return NotFound();
        StoredFile? uploaded = null; var old = p.QrStorageKey;
        if (qr != null)
        {
            try { uploaded = await storage.SaveAsync(qr, imageOnly: true); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(form); }
            p.QrStorageKey = uploaded.Key; p.QrContentType = uploaded.ContentType;
        }
        p.Name = form.Name.Trim(); p.Instructions = form.Instructions.Trim(); p.AccountNumber = form.AccountNumber?.Trim(); p.IsActive = form.IsActive;
        if (id == 0) db.PaymentMethods.Add(p);
        try { await db.SaveChangesAsync(); } catch { if (uploaded != null) storage.Delete(uploaded.Key); throw; }
        if (uploaded != null && old != null) storage.Delete(old);
        TempData["Success"] = "Medio de pago guardado."; return RedirectToAction(nameof(Payments));
    }
    [AllowAnonymous] public async Task<IActionResult> Qr(int id)
    {
        var p = await db.PaymentMethods.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (p?.QrStorageKey == null) return NotFound();
        Response.Headers.CacheControl = "no-store"; return File(storage.Open(p.QrStorageKey), p.QrContentType!);
    }
}
