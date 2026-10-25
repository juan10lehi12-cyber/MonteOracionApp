using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
using MonteOracionApp.Modules.Inventory;
using MonteOracionApp.Modules.Treasury;
using MonteOracionApp.Modules.Congregants;
using MonteOracionApp.Modules.Publications;
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
        if (form.Amount > 99999.99m) throw new InvalidOperationException("Ingresa un monto positivo hasta S/ 99 999.99 con dos decimales.");
        var payment = await db.PaymentMethods.SingleOrDefaultAsync(x => x.Id == form.PaymentMethodId && x.IsActive)
            ?? throw new InvalidOperationException("Selecciona un medio de pago disponible.");
        Congregant? congregant = null;
        if (!form.Anonymous)
        {
            if (string.IsNullOrWhiteSpace(form.Dni)) throw new InvalidOperationException("Ingresa tu DNI o marca la casilla de donación anónima.");
            congregant = await db.Congregants.SingleOrDefaultAsync(x => x.Dni == form.Dni && x.IsActive)
                ?? throw new InvalidOperationException("No encontramos ese DNI. Acércate a la administración para registrar tu ficha o dona de forma anónima.");
        }
        // El donante elige propósito; el dinero siempre cae a un fondo activo (General) para la contabilidad.
        var fund = await db.Funds.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.IsActive)
            ?? throw new InvalidOperationException("La iglesia todavía no tiene fondos configurados. Acércate a la administración.");
        Publications.Publication? eventModel = null;
        var operation = form.OperationNumber.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(operation, "^[A-Z0-9-]{4,20}$")) throw new InvalidOperationException("El número de operación admite de 4 a 20 caracteres (letras, números o guiones).");
        if (await db.Donations.AnyAsync(x => x.PaymentMethodId == payment.Id && x.ReceivedOrPaidOn == form.PaidOn.Date && x.OperationNumber == operation)) throw new InvalidOperationException("Esta operación ya fue registrada para ese medio y fecha. Consulta con tesorería.");
        if (form.EventId.HasValue)
        {
            eventModel = await db.Publications.SingleOrDefaultAsync(x => x.Id == form.EventId)
                ?? throw new InvalidOperationException("El evento indicado no está disponible.");
            if (eventModel.Kind != Publications.PublicationKind.Evento || !eventModel.IsPublished || eventModel.IsCancelled
                || eventModel.EventAt is null || eventModel.EventAt.Value.Date < form.PaidOn.Date)
                throw new InvalidOperationException("El evento indicado no está aceptando donaciones.");
        }
        if (form.Receipt == null) throw new InvalidOperationException("El comprobante es obligatorio.");
        var file = await storage.SaveAsync(form.Receipt, imageOnly: true);
        var donation = new Donation {
            Kind = DonationKind.Dinero, Status = DonationStatus.Pendiente,
            DonorKind = congregant != null ? DonorKind.Congregante : DonorKind.Anonimo,
            CongregantId = congregant?.Id, ExternalName = null, ExternalContact = null,
            Amount = form.Amount, PaymentMethodId = payment.Id, PaymentMethodName = payment.Name, OperationNumber = operation, FundId = fund.Id,
            EventId = eventModel?.Id, ReceivedOrPaidOn = form.PaidOn.Date, ReceiptStorageKey = file.Key, ReceiptContentType = file.ContentType, RecordedByUserId = null, Digital = true
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
        var fund = await db.Funds.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.IsActive)
            ?? throw new InvalidOperationException("La iglesia todavía no tiene fondos configurados. Acércate a la administración.");
        int? congregantId = null;
        if (form.DonorKind == DonorKind.Congregante)
        {
            congregantId = form.CongregantId;
            if (congregantId == null && !string.IsNullOrWhiteSpace(form.NewMemberName))
            {
                Congregant? creado = form.NewMemberDni is { Length: 8 } dni
                    ? await db.Congregants.SingleOrDefaultAsync(x => x.Dni == dni) : null;
                if (creado != null && !creado.IsActive) throw new InvalidOperationException("El miembro con ese DNI está desactivado; reactiva su ficha en Miembros antes de registrar la donación.");
                if (creado == null)
                {
                    var full = form.NewMemberName.Trim();
                    var corte = full.IndexOf(' ');
                    var firstName = corte > 0 ? full[..corte] : full;
                    var lastName = corte > 0 ? full[(corte + 1)..] : "";
                    creado = new Congregant { FirstName = firstName, LastName = lastName, Dni = form.NewMemberDni, Phone = form.NewMemberPhone };
                    db.Congregants.Add(creado);
                    db.Audit(administratorId, "Miembro creado en recepción", creado.FullName);
                }
                await db.SaveChangesAsync();
                congregantId = creado.Id;
            }
            if (congregantId == null || !await db.Congregants.AnyAsync(x => x.Id == congregantId && x.IsActive))
                throw new InvalidOperationException("Busca y selecciona al miembro que dona, o registra sus datos básicos.");
        }
        if (form.DonorKind == DonorKind.Externo && string.IsNullOrWhiteSpace(form.ExternalName)) throw new InvalidOperationException("Indica el nombre del donante externo.");
        var d = new Donation { Kind = form.Kind, Status = DonationStatus.Aprobada, DonorKind = form.DonorKind,
            CongregantId = form.DonorKind == DonorKind.Congregante ? congregantId : null,
            ExternalName = form.DonorKind == DonorKind.Externo ? form.ExternalName?.Trim() : null,
            ExternalContact = form.DonorKind == DonorKind.Externo ? form.ExternalContact?.Trim() : null,
            ReceivedOrPaidOn = form.ReceivedOn.Date, RecordedByUserId = administratorId, FundId = fund.Id };
        if (form.Kind == DonationKind.Dinero)
        {
            DonationRules.ValidateAmount(form.Amount);
            d.Amount = form.Amount; d.PaymentMethodName = "Efectivo";
        }
        else
        {
            var items = form.Items.Where(x => x.ProductId.HasValue || x.Quantity.HasValue || !string.IsNullOrWhiteSpace(x.NewProductName)).ToList();
            if (items.Count is 0 or > 50 || items.Any(x => (x.ProductId == null && string.IsNullOrWhiteSpace(x.NewProductName)) || x.Quantity is null or <= 0 or > 9999999999.99m || decimal.Round(x.Quantity.Value, 2) != x.Quantity)) throw new InvalidOperationException("Registra de 1 a 50 bienes: elige del catálogo o escribe un producto nuevo, con cantidad positiva y máximo dos decimales.");
            foreach (var it in items)
            {
                if (it.ProductId != null) continue;
                var name = it.NewProductName!.Trim();
                var unit = string.IsNullOrWhiteSpace(it.NewProductUnit) ? "unidad" : it.NewProductUnit.Trim();
                var product = await db.Products.SingleOrDefaultAsync(p => p.Name == name && p.Unit == unit);
                if (product == null)
                {
                    product = new Product { Name = name, Unit = unit, Category = "General", RequiresExpiry = it.NewProductRequiresExpiry };
                    db.Products.Add(product);
                    db.Audit(administratorId, "Producto creado en recepción", $"{name} / {unit}");
                }
                await db.SaveChangesAsync();
                it.ProductId = product.Id;
            }
            // El mismo producto con vencimiento distinto forma lotes distintos; filas repetidas idénticas se suman.
            d.Items = items.GroupBy(x => (x.ProductId, x.ExpiresOn))
                .Select(g => new DonationItem { ProductId = g.Key.ProductId, Quantity = g.Sum(x => x.Quantity!.Value), ExpiresOn = g.Key.ExpiresOn })
                .ToList();
            await inventory.ReceiveDonationAsync(d, administratorId);
        }
        if (d.Kind == DonationKind.Dinero) await treasury.DonationAsync(d, administratorId);
        d.Decisions.Add(new() { AdministratorId = administratorId, Status = DonationStatus.Aprobada, Reason = "Recepción presencial confirmada." });
        db.Donations.Add(d);
        db.Audit(administratorId, "Donación presencial recibida", "Recepción", d.Kind.ToString());
        await db.SaveOperationAsync(); await transaction.CommitAsync();
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
                lot.PendingReview = false; lot.PendingActorId = null;
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
