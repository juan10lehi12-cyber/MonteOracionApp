using System.Data;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Inventory;

public class InventoryService(ApplicationDbContext db, IFileStorage storage)
{
    public async Task ReceiveDonationAsync(Donation donation, string actor)
    {
        foreach (var item in donation.Items)
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.Id == item.ProductId && x.IsActive)
                ?? throw new InvalidOperationException("Selecciona un producto del catálogo para cada bien.");
            ValidateExpiry(product, item.ExpiresOn);
            item.Description = product.Name; item.Unit = product.Unit;
            // La entrada queda pendiente: el almacenista distinto la confirma ("visto bueno") con su ubicación física.
            var lot = new StockLot { ProductId = product.Id, DonationItem = item, OnHand = item.Quantity,
                ExpiresOn = item.ExpiresOn?.Date, Location = "", Origin = "Donación recibida",
                PendingReview = true, PendingActorId = actor, PendingAt = DateTimeOffset.UtcNow };
            db.StockLots.Add(lot);
            AddMovement(lot, StockMovementKind.Donacion, item.Quantity, 0, actor, "Ingreso de donación recibida; pendiente de visto bueno");
        }
    }
    public async Task ConfirmEntryAsync(int lotId, string location, string actor, Guid commandId)
    {
        await CheckCommand(commandId);
        OperationRules.Reason(location);
        if (location.Length > 160) throw new InvalidOperationException("Ubicación demasiado larga.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lot = await db.StockLots.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == lotId) ?? throw new InvalidOperationException("Lote inexistente.");
        if (!lot.PendingReview) throw new InvalidOperationException("Este lote ya fue confirmado.");
        OperationRules.OtherApprover(lot.PendingActorId ?? "", actor);
        if (lot.Product.RequiresExpiry && !lot.ExpiresOn.HasValue) throw new InvalidOperationException("Indica o ajusta el vencimiento antes de confirmar.");
        lot.Location = location.Trim();
        lot.PendingReview = false;
        lot.PendingActorId = null; lot.PendingAt = null;
        lot.ConfirmedBy = actor; lot.ConfirmedAt = DateTimeOffset.UtcNow;
        AddMovement(lot, StockMovementKind.VistoBueno, 0, 0, actor, $"Visto bueno; ubicación: {location.Trim()}").CommandId = commandId;
        db.Audit(actor, "Visto bueno de entrada", $"Lote #{lot.Id}", $"Producto {lot.Product.Name}; cantidad {lot.OnHand} {lot.Product.Unit}; ubicación: {location.Trim()}");
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task<int> RequestAsync(StockRequestForm form, string actor)
    {
        if (!Enum.IsDefined(form.Kind) || !Enum.IsDefined(form.Priority)) throw new InvalidOperationException("Tipo o prioridad inválidos.");
        OperationRules.Reason(form.Purpose);
        if (form.NeededOn.Date < ChurchTime.Today || form.Kind == StockRequestKind.Prestamo && (!form.ReturnDueOn.HasValue || form.ReturnDueOn.Value.Date < form.NeededOn.Date))
            throw new InvalidOperationException("Revisa la fecha necesaria y la fecha límite del préstamo.");
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == form.BeneficiaryId && x.IsActive) ?? throw new InvalidOperationException("Selecciona un beneficiario activo.");
        var lines = form.Lines.Where(x => x.StockLotId != 0 || x.Quantity != 0).ToList();
        if (lines.Count is 0 or > 50 || lines.Select(x => x.StockLotId).Distinct().Count() != lines.Count) throw new InvalidOperationException("Registra entre 1 y 50 lotes sin repetirlos.");
        var request = new StockRequest { BeneficiaryId = form.BeneficiaryId, BeneficiaryName = beneficiary.Name, BeneficiaryAddress = beneficiary.Address, BeneficiaryContact = beneficiary.Contact, Purpose = form.Purpose.Trim(), Kind = form.Kind,
            Priority = form.Priority, NeededOn = form.NeededOn.Date, ReturnDueOn = form.Kind == StockRequestKind.Prestamo ? form.ReturnDueOn?.Date : null, RequestedBy = actor };
        foreach (var row in lines)
        {
            var lot = await db.StockLots.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == row.StockLotId)
                ?? throw new InvalidOperationException("Lote no encontrado.");
            Usable(lot); OperationRules.Available(lot.OnHand - lot.Reserved, row.Quantity);
            request.Lines.Add(new StockRequestLine { StockLotId = lot.Id, Quantity = row.Quantity });
        }
        db.StockRequests.Add(request);
        db.Audit(actor, "Solicitud de bienes", "Nueva", form.Purpose);
        await db.SaveOperationAsync(); return request.Id;
    }
    public async Task DecideAsync(int id, bool approve, string reason, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await Load(id); OperationRules.OtherApprover(request.RequestedBy, actor);
        if (request.Status != StockRequestStatus.Pendiente) throw new InvalidOperationException("La solicitud ya fue atendida.");
        if (!approve) OperationRules.Reason(reason);
        if (approve)
        {
            if (!await db.Beneficiaries.AnyAsync(x=>x.Id==request.BeneficiaryId&&x.IsActive)) throw new InvalidOperationException("El beneficiario está inactivo; revisa el destino de la solicitud.");
            foreach (var line in request.Lines)
            {
                var lot = line.StockLot; Usable(lot);
                OperationRules.Available(lot.OnHand - lot.Reserved, line.Quantity);
                lot.Reserved += line.Quantity;
                AddMovement(lot, StockMovementKind.Reserva, 0, line.Quantity, actor, $"Reserva solicitud #{id}", line);
            }
        }
        request.Status = approve ? StockRequestStatus.Aprobada : StockRequestStatus.Rechazada;
        request.ApprovedBy = actor; request.DecisionReason = reason;
        db.Audit(actor, approve ? "Reserva aprobada" : "Solicitud rechazada", $"Bienes #{id}", reason);
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task DeliverAsync(int lineId, decimal quantity, string receiver, IFormFile? evidence, string actor, Guid commandId)
    {
        await CheckCommand(commandId);
        OperationRules.Quantity(quantity); OperationRules.Reason(receiver);
        if (receiver.Length > 160 || evidence == null) throw new InvalidOperationException("Indica receptor (hasta 160 caracteres) y adjunta constancia JPG, PNG o PDF.");
        var file = await storage.SaveAsync(evidence);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var line = await db.StockRequestLines.Include(x => x.StockRequest).Include(x => x.StockLot).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == lineId)
                ?? throw new InvalidOperationException("Línea no encontrada.");
            var request = await Load(line.StockRequestId);
            if (request.Status is not (StockRequestStatus.Aprobada or StockRequestStatus.Parcial)) throw new InvalidOperationException("La solicitud no está autorizada para entregar.");
            Usable(line.StockLot); OperationRules.Available(line.Quantity - line.Delivered, quantity);
            if (quantity > line.StockLot.Reserved || quantity > line.StockLot.OnHand) throw new InvalidOperationException("La reserva no coincide con el inventario.");
            line.StockLot.OnHand -= quantity; line.StockLot.Reserved -= quantity; line.Delivered += quantity;
            request.Status = request.Lines.All(x => x.Delivered == x.Quantity) ? StockRequestStatus.Entregada : StockRequestStatus.Parcial;
            var movement = AddMovement(line.StockLot, StockMovementKind.Entrega, -quantity, -quantity, actor, $"Entrega solicitud #{request.Id}", line);
            movement.CommandId = commandId; movement.Receiver = receiver.Trim(); movement.EvidenceKey = file.Key; movement.EvidenceType = file.ContentType;
            db.Audit(actor, "Entrega de bienes", $"Bienes #{request.Id}", $"{quantity} {line.StockLot.Product.Unit}; receptor: {receiver}");
            await db.SaveOperationAsync(); await tx.CommitAsync();
        }
        catch { storage.Delete(file.Key); throw; }
    }
    public async Task CancelAsync(int id, string reason, string actor, bool administrator)
    {
        OperationRules.Reason(reason);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await Load(id);
        if (!administrator && (request.RequestedBy != actor || request.Status != StockRequestStatus.Pendiente)) throw new InvalidOperationException("Solo puedes cancelar tu solicitud pendiente.");
        if (request.Status is StockRequestStatus.Cancelada or StockRequestStatus.Rechazada or StockRequestStatus.Entregada) throw new InvalidOperationException("La solicitud ya está cerrada.");
        if (request.Status is StockRequestStatus.Aprobada or StockRequestStatus.Parcial)
            foreach (var line in request.Lines)
            {
                var remaining = line.Quantity - line.Delivered;
                line.StockLot.Reserved -= remaining;
                AddMovement(line.StockLot, StockMovementKind.Liberacion, 0, -remaining, actor, reason, line);
            }
        request.Status = StockRequestStatus.Cancelada; request.DecisionReason = reason;
        db.Audit(actor, "Solicitud cancelada", $"Bienes #{id}", reason);
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task ReturnAsync(int lineId, decimal quantity, string reason, string location, DateTime? expiry, string actor, Guid commandId)
    {
        await CheckCommand(commandId);
        OperationRules.Quantity(quantity); OperationRules.Reason(reason); OperationRules.Reason(location);
        if (location.Length > 160) throw new InvalidOperationException("Ubicación demasiado larga.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var line = await db.StockRequestLines.Include(x => x.StockRequest).Include(x => x.StockLot).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == lineId)
            ?? throw new InvalidOperationException("Préstamo no encontrado.");
        if (line.StockRequest.Kind != StockRequestKind.Prestamo) throw new InvalidOperationException("La solicitud no es un préstamo.");
        OperationRules.Available(line.Delivered - line.Returned - line.WrittenOff, quantity);
        ValidateExpiry(line.StockLot.Product, expiry);
        var lot = new StockLot { ProductId = line.StockLot.ProductId, Location = location.Trim(), ExpiresOn = expiry?.Date,
            OnHand = quantity, Origin = $"Reposición del préstamo #{line.StockRequestId}" };
        db.StockLots.Add(lot); line.Returned += quantity;
        // Touch request rowversion so simultaneous returns cannot overwrite the same outstanding amount.
        db.Entry(line.StockRequest).Property(x => x.Purpose).IsModified = true;
        AddMovement(lot, StockMovementKind.Reposicion, quantity, 0, actor, reason, line).CommandId = commandId;
        db.Audit(actor, "Reposición recibida", $"Bienes #{line.StockRequestId}", reason);
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task WriteOffAsync(int lineId, decimal quantity, string reason, string actor, Guid commandId)
    {
        await CheckCommand(commandId);
        OperationRules.Reason(reason);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var line = await db.StockRequestLines.Include(x => x.StockRequest).Include(x => x.StockLot).SingleOrDefaultAsync(x => x.Id == lineId)
            ?? throw new InvalidOperationException("Préstamo no encontrado.");
        OperationRules.OtherApprover(line.StockRequest.RequestedBy, actor);
        if (line.StockRequest.Kind != StockRequestKind.Prestamo) throw new InvalidOperationException("No es un préstamo.");
        OperationRules.Available(line.Delivered - line.Returned - line.WrittenOff, quantity);
        line.WrittenOff += quantity; db.Entry(line.StockRequest).Property(x => x.Purpose).IsModified = true;
        AddMovement(line.StockLot, StockMovementKind.Condonacion, 0, 0, actor, $"Condonadas {quantity} unidades: {reason}", line).CommandId = commandId;
        db.Audit(actor, "Reposición condonada", $"Bienes #{line.StockRequestId}", reason);
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task AdjustAsync(int lotId, decimal change, string reason, string actor, bool loss, Guid commandId)
    {
        await CheckCommand(commandId);
        OperationRules.Reason(reason); OperationRules.Quantity(Math.Abs(change));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var lot = await db.StockLots.SingleOrDefaultAsync(x => x.Id == lotId) ?? throw new InvalidOperationException("Lote inexistente.");
        if (lot.PendingReview) throw new InvalidOperationException("El lote está pendiente del visto bueno del almacén; confírmalo antes de ajustar.");
        if (loss && change >= 0) throw new InvalidOperationException("Una merma debe ser una baja.");
        if (lot.OnHand + change < lot.Reserved) throw new InvalidOperationException("El ajuste afectaría bienes comprometidos. Cancela o reasigna primero la reserva.");
        if (lot.OnHand + change > 9999999999.99m) throw new InvalidOperationException("El saldo del lote supera la capacidad admitida; registra un lote separado.");
        lot.OnHand += change;
        AddMovement(lot, loss ? StockMovementKind.Merma : change > 0 ? StockMovementKind.AjustePositivo : StockMovementKind.AjusteNegativo, change, 0, actor, reason).CommandId = commandId;
        db.Audit(actor, "Ajuste de inventario", $"Lote #{lotId}", reason);
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task ImportLegacyAsync(int donationId, int productId, string location, DateTime? expiry, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await db.Set<DonationItem>().SingleOrDefaultAsync(x => x.Id == donationId) ?? throw new InvalidOperationException("Bien no encontrado.");
        var donation = await db.Donations.SingleAsync(x => x.Id == item.DonationId);
        if (donation.Status != DonationStatus.Aprobada || await db.StockLots.AnyAsync(x => x.DonationItemId == item.Id)) throw new InvalidOperationException("El bien ya ingresó o la donación no está aprobada.");
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId && x.IsActive) ?? throw new InvalidOperationException("Selecciona producto.");
        if (!string.Equals(product.Unit.Trim(), item.Unit.Trim(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("La unidad del catálogo debe coincidir con la recepción histórica; no se convierte automáticamente.");
        ValidateExpiry(product, expiry); OperationRules.Reason(location);
        if (location.Length > 160) throw new InvalidOperationException("Ubicación demasiado larga.");
        item.ProductId = productId;
        var lot = new StockLot { ProductId = productId, DonationItem = item, Location = location, ExpiresOn = expiry?.Date, OnHand = item.Quantity, Origin = $"Recepción histórica #{donation.Id}" };
        db.StockLots.Add(lot); AddMovement(lot, StockMovementKind.Donacion, item.Quantity, 0, actor, "Ingreso físico verificado de recepción histórica");
        db.Audit(actor, "Bien histórico incorporado", $"Donación #{donation.Id}");
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task UnifyProductAsync(int duplicateId, int targetId, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (duplicateId == targetId) throw new InvalidOperationException("Elige dos productos distintos: el duplicado y el correcto.");
        var from = await db.Products.FindAsync(duplicateId) ?? throw new InvalidOperationException("Producto duplicado inexistente.");
        var to = await db.Products.FindAsync(targetId) ?? throw new InvalidOperationException("Producto correcto inexistente.");
        if (!from.IsActive || !to.IsActive) throw new InvalidOperationException("Ambos productos deben estar activos para unificar.");
        var lots = await db.StockLots.Where(x => x.ProductId == duplicateId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ProductId, targetId));
        var items = await db.Set<DonationItem>().Where(x => x.ProductId == duplicateId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ProductId, targetId));
        from.IsActive = false;
        from.Name = $"{from.Name.Trim()} (unificado)";
        db.Audit(actor, "Productos unificados", $"Producto #{duplicateId} -> Producto #{targetId}",
            $"{from.Name}: {lots} lote(s) y {items} bien(es) de donación movidos al producto '{to.Name.Trim()}'; quedó desactivado.");
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    private async Task CheckCommand(Guid commandId)
    {
        if (commandId == Guid.Empty) throw new InvalidOperationException("Actualiza el formulario antes de registrar la operación.");
        if (await db.StockMovements.AnyAsync(x => x.CommandId == commandId)) throw new InvalidOperationException("Esta operación ya fue registrada. Actualiza la página.");
    }
    private async Task<StockRequest> Load(int id) => await db.StockRequests.Include(x => x.Lines).ThenInclude(x => x.StockLot).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new InvalidOperationException("Solicitud no encontrada.");
    private static void Usable(StockLot lot)
    { if (lot.PendingReview || !lot.Product.IsActive || lot.ExpiresOn?.Date < ChurchTime.Today) throw new InvalidOperationException(lot.PendingReview ? "El lote todavía no tiene visto bueno del almacén." : "El producto está inactivo o el lote vencido."); }
    public static void ValidateExpiry(Product product, DateTime? expiry)
    { if (product.RequiresExpiry && !expiry.HasValue || expiry?.Date < ChurchTime.Today) throw new InvalidOperationException("Indica un vencimiento válido para el producto."); }
    private StockMovement AddMovement(StockLot lot, StockMovementKind kind, decimal quantity, decimal reserved, string actor, string reason, StockRequestLine? line = null)
    {
        var movement = new StockMovement { StockLot = lot, Kind = kind, Quantity = quantity, ReservedChange = reserved, ActorId = actor, Reason = reason, StockRequestLine = line };
        db.StockMovements.Add(movement); return movement;
    }
}
