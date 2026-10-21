using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Treasury;
[Authorize(Roles = Roles.Staff)]
public class TreasuryController(ApplicationDbContext db,TreasuryService service,IFileStorage storage) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [Authorize(Roles = Roles.Finance)] public async Task<IActionResult> Index()
    {
        var funds=await db.Funds.OrderBy(x=>x.Name).ToListAsync(); var balances=new List<FundBalance>();
        foreach(var f in funds) foreach(var channel in Enum.GetValues<MoneyChannel>())
        {
            var balance=await db.MoneyMovements.Where(x=>x.FundId==f.Id && x.Channel==channel).SumAsync(x=>(decimal?)x.Amount)??0;
            var available=await service.AvailableAsync(f.Id,channel);
            balances.Add(new FundBalance(f,channel,balance,balance-available));
        }
        ViewBag.Pending=await db.FinanceRequests.CountAsync(x=>x.Status==FinanceStatus.Pendiente);
        ViewBag.Overdue=await db.FinanceRequests.CountAsync(x=>x.Kind==FinanceKind.Anticipo && x.Status==FinanceStatus.Pagada && x.DueOn<ChurchTime.Today);
        return View(balances);
    }
    [HttpPost,Authorize(Roles=Roles.Administrator)] public async Task<IActionResult> Fund([Bind("Name,Purpose")] Fund form)
    {
        if(!ModelState.IsValid){TempData["Error"]="Completa nombre y finalidad del fondo.";return RedirectToAction(nameof(Index));}
        if(await db.Funds.AnyAsync(x=>x.Name==form.Name.Trim())){TempData["Error"]="Ya existe un fondo con ese nombre.";return RedirectToAction(nameof(Index));}
        form.Name=form.Name.Trim();db.Funds.Add(form);db.Audit(Actor,"Fondo creado",form.Name,form.Purpose);await db.SaveOperationAsync();return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Requests(FinanceStatus? status)
    {
        var q=db.FinanceRequests.Include(x=>x.Fund).AsNoTracking();
        if(!User.IsInRole(Roles.Administrator)&&!User.IsInRole(Roles.Treasurer))q=q.Where(x=>x.RequestedBy==Actor);
        if(status.HasValue)q=q.Where(x=>x.Status==status);
        ViewBag.Status=status; return View(await q.OrderByDescending(x=>x.Id).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Create(){ViewBag.Funds=await db.Funds.Where(x=>x.IsActive).ToListAsync();return View(new FinanceForm());}
    [HttpPost] public async Task<IActionResult> Create(FinanceForm form)
    {
        if(ModelState.IsValid)try{return RedirectToAction(nameof(Details),new{id=await service.RequestAsync(form,Actor,User.IsInRole(Roles.Administrator))});}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}
        ViewBag.Funds=await db.Funds.Where(x=>x.IsActive).ToListAsync();return View(form);
    }
    public async Task<IActionResult> Details(int id)
    {
        var r=await db.FinanceRequests.Include(x=>x.Fund).AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
        if(r==null)return NotFound();
        if(!User.IsInRole(Roles.Administrator)&&!User.IsInRole(Roles.Treasurer)&&r.RequestedBy!=Actor)return Forbid();
        ViewBag.Movements=await db.MoneyMovements.Where(x=>x.FinanceRequestId==id).OrderBy(x=>x.Id).ToListAsync();
        ViewBag.Names=await db.StaffProfiles.ToDictionaryAsync(x=>x.UserId,x=>x.FullName);return View(r);
    }
    [HttpPost,Authorize(Roles=Roles.Administrator)]public async Task<IActionResult> Decide(int id,bool approve,string reason="")=>await Act(id,()=>service.DecideAsync(id,approve,reason,Actor));
    [HttpPost,Authorize(Roles=Roles.Finance),RequestSizeLimit(6*1024*1024)]public async Task<IActionResult> Pay(int id,IFormFile? evidence)=>await Act(id,()=>service.PayAsync(id,evidence,Actor));
    [HttpPost,Authorize(Roles=Roles.Finance),RequestSizeLimit(6*1024*1024)]public async Task<IActionResult> Settle(int id,decimal spent,IFormFile? evidence)=>await Act(id,()=>service.SettleAsync(id,spent,evidence,Actor));
    [HttpPost]public async Task<IActionResult> Cancel(int id,string reason)=>await Act(id,()=>service.CancelAsync(id,reason,Actor,User.IsInRole(Roles.Administrator)));
    [Authorize(Roles=Roles.Finance)]public async Task<IActionResult> Closings()=>View(await db.CashClosings.OrderByDescending(x=>x.Day).ToListAsync());
    [HttpPost,Authorize(Roles=Roles.Finance),RequestSizeLimit(6*1024*1024)]public async Task<IActionResult> Close(DateTime day,decimal counted,MoneyChannel channel,IFormFile? evidence,string notes="")
    {
        try{if(!ModelState.IsValid)throw new InvalidOperationException("Revisa fecha y monto contado.");await service.CloseAsync(day,counted,notes,Actor,channel,evidence);TempData["Success"]="Cierre registrado. La fecha queda bloqueada para ese medio.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Closings));
    }
    [HttpPost,Authorize(Roles=Roles.Administrator)]public async Task<IActionResult> Review(int id)
    {
        try{await service.ReviewClosingAsync(id,Actor);TempData["Success"]="Cierre revisado.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Closings));
    }
    [Authorize(Roles=Roles.Finance)]public async Task<IActionResult> Evidence(int id,bool settlement=false)
    {
        var r=await db.FinanceRequests.SingleOrDefaultAsync(x=>x.Id==id);var key=settlement?r?.SettlementKey:r?.EvidenceKey;
        if(key==null)return NotFound();Response.Headers.CacheControl="no-store";return File(storage.Open(key),(settlement?r!.SettlementType:r!.EvidenceType)!,"comprobante-"+id+Path.GetExtension(key));
    }
    [Authorize(Roles=Roles.Finance)] public async Task<IActionResult> ClosingEvidence(int id)
    {
        var c=await db.CashClosings.SingleOrDefaultAsync(x=>x.Id==id);
        if(c?.EvidenceKey==null)return NotFound();Response.Headers.CacheControl="no-store";
        return File(storage.Open(c.EvidenceKey),c.EvidenceType!,"conciliacion-"+id+Path.GetExtension(c.EvidenceKey));
    }
    private async Task<IActionResult> Act(int id,Func<Task> action)
    {
        try{if(!ModelState.IsValid)throw new InvalidOperationException("Revisa los datos.");await action();TempData["Success"]="Operación registrada.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Details),new{id});
    }
}
