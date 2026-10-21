using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Modules.Inventory;
using MonteOracionApp.Modules.Treasury;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Dashboard;
[Authorize(Roles=Roles.Staff)]
public class DashboardController(ApplicationDbContext db):Controller
{
    public async Task<IActionResult>Index()
    {
        ViewBag.Pending=await db.Donations.CountAsync(x=>x.Status==DonationStatus.Pendiente);
        ViewBag.StockPending=await db.StockRequests.CountAsync(x=>x.Status==StockRequestStatus.Pendiente);
        ViewBag.FinancePending=await db.FinanceRequests.CountAsync(x=>x.Status==FinanceStatus.Pendiente);
        ViewBag.Expiring=await db.StockLots.CountAsync(x=>x.OnHand>0&&x.ExpiresOn<=ChurchTime.Today.AddDays(30));
        ViewBag.Loans=await db.StockRequests.CountAsync(x=>x.Kind==StockRequestKind.Prestamo&&x.ReturnDueOn<ChurchTime.Today&&x.Lines.Any(l=>l.Delivered>l.Returned+l.WrittenOff));
        ViewBag.Advances=await db.FinanceRequests.CountAsync(x=>x.Kind==FinanceKind.Anticipo&&x.Status==FinanceStatus.Pagada&&x.DueOn<ChurchTime.Today);
        return View();
    }
}
