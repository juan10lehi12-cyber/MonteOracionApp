using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Donations;
public class DonationsController(ApplicationDbContext db, IDonationService service, IFileStorage storage) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task LoadPayments() => ViewBag.Payments = await db.PaymentMethods.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    private async Task LoadCongregants() => ViewBag.Congregants = await db.Congregants.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.LastName).ToListAsync();
    [HttpGet] public async Task<IActionResult> Create() { await LoadPayments(); return View(new OnlineDonationForm()); }
    [HttpPost, EnableRateLimiting("forms"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(OnlineDonationForm form)
    {
        if (!ModelState.IsValid) { await LoadPayments(); return View(form); }
        try
        {
            var d = await service.RegisterOnlineAsync(form, User.Identity?.IsAuthenticated == true ? UserId : null);
            TempData["DonationCode"] = d.TrackingCode.ToString("N"); return RedirectToAction(nameof(Submitted));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); await LoadPayments(); return View(form); }
    }
    [HttpGet] public IActionResult Submitted() { var code = TempData["DonationCode"] as string; return code == null ? RedirectToAction(nameof(Create)) : View((object)code); }
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Index(DonationFilter filter)
    {
        filter.Page = Math.Clamp(filter.Page, 1, 100000);
        var q = db.Donations.Include(x => x.Congregant).AsNoTracking();
        if (filter.Status.HasValue) q = q.Where(x => x.Status == filter.Status);
        if (filter.Kind.HasValue) q = q.Where(x => x.Kind == filter.Kind);
        if (filter.From.HasValue) q = q.Where(x => x.ReceivedOrPaidOn >= filter.From.Value.Date);
        if (filter.To.HasValue) q = q.Where(x => x.ReceivedOrPaidOn <= filter.To.Value.Date);
        if (!string.IsNullOrWhiteSpace(filter.Donor))
        {
            var term = filter.Donor.Trim();
            if (Guid.TryParse(term, out var code)) q = q.Where(x => x.TrackingCode == code);
            else if (int.TryParse(term.TrimStart('#'), out var number)) q = q.Where(x => x.Id == number);
            else q = q.Where(x => (x.ExternalName != null && x.ExternalName.Contains(term)) || (x.Congregant != null && (x.Congregant.FirstName + " " + x.Congregant.LastName).Contains(term)));
        }
        var count = await q.CountAsync();
        var money = await q.Where(x => x.Status == DonationStatus.Aprobada && x.Kind == DonationKind.Dinero).SumAsync(x => x.Amount) ?? 0;
        return View(new DonationList(await q.OrderByDescending(x => x.Id).Skip((filter.Page - 1) * 30).Take(30).ToListAsync(), filter, count, money));
    }
    [Authorize(Roles = Roles.Administrator), HttpGet] public async Task<IActionResult> Receive() { await LoadCongregants(); return View(new ReceivedDonationForm()); }
    [Authorize(Roles = Roles.Administrator), HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Receive(ReceivedDonationForm form)
    {
        if (!ModelState.IsValid) { await LoadCongregants(); return View(form); }
        try { var d = await service.RegisterReceivedAsync(form, UserId); TempData["Success"] = "Donación recibida y aprobada."; return RedirectToAction(nameof(Details), new { id = d.Id }); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); await LoadCongregants(); return View(form); }
    }
    private IQueryable<Donation> VisibleDonations()
    {
        var q = db.Donations.AsQueryable();
        if (!User.IsInRole(Roles.Administrator)) q = q.Where(x => x.Congregant != null && x.Congregant.UserId == UserId && x.Congregant.IsActive);
        return q;
    }
    [Authorize] public async Task<IActionResult> Details(int id)
    {
        var d = await VisibleDonations().Include(x => x.Congregant).Include(x => x.Items).Include(x => x.Decisions).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (d != null && User.IsInRole(Roles.Administrator) && d.Status == DonationStatus.Pendiente && d.CongregantId == null)
            await LoadCongregants();
        return d == null ? NotFound() : View(d);
    }
    [Authorize] public async Task<IActionResult> Receipt(int id)
    {
        var d = await VisibleDonations().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (d?.ReceiptStorageKey == null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return File(storage.Open(d.ReceiptStorageKey), d.ReceiptContentType!);
    }
    [Authorize(Roles = Roles.Administrator), HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ReplaceItemPhoto(int id, IFormFile? photo, string? version)
    {
        try
        {
            if (photo == null) throw new InvalidOperationException("Selecciona una foto JPG o PNG.");
            if (string.IsNullOrEmpty(version)) throw new InvalidOperationException("Actualiza la página e inténtalo de nuevo.");
            await service.ReplaceItemPhotoAsync(id, photo, Convert.FromBase64String(version), UserId);
            TempData["Success"] = "Foto del bien actualizada.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (FormatException) { TempData["Error"] = "Actualiza la página e inténtalo de nuevo."; }
        return RedirectToAction(nameof(Details), new { id });
    }
    [Authorize(Roles = Roles.Administrator), HttpPost]
    public async Task<IActionResult> Decide(int id, DonationStatus status, string? reason, string? version, bool confirmReceived, int? congregantId)
    {
        try
        {
            if (string.IsNullOrEmpty(version)) throw new InvalidOperationException("Actualiza la página e inténtalo de nuevo.");
            if (status == DonationStatus.Aprobada && !confirmReceived) throw new InvalidOperationException("Confirma la recepción efectiva antes de aprobar.");
            await service.DecideAsync(id, status, reason, UserId, Convert.FromBase64String(version ?? ""), congregantId);
            TempData["Success"] = "Decisión registrada.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (FormatException) { TempData["Error"] = "Actualiza la página e inténtalo de nuevo."; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
