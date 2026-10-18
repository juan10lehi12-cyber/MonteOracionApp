using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Models;
using MonteOracionApp.Modules.Settings;
using System.Diagnostics;
namespace MonteOracionApp.Controllers;
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Church = await db.ChurchSettings.AsNoTracking().SingleOrDefaultAsync() ?? new ChurchSettings();
        ViewBag.PaymentMethods = await db.PaymentMethods.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
        var q = db.Publications.AsNoTracking().Where(x => x.IsPublished);
        ViewBag.PageSize = 12; ViewBag.Page = Math.Clamp(page, 1, 100000); ViewBag.Total = await q.CountAsync();
        return View(await q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((Math.Clamp(page, 1, 100000) - 1) * 12).Take(12).ToListAsync());
    }
    public IActionResult Privacy() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
