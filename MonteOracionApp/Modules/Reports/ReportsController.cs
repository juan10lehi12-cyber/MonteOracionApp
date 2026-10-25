using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Modules.Inventory;
using MonteOracionApp.Modules.Treasury;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Reports;
[Authorize(Roles=Roles.Staff)]
public class ReportsController(ApplicationDbContext db):Controller
{
    public IActionResult Index()=>View();
    [HttpGet]public async Task<IActionResult>Download(string kind="donaciones",string dateMode="rango",DateTime? on=null,DateTime? from=null,DateTime? to=null,int? id=null)
    {
        var anchor=(on??ChurchTime.Today).Date;
        var start=(from??new DateTime(anchor.Year,anchor.Month,1)).Date;
        var end=(to??ChurchTime.Today).Date;
        if(dateMode=="dia"){start=anchor;end=anchor;}
        if(dateMode=="mes"){start=new DateTime(anchor.Year,anchor.Month,1);end=start.AddMonths(1).AddDays(-1);}
        if(dateMode=="anio"){start=new DateTime(anchor.Year,1,1);end=new DateTime(anchor.Year,12,31);}
        if(!ModelState.IsValid||end<start||end==DateTime.MaxValue.Date){TempData["Error"]="Revisa el período del reporte.";return RedirectToAction(nameof(Index));}
        var exclusive=end.AddDays(1);var rows=new List<string>();var title="Reporte";var names=await db.StaffProfiles.AsNoTracking().ToDictionaryAsync(x=>x.UserId,x=>x.FullName);
        string Person(string? uid)=>uid==null?"Sin responsable":names.GetValueOrDefault(uid,uid);
        var finance=User.IsInRole(Roles.Administrator)||User.IsInRole(Roles.Treasurer);
        var stock=User.IsInRole(Roles.Administrator)||User.IsInRole(Roles.Warehouse);
        switch(kind)
        {
            case "donaciones":
                title="Donaciones recibidas / declaradas";
                var q=db.Donations.Include(x=>x.Congregant).Include(x=>x.Items).AsNoTracking().Where(x=>x.ReceivedOrPaidOn>=start&&x.ReceivedOrPaidOn<exclusive);
                if(!User.IsInRole(Roles.Administrator))q=q.Where(x=>x.Kind==(User.IsInRole(Roles.Treasurer)?DonationKind.Dinero:DonationKind.Bienes) || (User.IsInRole(Roles.Treasurer)&&User.IsInRole(Roles.Warehouse)));
                var donations=await q.OrderBy(x=>x.ReceivedOrPaidOn).ToListAsync();
                foreach(var d in donations)
                {
                    rows.Add($"#{d.Id} | {d.ReceivedOrPaidOn:dd/MM/yyyy} | {d.Status} | {d.DonorLabel}");
                    rows.Add(d.Kind==DonationKind.Dinero?$"  S/ {d.Amount:N2} | {d.PaymentMethodName} | operación {d.OperationNumber??"-"}":string.Join("; ",d.Items.Select(x=>$"  {x.Description}: {x.Quantity:0.##} {x.Unit}")));
                }
                rows.Add("");rows.Add($"TOTAL DINERO APROBADO: S/ {donations.Where(x=>x.Kind==DonationKind.Dinero&&x.Status==DonationStatus.Aprobada).Sum(x=>x.Amount??0):N2}");
                foreach(var group in donations.Where(x=>x.Kind==DonationKind.Dinero&&x.Status==DonationStatus.Aprobada).GroupBy(x=>x.PaymentMethodName))rows.Add($"  {group.Key}: S/ {group.Sum(x=>x.Amount??0):N2}");
                rows.Add("Fecha del reporte: pago/recepción declarada. La confirmación contable se consulta en movimientos.");
                break;
            case "dinero":
                if(!finance)return Forbid();title="Movimientos reales de dinero";
                var money=await db.MoneyMovements.Include(x=>x.Fund).AsNoTracking().Where(x=>x.BusinessDate>=start&&x.BusinessDate<exclusive).OrderBy(x=>x.BusinessDate).ThenBy(x=>x.Id).ToListAsync();
                foreach(var m in money)rows.Add($"#{m.Id} | {m.BusinessDate:dd/MM/yyyy} | {m.Fund.Name} | {m.Channel} | {m.Kind} | S/ {m.Amount:N2}\n  {m.Detail} | {Person(m.ActorId)}");
                foreach(var channel in Enum.GetValues<MoneyChannel>())
                {
                    var opening=await db.MoneyMovements.Where(x=>x.Channel==channel&&x.BusinessDate<start).SumAsync(x=>(decimal?)x.Amount)??0;
                    rows.Add($"{channel}: saldo inicial S/ {opening:N2}; entradas S/ {money.Where(x=>x.Channel==channel&&x.Amount>0).Sum(x=>x.Amount):N2}; salidas S/ {-money.Where(x=>x.Channel==channel&&x.Amount<0).Sum(x=>x.Amount):N2}; saldo final S/ {opening+money.Where(x=>x.Channel==channel).Sum(x=>x.Amount):N2}");
                }
                rows.Add("Los anticipos figuran como salida y su sobrante como devolución. No se duplica el gasto.");
                break;
            case "entregas":
                if(!stock)return Forbid();title="Entregas y reposiciones de bienes";
                var movements=await db.StockMovements.Include(x=>x.StockLot).ThenInclude(x=>x.Product).Include(x=>x.StockRequestLine!).ThenInclude(x=>x.StockRequest).ThenInclude(x=>x.Beneficiary).AsNoTracking()
                    .Where(x=>x.BusinessDate>=start&&x.BusinessDate<exclusive&&(x.Kind==StockMovementKind.Entrega||x.Kind==StockMovementKind.Reposicion)).OrderBy(x=>x.Id).ToListAsync();
                foreach(var m in movements)rows.Add($"#{m.Id} | {m.BusinessDate:dd/MM/yyyy} | {m.Kind} | {m.StockLot.Product.Name} | {Math.Abs(m.Quantity):0.##} {m.StockLot.Product.Unit}\n  Destino: {m.StockRequestLine?.StockRequest.BeneficiaryName}; solicitud #{m.StockRequestLine?.StockRequestId}\n  {Person(m.ActorId)}; receptor: {m.Receiver??"-"}; constancia: {(m.EvidenceKey==null?"-":"registrada")}");
                break;
            case "inventario":
                if(!stock)return Forbid();title="Inventario actual y movimientos";
                rows.Add($"EXISTENCIAS ACTUALES A {ChurchTime.Now:dd/MM/yyyy HH:mm} (no históricas)");
                foreach(var lot in await db.StockLots.Include(x=>x.Product).AsNoTracking().Where(x=>!x.PendingReview).OrderBy(x=>x.Product.Name).ToListAsync())rows.Add($"Lote #{lot.Id} | {lot.Product.Name} | {lot.Location} | {lot.Product.Unit}\n  Físico {lot.OnHand:0.##}; reservado {lot.Reserved:0.##}; disponible {lot.OnHand-lot.Reserved:0.##}; vence {lot.ExpiresOn:dd/MM/yyyy}");
                rows.Add("");rows.Add("MOVIMIENTOS DEL PERÍODO");
                foreach(var m in await db.StockMovements.Include(x=>x.StockLot).ThenInclude(x=>x.Product).AsNoTracking().Where(x=>x.BusinessDate>=start&&x.BusinessDate<exclusive).OrderBy(x=>x.Id).ToListAsync())rows.Add($"#{m.Id} | {m.BusinessDate:dd/MM/yyyy} | {m.StockLot.Product.Name} | {m.Kind}\n  Cambio físico {m.Quantity:0.##}; reserva {m.ReservedChange:0.##}; {m.Reason}; {Person(m.ActorId)}");
                break;
            case "cierre":
                if(!finance)return Forbid();title="Constancia de cierre / conciliación";
                var c=await db.CashClosings.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(c==null)return NotFound();
                rows.Add($"{c.Channel} | Cierre #{c.Id} | fecha: {c.Day:dd/MM/yyyy}");rows.Add($"Saldo previo: S/ {c.Opening:N2}\nIngresos: S/ {c.Income:N2}\nSalidas: S/ {c.Outgoing:N2}\nEsperado: S/ {c.Expected:N2}\nContado: S/ {c.Counted:N2}\nDiferencia: S/ {c.Counted-c.Expected:N2}");
                rows.Add("Preparado por: "+Person(c.PreparedBy));rows.Add("Revisión: "+(c.ReviewedBy==null?"Pendiente":Person(c.ReviewedBy)));rows.Add(c.Notes);
                start=c.Day;end=c.Day;break;
            case "auditoria":
                if(!User.IsInRole(Roles.Administrator))return Forbid();title="Auditoría de operaciones";
                var utcStart=new DateTimeOffset(start,TimeSpan.FromHours(-5));var utcEnd=new DateTimeOffset(exclusive,TimeSpan.FromHours(-5));
                foreach(var a in await db.AuditEntries.AsNoTracking().Where(x=>x.At>=utcStart&&x.At<utcEnd).OrderBy(x=>x.Id).ToListAsync())rows.Add($"#{a.Id} | {a.At.ToOffset(TimeSpan.FromHours(-5)):dd/MM/yyyy HH:mm} | {Person(a.ActorId)}\n  {a.Action} | {a.Reference} | {a.Detail}");
                break;
            default:return BadRequest("Reporte desconocido.");
        }
        Response.Headers.CacheControl="no-store";
        return File(PdfReport.Create(title,$"Período {start:dd/MM/yyyy} - {end:dd/MM/yyyy} | Emite: {User.FindFirst("staff_name")?.Value??User.Identity?.Name}",rows),"application/pdf",$"monte-oracion-{kind}-{start:yyyyMMdd}-{end:yyyyMMdd}.pdf");
    }
    [Authorize(Roles=Roles.Administrator)]public async Task<IActionResult>Audit(int page=1)
    {
        ViewBag.Page=Math.Clamp(page,1,100000);ViewBag.Total=await db.AuditEntries.CountAsync();ViewBag.Names=await db.StaffProfiles.ToDictionaryAsync(x=>x.UserId,x=>x.FullName);
        return View(await db.AuditEntries.AsNoTracking().OrderByDescending(x=>x.Id).Skip((Math.Clamp(page,1,100000)-1)*30).Take(30).ToListAsync());
    }
}
