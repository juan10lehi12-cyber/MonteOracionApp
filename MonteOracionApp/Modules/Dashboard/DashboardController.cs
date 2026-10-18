using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Dashboard;
[Authorize(Roles = Roles.Administrator)]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Pending = await db.Donations.CountAsync(x => x.Status == DonationStatus.Pendiente);
        ViewBag.Congregants = await db.Congregants.CountAsync(x => x.IsActive);
        ViewBag.Money = await db.Donations.Where(x => x.Status == DonationStatus.Aprobada && x.Kind == DonationKind.Dinero).SumAsync(x => x.Amount) ?? 0;
        ViewBag.Goods = await db.Donations.CountAsync(x => x.Status == DonationStatus.Aprobada && x.Kind == DonationKind.Bienes);
        return View();
    }
}
