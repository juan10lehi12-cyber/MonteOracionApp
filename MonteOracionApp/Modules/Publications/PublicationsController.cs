using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Publications;
[Authorize(Roles=Roles.Administrator)]
public class PublicationsController(ApplicationDbContext db,IFileStorage storage):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(int page=1)
    {
        ViewBag.Page=Math.Clamp(page,1,100000);ViewBag.Total=await db.Publications.CountAsync();
        return View(await db.Publications.AsNoTracking().OrderByDescending(x=>x.Id).Skip((Math.Clamp(page,1,100000)-1)*30).Take(30).ToListAsync());
    }
    [HttpGet]public async Task<IActionResult>Edit(int? id)=>id==null?View(new Publication()):await db.Publications.FindAsync(id) is {} p?View(p):NotFound();
    [HttpPost,RequestSizeLimit(6*1024*1024)]public async Task<IActionResult>Edit(int id,[Bind("Kind,Title,Body,EventAt,EventEndsAt,Location,IsPublished")]Publication form,IFormFile? image)
    {
        if(!Enum.IsDefined(form.Kind))ModelState.AddModelError("Kind","Tipo inválido.");
        if(form.Kind==PublicationKind.Evento&&(form.EventAt==null||string.IsNullOrWhiteSpace(form.Location)||form.EventEndsAt==null||form.EventEndsAt<=form.EventAt))ModelState.AddModelError("","Los eventos requieren inicio, fin posterior y lugar.");
        if(!ModelState.IsValid){form.Id=id;return View(form);}
        var p=id==0?new Publication():await db.Publications.FindAsync(id);if(p==null)return NotFound();
        if(p.Kind==PublicationKind.Evento&&form.Kind!=PublicationKind.Evento&&await db.EventParticipants.AnyAsync(x=>x.PublicationId==id)){ModelState.AddModelError("","Un evento con participantes conserva su tipo. Crea un aviso separado.");form.Id=id;return View(form);}
        if(p.IsCancelled){TempData["Error"]="El evento cancelado conserva su información.";return RedirectToAction(nameof(Index));}
        StoredFile? uploaded=null;var old=p.ImageKey;
        try{if(image!=null)uploaded=await storage.SaveAsync(image,true);}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);form.Id=id;return View(form);}
        if(form.Kind==PublicationKind.Evento&&uploaded==null&&p.ImageKey==null){ModelState.AddModelError("","Adjunta la imagen del evento.");form.Id=id;return View(form);}
        p.Kind=form.Kind;p.Title=form.Title.Trim();p.Body=form.Body.Trim();p.IsPublished=form.IsPublished;
        p.EventAt=form.Kind==PublicationKind.Evento?form.EventAt:null;p.EventEndsAt=form.Kind==PublicationKind.Evento?form.EventEndsAt:null;p.Location=form.Kind==PublicationKind.Evento?form.Location?.Trim():null;
        if(uploaded!=null){p.ImageKey=uploaded.Key;p.ImageType=uploaded.ContentType;}
        if(id==0)db.Publications.Add(p);
        db.Audit(Actor,"Publicación guardada",$"Publicación #{id}",p.Title);
        try{await db.SaveOperationAsync();}catch{if(uploaded!=null)storage.Delete(uploaded.Key);throw;}
        if(uploaded!=null&&old!=null)storage.Delete(old);
        TempData["Success"]="Publicación guardada.";return RedirectToAction(nameof(Index));
    }
    [HttpPost]public async Task<IActionResult>Cancel(int id)
    {
        var p=await db.Publications.FindAsync(id);if(p==null||p.Kind!=PublicationKind.Evento)return NotFound();
        p.IsCancelled=true;db.Audit(Actor,"Evento cancelado",$"Evento #{id}",p.Title);await db.SaveOperationAsync();return RedirectToAction(nameof(Index));
    }
    [AllowAnonymous]public async Task<IActionResult>Image(int id)
    {
        var p=await db.Publications.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&(x.IsPublished||User.IsInRole(Roles.Administrator)));
        if(p?.ImageKey==null)return NotFound();return File(storage.Open(p.ImageKey),p.ImageType!);
    }
    public async Task<IActionResult>Participants(int id)
    {
        var p=await db.Publications.FindAsync(id);if(p==null||p.Kind!=PublicationKind.Evento)return NotFound();
        ViewBag.Event=p;ViewBag.Congregants=await db.Congregants.Where(x=>x.IsActive).OrderBy(x=>x.LastName).ToListAsync();
        return View(await db.EventParticipants.Include(x=>x.Congregant).Where(x=>x.PublicationId==id).OrderBy(x=>x.Congregant.LastName).ToListAsync());
    }
    [HttpPost]public async Task<IActionResult>Assign(int id,int congregantId)
    {
        var p=await db.Publications.FindAsync(id);
        if(p==null||p.Kind!=PublicationKind.Evento||p.IsCancelled||p.EventAt<ChurchTime.Now){TempData["Error"]="Selecciona un evento próximo activo.";return RedirectToAction(nameof(Participants),new{id});}
        if(!await db.Congregants.AnyAsync(x=>x.Id==congregantId&&x.IsActive)){TempData["Error"]="Congregante inválido.";return RedirectToAction(nameof(Participants),new{id});}
        var existing=await db.EventParticipants.SingleOrDefaultAsync(x=>x.PublicationId==id&&x.CongregantId==congregantId);
        if(existing is { IsActive:false })
        {
            existing.IsActive=true;existing.RemovalReason=null;existing.RecordedBy=Actor;
            db.Audit(Actor,"Participación restablecida",$"Evento #{id}",$"Congregante #{congregantId}");await db.SaveOperationAsync();
        }
        if(existing==null)
        {
            db.EventParticipants.Add(new EventParticipant{PublicationId=id,CongregantId=congregantId,RecordedBy=Actor});
            db.Audit(Actor,"Participante asignado",$"Evento #{id}",$"Congregante #{congregantId}");
            try{await db.SaveOperationAsync();}catch(DbUpdateException){TempData["Error"]="El participante ya fue asignado.";}
        }
        return RedirectToAction(nameof(Participants),new{id});
    }
    [HttpPost]public async Task<IActionResult>Attendance(int id,AttendanceStatus status,string? note,string? version)
    {
        var p=await db.EventParticipants.Include(x=>x.Publication).SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();
        if(!p.IsActive||!Enum.IsDefined(status)||note?.Length>1000||p.Publication.IsCancelled||p.Publication.EventAt>ChurchTime.Now){TempData["Error"]="Revisa el estado; la asistencia se registra al comenzar el evento.";return RedirectToAction(nameof(Participants),new{id=p.PublicationId});}
        if(status==AttendanceStatus.Justificada&&string.IsNullOrWhiteSpace(note)){TempData["Error"]="Indica la justificación.";return RedirectToAction(nameof(Participants),new{id=p.PublicationId});}
        try {
            var original=Convert.FromBase64String(version??"");
            if(!original.SequenceEqual(p.Version)){TempData["Error"]="Otra persona modificó la asistencia. Actualiza la página.";return RedirectToAction(nameof(Participants),new{id=p.PublicationId});}
            db.Entry(p).Property(x=>x.Version).OriginalValue=original;
        }
        catch(FormatException){TempData["Error"]="Actualiza la página.";return RedirectToAction(nameof(Participants),new{id=p.PublicationId});}
        var previous=p.Attendance;var oldNote=p.InternalNote??"";
        p.Attendance=status;p.InternalNote=note;p.RecordedBy=Actor;
        db.Audit(Actor,"Asistencia actualizada",$"Participante #{id}",$"{previous} -> {status}. Anterior: {oldNote[..Math.Min(oldNote.Length,750)]}. Nueva: {(note??"")[..Math.Min(note?.Length??0,750)]}");
        try{await db.SaveOperationAsync();}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Participants),new{id=p.PublicationId});
    }
    [HttpPost]public async Task<IActionResult>RemoveParticipant(int id,string reason)
    {
        var p=await db.EventParticipants.Include(x=>x.Publication).SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();
        try{
            OperationRules.Reason(reason);
            if(!p.IsActive||p.Publication.EventAt<=ChurchTime.Now||p.Attendance!=AttendanceStatus.Pendiente)throw new InvalidOperationException("Solo se retira una asignación activa antes del evento.");
            p.IsActive=false;p.RemovalReason=reason;p.RecordedBy=Actor;db.Audit(Actor,"Participante retirado",$"Participante #{id}",reason);await db.SaveOperationAsync();
        }catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Participants),new{id=p.PublicationId});
    }
    public async Task<IActionResult>History(string? search)
    {
        var q=db.EventParticipants.Include(x=>x.Congregant).Include(x=>x.Publication).AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.Congregant.Dni==search||x.Congregant.FirstName.Contains(search)||x.Congregant.LastName.Contains(search));
        ViewBag.Search=search;return View(await q.OrderByDescending(x=>x.Publication.EventAt).ToListAsync());
    }
}
