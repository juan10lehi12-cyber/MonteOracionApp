using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Portal;
[Authorize(Roles = Roles.Congregant)]
public class PortalController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var c = await db.Congregants.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == uid && x.IsActive);
        if (c == null) return Forbid();
        ViewBag.Congregant = c;
        var q = db.Donations.AsNoTracking().Where(x => x.CongregantId == c.Id);
        ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await q.CountAsync();
        return View(await q.OrderByDescending(x => x.Id).Skip((Math.Clamp(page, 1, 100000) - 1) * 30).Take(30).ToListAsync());
    }
}
