using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
using MonteOracionApp.Modules.Inventory;
using MonteOracionApp.Modules.Treasury;
using System.Data;
namespace MonteOracionApp.Modules.Donations;
public interface IDonationService
{
    Task<Donation> RegisterOnlineAsync(OnlineDonationForm form, string? userId);
    Task<Donation> RegisterReceivedAsync(ReceivedDonationForm form, string administratorId);
    Task ReplaceItemPhotoAsync(int id, IFormFile photo, byte[] version, string administratorId);
    Task DecideAsync(int id, DonationStatus status, string? reason, string administratorId, byte[] version, int? congregantId = null);
}
public class DonationService(ApplicationDbContext db, IFileStorage storage, InventoryService inventory, TreasuryService treasury) : IDonationService
{
    public async Task<Donation> RegisterOnlineAsync(OnlineDonationForm form, string? userId)
    {
        DonationRules.ValidateDate(form.PaidOn, DateTime.UtcNow.AddHours(-5));
        DonationRules.ValidateAmount(form.Amount);
        var payment = await db.PaymentMethods.SingleOrDefaultAsync(x => x.Id == form.PaymentMethodId && x.IsActive)
            ?? throw new InvalidOperationException("Selecciona un medio de pago disponible.");
        var congregant = form.Anonymous || string.IsNullOrWhiteSpace(form.Dni) ? null : await db.Congregants.SingleOrDefaultAsync(x => x.Dni == form.Dni && x.IsActive);
        if (!form.Anonymous && !string.IsNullOrWhiteSpace(form.Dni) && congregant == null) throw new InvalidOperationException("Consulta con la iglesia para registrar o actualizar tu DNI.");
        if (!await db.Funds.AnyAsync(x => x.Id == form.FundId && x.IsActive)) throw new InvalidOperationException("Selecciona un fondo activo.");
        var operation = form.OperationNumber.Trim().ToUpperInvariant();
        if (operation.Length is 0 or > 80) throw new InvalidOperationException("Indica número de operación.");
        if (await db.Donations.AnyAsync(x => x.PaymentMethodId == payment.Id && x.ReceivedOrPaidOn == form.PaidOn.Date && x.OperationNumber == operation)) throw new InvalidOperationException("Esta operación ya fue registrada para ese medio y fecha. Consulta con tesorería.");
        if (congregant == null && !form.Anonymous && string.IsNullOrWhiteSpace(form.Name)) throw new InvalidOperationException("Indica tu nombre o selecciona la donación anónima.");
        if (form.Receipt == null) throw new InvalidOperationException("El comprobante es obligatorio.");
        var file = await storage.SaveAsync(form.Receipt, imageOnly: true);
        var donation = new Donation {
            Kind = DonationKind.Dinero, Status = DonationStatus.Pendiente,
            DonorKind = congregant != null ? DonorKind.Congregante : form.Anonymous ? DonorKind.Anonimo : DonorKind.Externo,
            CongregantId = congregant?.Id, ExternalName = congregant == null && !form.Anonymous ? form.Name?.Trim() : null,
            ExternalContact = congregant == null && !form.Anonymous ? form.Contact?.Trim() : null,
            Amount = form.Amount, PaymentMethodId = payment.Id, PaymentMethodName = payment.Name, OperationNumber = operation, FundId = form.FundId,
            ReceivedOrPaidOn = form.PaidOn.Date, ReceiptStorageKey = file.Key, ReceiptContentType = file.ContentType, RecordedByUserId = null
        };
        db.Donations.Add(donation);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { storage.Delete(file.Key); throw new InvalidOperationException("La operación ya fue registrada. Consulta con tesorería."); }
        catch { storage.Delete(file.Key); throw; }
        return donation;
    }
    public async Task<Donation> RegisterReceivedAsync(ReceivedDonationForm form, string administratorId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        DonationRules.ValidateDate(form.ReceivedOn, DateTime.UtcNow.AddHours(-5));
        if (!Enum.IsDefined(form.Kind) || !Enum.IsDefined(form.DonorKind)) throw new InvalidOperationException("Tipo de donación o donante inválido.");
        if (!form.ConfirmReceived) throw new InvalidOperationException("Debes confirmar la recepción antes de aprobar.");
        if (form.DonorKind == DonorKind.Congregante && !await db.Congregants.AnyAsync(x => x.Id == form.CongregantId && x.IsActive)) throw new InvalidOperationException("Selecciona un congregante activo.");
        if (form.DonorKind == DonorKind.Externo && string.IsNullOrWhiteSpace(form.ExternalName)) throw new InvalidOperationException("Indica el nombre del donante externo.");
        var d = new Donation { Kind = form.Kind, Status = DonationStatus.Aprobada, DonorKind = form.DonorKind,
            CongregantId = form.DonorKind == DonorKind.Congregante ? form.CongregantId : null,
            ExternalName = form.DonorKind == DonorKind.Externo ? form.ExternalName?.Trim() : null,
            ExternalContact = form.DonorKind == DonorKind.Externo ? form.ExternalContact?.Trim() : null,
            ReceivedOrPaidOn = form.ReceivedOn.Date, RecordedByUserId = administratorId, FundId = form.FundId };
        if (form.Kind == DonationKind.Dinero)
        {
            DonationRules.ValidateAmount(form.Amount);
            if (!await db.Funds.AnyAsync(x => x.Id == form.FundId && x.IsActive)) throw new InvalidOperationException("Selecciona un fondo activo.");
            d.Amount = form.Amount; d.PaymentMethodName = "Efectivo";
        }
        else
        {
            var items = form.Items.Where(x => x.ProductId.HasValue || x.Quantity.HasValue).ToList();
            if (items.Count is 0 or > 50 || items.Any(x => x.ProductId == null || x.Quantity is null or <= 0 or > 9999999999.99m || decimal.Round(x.Quantity.Value, 2) != x.Quantity)) throw new InvalidOperationException("Registra de 1 a 50 bienes del catálogo con cantidad positiva y máximo dos decimales.");
            if (form.ItemPhoto == null) throw new InvalidOperationException("Adjunta una foto del bien recibido.");
            d.Items = items.Select(x => new DonationItem { ProductId = x.ProductId, Quantity = x.Quantity!.Value, ExpiresOn = x.ExpiresOn, StorageLocation = x.StorageLocation }).ToList();
            await inventory.ReceiveDonationAsync(d, administratorId);
        }
        if (d.Kind == DonationKind.Dinero) await treasury.DonationAsync(d, administratorId);
        StoredFile? photo = null;
        if (form.Kind == DonationKind.Bienes)
        {
            photo = await storage.SaveAsync(form.ItemPhoto!, imageOnly: true);
            d.ReceiptStorageKey = photo.Key;
            d.ReceiptContentType = photo.ContentType;
        }
        d.Decisions.Add(new() { AdministratorId = administratorId, Status = DonationStatus.Aprobada, Reason = "Recepción presencial confirmada." });
        db.Donations.Add(d);
        db.Audit(administratorId, "Donación presencial recibida", "Recepción", d.Kind.ToString());
        try { await db.SaveOperationAsync(); await transaction.CommitAsync(); }
        catch { if (photo != null) storage.Delete(photo.Key); throw; }
        return d;
    }
    public async Task DecideAsync(int id, DonationStatus status, string? reason, string administratorId, byte[] version, int? congregantId = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var d = await db.Donations.SingleOrDefaultAsync(x => x.Id == id) ?? throw new InvalidOperationException("Donación no encontrada.");
        DonationRules.ValidateDecision(d.Status, status, reason);
        if (d.Kind == DonationKind.Dinero && (status == DonationStatus.Aprobada || d.Status == DonationStatus.Aprobada && status == DonationStatus.Anulada))
            await treasury.DonationAsync(d, administratorId, status == DonationStatus.Anulada);
        if (d.Kind == DonationKind.Bienes && status == DonationStatus.Anulada)
        {
            var lots = await db.StockLots.Where(x => x.DonationItem != null && x.DonationItem.DonationId == id).ToListAsync();
            foreach (var lot in lots)
            {
                if (lot.Reserved != 0 || await db.StockMovements.AnyAsync(x => x.StockLotId == lot.Id && x.Kind != StockMovementKind.Donacion)) throw new InvalidOperationException("El bien tiene movimientos o compromisos. Registra una corrección de inventario.");
                db.StockMovements.Add(new StockMovement { StockLotId = lot.Id, Quantity = -lot.OnHand, Kind = StockMovementKind.AjusteNegativo, ActorId = administratorId, Reason = "Anulación de recepción: " + reason });
                lot.OnHand = 0;
            }
        }
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
        db.Audit(administratorId, "Decisión de donación", $"Donación #{id}", status + ": " + reason);
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
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
