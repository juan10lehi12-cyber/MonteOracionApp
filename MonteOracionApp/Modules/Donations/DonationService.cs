using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Donations;
public interface IDonationService
{
    Task<Donation> RegisterOnlineAsync(OnlineDonationForm form, string? userId);
    Task<Donation> RegisterReceivedAsync(ReceivedDonationForm form, string administratorId);
    Task ReplaceItemPhotoAsync(int id, IFormFile photo, byte[] version, string administratorId);
    Task DecideAsync(int id, DonationStatus status, string? reason, string administratorId, byte[] version, int? congregantId = null);
}
public class DonationService(ApplicationDbContext db, IFileStorage storage) : IDonationService
{
    public async Task<Donation> RegisterOnlineAsync(OnlineDonationForm form, string? userId)
    {
        DonationRules.ValidateDate(form.PaidOn, DateTime.UtcNow.AddHours(-5));
        DonationRules.ValidateAmount(form.Amount);
        var payment = await db.PaymentMethods.SingleOrDefaultAsync(x => x.Id == form.PaymentMethodId && x.IsActive)
            ?? throw new InvalidOperationException("Selecciona un medio de pago disponible.");
        var congregant = userId == null ? null : await db.Congregants.SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (userId != null && congregant == null) throw new InvalidOperationException("Tu cuenta no está vinculada a un congregante activo. Usa el registro de recepción del administrador.");
        if (congregant == null && !form.Anonymous && string.IsNullOrWhiteSpace(form.Name)) throw new InvalidOperationException("Indica tu nombre o selecciona la donación anónima.");
        if (form.Receipt == null) throw new InvalidOperationException("El comprobante es obligatorio.");
        var file = await storage.SaveAsync(form.Receipt, imageOnly: true);
        var donation = new Donation {
            Kind = DonationKind.Dinero, Status = DonationStatus.Pendiente,
            DonorKind = congregant != null ? DonorKind.Congregante : form.Anonymous ? DonorKind.Anonimo : DonorKind.Externo,
            CongregantId = congregant?.Id, ExternalName = congregant == null && !form.Anonymous ? form.Name?.Trim() : null,
            ExternalContact = congregant == null && !form.Anonymous ? form.Contact?.Trim() : null,
            Amount = form.Amount, PaymentMethodId = payment.Id, PaymentMethodName = payment.Name,
            ReceivedOrPaidOn = form.PaidOn.Date, ReceiptStorageKey = file.Key, ReceiptContentType = file.ContentType, RecordedByUserId = userId
        };
        db.Donations.Add(donation);
        try { await db.SaveChangesAsync(); }
        catch { storage.Delete(file.Key); throw; }
        return donation;
    }
    public async Task<Donation> RegisterReceivedAsync(ReceivedDonationForm form, string administratorId)
    {
        DonationRules.ValidateDate(form.ReceivedOn, DateTime.UtcNow.AddHours(-5));
        if (!Enum.IsDefined(form.Kind) || !Enum.IsDefined(form.DonorKind)) throw new InvalidOperationException("Tipo de donación o donante inválido.");
        if (!form.ConfirmReceived) throw new InvalidOperationException("Debes confirmar la recepción antes de aprobar.");
        if (form.DonorKind == DonorKind.Congregante && !await db.Congregants.AnyAsync(x => x.Id == form.CongregantId && x.IsActive)) throw new InvalidOperationException("Selecciona un congregante activo.");
        if (form.DonorKind == DonorKind.Externo && string.IsNullOrWhiteSpace(form.ExternalName)) throw new InvalidOperationException("Indica el nombre del donante externo.");
        var d = new Donation { Kind = form.Kind, Status = DonationStatus.Aprobada, DonorKind = form.DonorKind,
            CongregantId = form.DonorKind == DonorKind.Congregante ? form.CongregantId : null,
            ExternalName = form.DonorKind == DonorKind.Externo ? form.ExternalName?.Trim() : null,
            ExternalContact = form.DonorKind == DonorKind.Externo ? form.ExternalContact?.Trim() : null,
            ReceivedOrPaidOn = form.ReceivedOn.Date, RecordedByUserId = administratorId };
        if (form.Kind == DonationKind.Dinero)
        {
            DonationRules.ValidateAmount(form.Amount);
            d.Amount = form.Amount; d.PaymentMethodName = "Efectivo";
        }
        else
        {
            var items = form.Items.Where(x => !string.IsNullOrWhiteSpace(x.Description) || x.Quantity.HasValue || !string.IsNullOrWhiteSpace(x.Unit)).ToList();
            if (items.Count is 0 or > 50 || items.Any(x => string.IsNullOrWhiteSpace(x.Description) || string.IsNullOrWhiteSpace(x.Unit) || x.Quantity is null or <= 0 or > 9999999999.99m || decimal.Round(x.Quantity.Value, 2) != x.Quantity)) throw new InvalidOperationException("Registra de 1 a 50 bienes con descripción, cantidad positiva (máximo dos decimales) y unidad.");
            if (form.ItemPhoto == null) throw new InvalidOperationException("Adjunta una foto del bien recibido.");
            d.Items = items.Select(x => new DonationItem { Description = x.Description!.Trim(), Quantity = x.Quantity!.Value, Unit = x.Unit!.Trim() }).ToList();
        }
        StoredFile? photo = null;
        if (form.Kind == DonationKind.Bienes)
        {
            photo = await storage.SaveAsync(form.ItemPhoto!, imageOnly: true);
            d.ReceiptStorageKey = photo.Key;
            d.ReceiptContentType = photo.ContentType;
        }
        d.Decisions.Add(new() { AdministratorId = administratorId, Status = DonationStatus.Aprobada, Reason = "Recepción presencial confirmada." });
        db.Donations.Add(d);
        try { await db.SaveChangesAsync(); }
        catch { if (photo != null) storage.Delete(photo.Key); throw; }
        return d;
    }
    public async Task DecideAsync(int id, DonationStatus status, string? reason, string administratorId, byte[] version, int? congregantId = null)
    {
        var d = await db.Donations.SingleOrDefaultAsync(x => x.Id == id) ?? throw new InvalidOperationException("Donación no encontrada.");
        DonationRules.ValidateDecision(d.Status, status, reason);
        if (congregantId.HasValue)
        {
            if (d.Status != DonationStatus.Pendiente || status is not (DonationStatus.Aprobada or DonationStatus.Rechazada) || d.CongregantId != null)
                throw new InvalidOperationException("Solo puedes vincular una donación pendiente sin congregante al aprobarla o rechazarla.");
            var congregant = await db.Congregants.SingleOrDefaultAsync(x => x.Id == congregantId && x.IsActive)
                ?? throw new InvalidOperationException("El congregante seleccionado no existe o está inactivo.");
            d.CongregantId = congregant.Id;
            d.DonorKind = DonorKind.Congregante;
            d.Decisions.Add(new() { Status = status, AdministratorId = administratorId,
                Reason = $"Vinculada a la ficha del congregante #{congregant.Id} durante la revisión." });
        }
        db.Entry(d).Property(x => x.Version).OriginalValue = version;
        d.Status = status;
        d.Decisions.Add(new() { Status = status, Reason = reason?.Trim(), AdministratorId = administratorId });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Otro administrador ya modificó esta donación. Actualiza la página."); }
    }
    public async Task ReplaceItemPhotoAsync(int id, IFormFile photo, byte[] version, string administratorId)
    {
        var d = await db.Donations.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Donación no encontrada.");
        if (d.Kind != DonationKind.Bienes || d.Status != DonationStatus.Aprobada)
            throw new InvalidOperationException("Solo puedes cambiar la foto de bienes con recepción aprobada.");
        var uploaded = await storage.SaveAsync(photo, imageOnly: true);
        var oldKey = d.ReceiptStorageKey;
        db.Entry(d).Property(x => x.Version).OriginalValue = version;
        d.ReceiptStorageKey = uploaded.Key;
        d.ReceiptContentType = uploaded.ContentType;
        d.Decisions.Add(new() { AdministratorId = administratorId, Status = d.Status, Reason = "Foto del bien recibido actualizada." });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            storage.Delete(uploaded.Key);
            throw new InvalidOperationException("Otro administrador ya modificó esta donación. Actualiza la página.");
        }
        catch { storage.Delete(uploaded.Key); throw; }
        if (oldKey != null) storage.Delete(oldKey);
    }
}
