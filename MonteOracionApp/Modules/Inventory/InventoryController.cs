using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Inventory;
[Authorize(Roles = Roles.Staff)]
public class InventoryController(ApplicationDbContext db, InventoryService service, IFileStorage storage) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(string? search, bool availableOnly = false)
    {
        var q = db.StockLots.Include(x => x.Product).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Product.Name.Contains(search) || x.Location.Contains(search));
        if (availableOnly) q = q.Where(x => x.OnHand > x.Reserved && (x.ExpiresOn == null || x.ExpiresOn >= ChurchTime.Today));
        ViewBag.Search = search; ViewBag.AvailableOnly = availableOnly;
        return View(await q.OrderBy(x => x.ExpiresOn ?? DateTime.MaxValue).ThenBy(x => x.Id).ToListAsync());
    }
    public async Task<IActionResult> Products() => View(await db.Products.OrderBy(x => x.Name).ToListAsync());
    [Authorize(Roles = Roles.Stock), HttpGet] public async Task<IActionResult> ProductEdit(int id) => await db.Products.FindAsync(id) is { } p ? View(p) : NotFound();
    [Authorize(Roles = Roles.Stock), HttpPost] public async Task<IActionResult> ProductEdit(int id, [Bind("Name,Unit,Category,RequiresExpiry,IsActive")] Product form, string reason)
    {
        if (!ModelState.IsValid) { form.Id=id; return View(form); }
        var p=await db.Products.FindAsync(id); if(p==null)return NotFound();
        try {
            OperationRules.Reason(reason);
            if (p.IsActive!=form.IsActive && !User.IsInRole(Roles.Administrator)) return Forbid();
            if (await db.StockLots.AnyAsync(x=>x.ProductId==id) && (p.Name!=form.Name.Trim() || p.Unit!=form.Unit.Trim() || p.RequiresExpiry!=form.RequiresExpiry)) throw new InvalidOperationException("El producto ya tiene lotes: conserva nombre, unidad y regla de vencimiento; crea otra presentación si corresponde.");
            if (!form.IsActive && await db.StockLots.AnyAsync(x=>x.ProductId==id&&x.Reserved>0)) throw new InvalidOperationException("Primero libera las reservas del producto.");
            db.Audit(Actor,"Producto actualizado",$"Producto #{id}",$"{p.Name} / {p.Unit} -> {form.Name} / {form.Unit}; {reason}");
            p.Name=form.Name.Trim();p.Unit=form.Unit.Trim();p.Category=form.Category.Trim();p.RequiresExpiry=form.RequiresExpiry;p.IsActive=form.IsActive;
            await db.SaveOperationAsync();return RedirectToAction(nameof(Products));
        } catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message);form.Id=id;return View(form); }
    }
    [Authorize(Roles = Roles.Administrator), HttpGet] public async Task<IActionResult> BeneficiaryEdit(int id) => await db.Beneficiaries.FindAsync(id) is { } b ? View(b) : NotFound();
    [Authorize(Roles = Roles.Administrator), HttpPost] public async Task<IActionResult> BeneficiaryEdit(int id,[Bind("Name,Address,Contact,Phone,IsActive")] Beneficiary form,string reason)
    {
        if(!ModelState.IsValid){form.Id=id;return View(form);}
        var b=await db.Beneficiaries.FindAsync(id);if(b==null)return NotFound();
        try { OperationRules.Reason(reason);db.Audit(Actor,"Beneficiario actualizado",$"Beneficiario #{id}",$"{b.Name} / {b.Address} -> {form.Name} / {form.Address}; {reason}");
            b.Name=form.Name.Trim();b.Address=form.Address.Trim();b.Contact=form.Contact.Trim();b.Phone=form.Phone?.Trim();b.IsActive=form.IsActive;
            await db.SaveOperationAsync();return RedirectToAction(nameof(Beneficiaries));
        }catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);form.Id=id;return View(form);}
    }
    [HttpPost, Authorize(Roles = Roles.Stock)] public async Task<IActionResult> Product([Bind("Name,Unit,Category,RequiresExpiry")] Product form)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Completa nombre, unidad y categoría (revisa sus longitudes)."; return RedirectToAction(nameof(Products)); }
        form.Name = form.Name.Trim(); form.Unit = form.Unit.Trim();
        if (await db.Products.AnyAsync(x => x.Name == form.Name && x.Unit == form.Unit)) TempData["Error"] = "El producto y unidad ya existen.";
        else { db.Products.Add(form); db.Audit(Actor,"Producto creado",form.Name); await db.SaveOperationAsync(); TempData["Success"] = "Producto creado."; }
        return RedirectToAction(nameof(Products));
    }
    public async Task<IActionResult> Beneficiaries() => View(await db.Beneficiaries.OrderBy(x => x.Name).ToListAsync());
    [HttpPost, Authorize(Roles = Roles.Administrator)] public async Task<IActionResult> Beneficiary([Bind("Name,Address,Contact,Phone")] Beneficiary form)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Completa correctamente beneficiario, dirección y contacto."; return RedirectToAction(nameof(Beneficiaries)); }
        db.Beneficiaries.Add(form); db.Audit(Actor,"Beneficiario creado",form.Name); await db.SaveOperationAsync(); return RedirectToAction(nameof(Beneficiaries));
    }
    public async Task<IActionResult> Requests(StockRequestStatus? status)
    {
        var q = db.StockRequests.Include(x => x.Beneficiary).Include(x => x.Lines).AsNoTracking();
        if (!User.IsInRole(Roles.Administrator) && !User.IsInRole(Roles.Warehouse)) q = q.Where(x => x.RequestedBy == Actor);
        if (status.HasValue) q = q.Where(x => x.Status == status);
        ViewBag.Status = status;
        return View(await q.OrderBy(x => x.Status == StockRequestStatus.Pendiente ? 0 : 1).ThenByDescending(x => x.Priority).ThenBy(x => x.NeededOn).ToListAsync());
    }
    private async Task Choices()
    {
        ViewBag.Beneficiaries = await db.Beneficiaries.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
        ViewBag.Lots = await db.StockLots.Include(x => x.Product).Where(x => x.Product.IsActive && x.OnHand > x.Reserved && (x.ExpiresOn == null || x.ExpiresOn >= ChurchTime.Today)).OrderBy(x => x.ExpiresOn ?? DateTime.MaxValue).ToListAsync();
    }
    [HttpGet] public async Task<IActionResult> Create() { await Choices(); return View(new StockRequestForm()); }
    [HttpPost] public async Task<IActionResult> Create(StockRequestForm form)
    {
        if (ModelState.IsValid) try { return RedirectToAction(nameof(Details), new { id = await service.RequestAsync(form, Actor) }); } catch (InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        await Choices(); return View(form);
    }
    public async Task<IActionResult> Details(int id)
    {
        var r = await db.StockRequests.Include(x => x.Beneficiary).Include(x => x.Lines).ThenInclude(x => x.StockLot).ThenInclude(x => x.Product).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (!User.IsInRole(Roles.Administrator) && !User.IsInRole(Roles.Warehouse) && r.RequestedBy != Actor) return Forbid();
        ViewBag.Movements = await db.StockMovements.Where(x => x.StockRequestLine != null && x.StockRequestLine.StockRequestId == id).OrderByDescending(x => x.Id).ToListAsync();
        ViewBag.Names = await db.StaffProfiles.ToDictionaryAsync(x => x.UserId,x => x.FullName);
        return View(r);
    }
    public async Task<IActionResult> Lot(int id)
    {
        var lot = await db.StockLots.Include(x => x.Product).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (lot == null) return NotFound();
        ViewBag.Movements = await db.StockMovements.Where(x => x.StockLotId == id).OrderByDescending(x => x.Id).ToListAsync();
        return View(lot);
    }
    [HttpPost, Authorize(Roles = Roles.Administrator)] public async Task<IActionResult> Decide(int id,bool approve,string reason = "") => await Act(id,()=>service.DecideAsync(id,approve,reason,Actor));
    [HttpPost, Authorize(Roles = Roles.Stock), RequestSizeLimit(6*1024*1024)] public async Task<IActionResult> Deliver(int id,int lineId,decimal quantity,string receiver,IFormFile? evidence,Guid commandId) => await Act(id,()=>service.DeliverAsync(lineId,quantity,receiver,evidence,Actor,commandId));
    [HttpPost] public async Task<IActionResult> Cancel(int id,string reason) => await Act(id,()=>service.CancelAsync(id,reason,Actor,User.IsInRole(Roles.Administrator)));
    [HttpPost, Authorize(Roles = Roles.Stock)] public async Task<IActionResult> Return(int id,int lineId,decimal quantity,string reason,string location,DateTime? expiry,Guid commandId) => await Act(id,()=>service.ReturnAsync(lineId,quantity,reason,location,expiry,Actor,commandId));
    [HttpPost, Authorize(Roles = Roles.Administrator)] public async Task<IActionResult> WriteOff(int id,int lineId,decimal quantity,string reason,Guid commandId) => await Act(id,()=>service.WriteOffAsync(lineId,quantity,reason,Actor,commandId));
    [HttpPost, Authorize(Roles = Roles.Administrator)] public async Task<IActionResult> Adjust(int id,decimal change,string reason,bool loss,Guid commandId)
    {
        try { if (!ModelState.IsValid) throw new InvalidOperationException("Revisa el importe y el motivo."); await service.AdjustAsync(id,change,reason,Actor,loss,commandId); TempData["Success"]="Movimiento registrado."; }
        catch(InvalidOperationException ex) { TempData["Error"]=ex.Message; }
        return RedirectToAction(nameof(Lot),new {id});
    }
    [Authorize(Roles = Roles.Stock)] public async Task<IActionResult> Legacy()
    {
        ViewBag.Products=await db.Products.Where(x=>x.IsActive).ToListAsync();
        return View(await db.Set<Donations.DonationItem>().Where(x => db.Donations.Any(d=>d.Id==x.DonationId && d.Status==Donations.DonationStatus.Aprobada) && !db.StockLots.Any(l=>l.DonationItemId==x.Id)).ToListAsync());
    }
    [HttpPost, Authorize(Roles = Roles.Stock)] public async Task<IActionResult> ImportLegacy(int itemId,int productId,string location,DateTime? expiry)
    {
        try { if(!ModelState.IsValid) throw new InvalidOperationException("Datos inválidos."); await service.ImportLegacyAsync(itemId,productId,location,expiry,Actor); TempData["Success"]="Ingreso histórico registrado una sola vez."; }
        catch(InvalidOperationException ex) { TempData["Error"]=ex.Message; }
        return RedirectToAction(nameof(Legacy));
    }
    [Authorize(Roles = Roles.Stock)] public async Task<IActionResult> Evidence(long id)
    {
        var m=await db.StockMovements.SingleOrDefaultAsync(x=>x.Id==id);
        if(m?.EvidenceKey==null) return NotFound();
        Response.Headers.CacheControl="no-store"; return File(storage.Open(m.EvidenceKey),m.EvidenceType!,"constancia-"+id+Path.GetExtension(m.EvidenceKey));
    }
    private async Task<IActionResult> Act(int id,Func<Task> action)
    {
        try { if(!ModelState.IsValid) throw new InvalidOperationException("Revisa los datos del formulario."); await action(); TempData["Success"]="Operación registrada."; }
        catch(InvalidOperationException ex) { TempData["Error"]=ex.Message; }
        return RedirectToAction(nameof(Details),new{id});
    }
}
