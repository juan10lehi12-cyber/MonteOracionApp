using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Modules.Publications;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Portal;
public class ConsultationForm
{
    [Required, RegularExpression("^[0-9]{8}$", ErrorMessage = "Escribe los ocho dígitos del DNI."), Display(Name = "DNI")] public string Dni { get; set; } = "";
    public int Page { get; set; } = 1;
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public List<Donation>? Donations { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public List<Publication> Events { get; set; } = [];
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public int Total { get; set; }
}
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PortalController(ApplicationDbContext db) : Controller
{
    [HttpGet] public IActionResult Index() => View(new ConsultationForm());
    [HttpPost, EnableRateLimiting("consulta")]
    public async Task<IActionResult> Index(ConsultationForm form)
    {
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        if (!ModelState.IsValid) return View(form);
        var c = await db.Congregants.AsNoTracking().SingleOrDefaultAsync(x => x.Dni == form.Dni && x.IsActive);
        form.Donations = [];
        if (c != null)
        {
            var q = db.Donations.AsNoTracking().Where(x => x.CongregantId == c.Id);
            form.Page = Math.Clamp(form.Page, 1, 100000); form.Total = await q.CountAsync();
            form.Donations = await q.Include(x => x.Items).OrderByDescending(x => x.ReceivedOrPaidOn).ThenByDescending(x => x.Id).Skip((form.Page - 1) * 15).Take(15).ToListAsync();
            form.Events = await db.EventParticipants.AsNoTracking().Where(x => x.IsActive && x.CongregantId == c.Id && x.Publication.IsPublished && !x.Publication.IsCancelled && x.Publication.EventAt >= ChurchTime.Now)
                .Select(x => x.Publication).OrderBy(x => x.EventAt).ToListAsync();
        }
        return View(form);
    }
}
