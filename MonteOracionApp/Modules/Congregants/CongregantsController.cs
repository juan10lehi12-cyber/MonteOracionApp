using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Accounts;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Congregants;
[Authorize(Roles = Roles.Administrator)]
public class CongregantsController(ApplicationDbContext db, CongregantService service) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var q = db.Congregants.Include(x => x.User).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.FirstName.Contains(search) || x.LastName.Contains(search));
        ViewBag.Search = search; ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await q.CountAsync();
        return View(await q.OrderBy(x => x.LastName).ThenBy(x => x.Id).Skip((Math.Clamp(page, 1, 100000) - 1) * 30).Take(30).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Edit(int? id) { if (id == null) return View(new Congregant()); var c = await db.Congregants.FindAsync(id); return c == null ? NotFound() : View(c); }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind("FirstName,LastName,Phone")] Congregant form)
    {
        if (!ModelState.IsValid) { form.Id = id; return View(form); }
        var c = id == 0 ? new Congregant() : await db.Congregants.FindAsync(id);
        if (c == null) return NotFound();
        c.FirstName = form.FirstName.Trim(); c.LastName = form.LastName.Trim(); c.Phone = form.Phone?.Trim();
        if (id == 0) db.Congregants.Add(c);
        await db.SaveChangesAsync(); TempData["Success"] = "Congregante guardado."; return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> SetActive(int id, bool active)
    {
        if (!await db.Congregants.AnyAsync(x => x.Id == id)) return NotFound();
        await service.SetActiveAsync(id, active); TempData["Success"] = active ? "Congregante activado." : "Congregante y acceso desactivados."; return RedirectToAction(nameof(Index));
    }
    [HttpGet] public async Task<IActionResult> CreateAccount(int id) { if (!await db.Congregants.AnyAsync(x => x.Id == id && x.IsActive && x.UserId == null)) return NotFound(); return View(new CreateAccountForm { CongregantId = id }); }
    [HttpPost] public async Task<IActionResult> CreateAccount(CreateAccountForm form)
    {
        if (!await db.Congregants.AnyAsync(x => x.Id == form.CongregantId)) return NotFound();
        if (!ModelState.IsValid) return View(form);
        try { await service.CreateAccountAsync(form); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(form); }
        TempData["Success"] = "Cuenta creada. Entrega las credenciales de forma privada al congregante."; return RedirectToAction(nameof(Index));
    }
    [HttpGet] public async Task<IActionResult> ResetPassword(int id) { if (!await db.Congregants.AnyAsync(x => x.Id == id && x.UserId != null)) return NotFound(); return View(new ResetPasswordForm { CongregantId = id }); }
    [HttpPost] public async Task<IActionResult> ResetPassword(ResetPasswordForm form)
    {
        if (!await db.Congregants.AnyAsync(x => x.Id == form.CongregantId)) return NotFound();
        if (!ModelState.IsValid) return View(form);
        try { await service.ResetPasswordAsync(form); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(form); }
        TempData["Success"] = "Contraseña restablecida. Entrega la nueva clave de forma privada."; return RedirectToAction(nameof(Index));
    }
}
