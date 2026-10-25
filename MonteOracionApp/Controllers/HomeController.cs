using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Models;
using MonteOracionApp.Modules.Settings;
using MonteOracionApp.Modules.Publications;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Shared;
using System.Diagnostics;
namespace MonteOracionApp.Controllers;
public class HomeController(ApplicationDbContext db):Controller
{
    public async Task<IActionResult>Index(int page=1,string period="proximos")
    {
        ViewBag.Church=await db.ChurchSettings.AsNoTracking().SingleOrDefaultAsync()??new ChurchSettings();
        ViewBag.PaymentMethods=await db.PaymentMethods.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync();
        period=period=="realizados"?"realizados":"proximos";ViewBag.Period=period;
        var now=ChurchTime.Now;var q=db.Publications.AsNoTracking().Where(x=>x.IsPublished);
        q=period=="realizados"?q.Where(x=>x.Kind==PublicationKind.Evento&&!x.IsCancelled&&(x.EventEndsAt??x.EventAt)<now):q.Where(x=>x.Kind==PublicationKind.Aviso||(x.EventEndsAt??x.EventAt)>=now);
        ViewBag.PageSize=12;ViewBag.Page=Math.Clamp(page,1,100000);ViewBag.Total=await q.CountAsync();
        var hoyLima=ChurchTime.Now;
        ViewBag.RaisedByEvent=await db.Donations.Where(x=>x.Status==DonationStatus.Aprobada&&x.EventId!=null)
            .GroupBy(x=>x.EventId!.Value).Select(g=>new{EventId=g.Key,Total=g.Sum(x=>x.Amount)})
            .ToDictionaryAsync(g=>g.EventId,g=>g.Total??0m);
        var ordered=period=="realizados"?q.OrderByDescending(x=>x.EventAt).ThenByDescending(x=>x.CreatedAt):q.OrderBy(x=>x.EventAt==null).ThenBy(x=>x.EventAt).ThenByDescending(x=>x.CreatedAt);
        return View(await ordered.Skip((Math.Clamp(page,1,100000)-1)*12).Take(12).ToListAsync());
    }
    public IActionResult Privacy()=>View();
    [ResponseCache(Duration=0,Location=ResponseCacheLocation.None,NoStore=true)]public IActionResult Error()=>View(new ErrorViewModel{RequestId=Activity.Current?.Id??HttpContext.TraceIdentifier});
}
