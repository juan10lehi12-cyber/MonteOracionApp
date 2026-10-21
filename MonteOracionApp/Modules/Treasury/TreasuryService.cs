using System.Data;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Treasury;

public class TreasuryService(ApplicationDbContext db, IFileStorage storage)
{
    public async Task EnsureDayOpenAsync(MoneyChannel channel, DateTime day)
    {
        if (await db.CashClosings.AnyAsync(x => x.Channel == channel && x.Day >= day.Date))
            throw new InvalidOperationException("La fecha está incluida en un cierre de caja. Registra la corrección en una fecha abierta.");
    }
    public async Task<decimal> AvailableAsync(int fund, MoneyChannel channel)
    {
        var balance = await db.MoneyMovements.Where(x => x.FundId == fund && x.Channel == channel).SumAsync(x => (decimal?)x.Amount) ?? 0;
        var reserved = await db.FinanceRequests.Where(x => x.FundId == fund && x.Channel == channel && x.Status == FinanceStatus.Aprobada && x.Kind != FinanceKind.SaldoInicial && x.Kind != FinanceKind.AjusteIngreso).SumAsync(x => (decimal?)x.Amount) ?? 0;
        return balance - reserved;
    }
    public async Task DonationAsync(Donation donation, string actor, bool cancel = false)
    {
        var channel = donation.PaymentMethodId == null ? MoneyChannel.Efectivo : MoneyChannel.Digital;
        // Approval is recognized when confirmed; declared payment date remains on the donation.
        await EnsureDayOpenAsync(channel, ChurchTime.Today);
        if (cancel) OperationRules.Available(await AvailableAsync(donation.FundId, channel), donation.Amount!.Value);
        db.MoneyMovements.Add(new MoneyMovement { FundId = donation.FundId, Channel = channel,
            Amount = donation.Amount!.Value * (cancel ? -1 : 1), Donation = donation, ActorId = actor,
            Kind = cancel ? MoneyMovementKind.AnulacionDonacion : MoneyMovementKind.Donacion,
            Detail = cancel ? "Anulación de donación confirmada" : "Donación verificada" });
    }
    public async Task<int> RequestAsync(FinanceForm form, string actor, bool administrator)
    {
        DonationRules.ValidateAmount(form.Amount); OperationRules.Reason(form.Purpose);
        if (!Enum.IsDefined(form.Channel) || !Enum.IsDefined(form.Kind)) throw new InvalidOperationException("Tipo o medio inválido.");
        if (!administrator && form.Kind is not (FinanceKind.Gasto or FinanceKind.Anticipo)) throw new InvalidOperationException("Solo administración solicita saldos iniciales y ajustes.");
        if (!await db.Funds.AnyAsync(x => x.Id == form.FundId && x.IsActive)) throw new InvalidOperationException("El fondo no existe o está inactivo.");
        if (form.Kind == FinanceKind.Anticipo && (!form.DueOn.HasValue || form.DueOn.Value.Date < ChurchTime.Today)) throw new InvalidOperationException("Indica fecha límite de rendición.");
        var request = new FinanceRequest { FundId = form.FundId, Channel = form.Channel, Kind = form.Kind, Amount = form.Amount,
            Purpose = form.Purpose.Trim(), Payee = form.Payee.Trim(), RequestedBy = actor, DueOn = form.Kind == FinanceKind.Anticipo ? form.DueOn?.Date : null };
        db.FinanceRequests.Add(request); db.Audit(actor, "Solicitud de dinero", "Nueva", request.Purpose);
        await db.SaveOperationAsync(); return request.Id;
    }
    public async Task DecideAsync(int id, bool approve, string reason, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await Load(id); OperationRules.OtherApprover(r.RequestedBy, actor);
        if (r.Status != FinanceStatus.Pendiente) throw new InvalidOperationException("La solicitud ya fue atendida.");
        if (!approve) OperationRules.Reason(reason);
        if (approve)
        {
            if (!await db.Funds.AnyAsync(x => x.Id == r.FundId && x.IsActive)) throw new InvalidOperationException("Fondo inactivo.");
            if (r.Kind == FinanceKind.SaldoInicial && (await db.MoneyMovements.AnyAsync(x => x.FundId == r.FundId && x.Channel == r.Channel) || await db.FinanceRequests.AnyAsync(x => x.Id != r.Id && x.FundId == r.FundId && x.Channel == r.Channel && x.Kind == FinanceKind.SaldoInicial && x.Status == FinanceStatus.Aprobada)))
                throw new InvalidOperationException("Ya hay movimientos o un saldo inicial aprobado. Usa un ajuste justificado.");
            if (r.Kind is not (FinanceKind.SaldoInicial or FinanceKind.AjusteIngreso)) OperationRules.Available(await AvailableAsync(r.FundId, r.Channel), r.Amount);
        }
        r.Status = approve ? FinanceStatus.Aprobada : FinanceStatus.Rechazada; r.ApprovedBy = actor; r.DecisionReason = reason;
        db.Audit(actor, approve ? "Dinero autorizado" : "Solicitud rechazada", $"Dinero #{id}", reason);
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task PayAsync(int id, IFormFile? evidence, string actor)
    {
        if (evidence == null) throw new InvalidOperationException("Adjunta comprobante o constancia JPG, PNG o PDF.");
        var file = await storage.SaveAsync(evidence);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var r = await Load(id);
            if (r.Status != FinanceStatus.Aprobada) throw new InvalidOperationException("La solicitud no está aprobada.");
            await EnsureDayOpenAsync(r.Channel, ChurchTime.Today);
            if (r.Kind == FinanceKind.SaldoInicial && await db.MoneyMovements.AnyAsync(x => x.FundId == r.FundId && x.Channel == r.Channel)) throw new InvalidOperationException("Ya hay movimientos en el fondo. Solicita un ajuste.");
            if (r.Kind is not (FinanceKind.SaldoInicial or FinanceKind.AjusteIngreso) && await AvailableAsync(r.FundId, r.Channel) < 0) throw new InvalidOperationException("El saldo ya no cubre las reservas.");
            var incoming = r.Kind is FinanceKind.SaldoInicial or FinanceKind.AjusteIngreso;
            r.Status = FinanceStatus.Pagada; r.PaidOn = ChurchTime.Today; r.PaidBy = actor; r.EvidenceKey = file.Key; r.EvidenceType = file.ContentType;
            db.MoneyMovements.Add(new MoneyMovement { FundId = r.FundId, Channel = r.Channel, FinanceRequestId = id, Amount = incoming ? r.Amount : -r.Amount,
                Kind = r.Kind == FinanceKind.Anticipo ? MoneyMovementKind.Anticipo : r.Kind == FinanceKind.SaldoInicial ? MoneyMovementKind.SaldoInicial : r.Kind == FinanceKind.Gasto ? MoneyMovementKind.Gasto : MoneyMovementKind.Ajuste,
                ActorId = actor, Detail = r.Purpose });
            db.Audit(actor, "Movimiento de dinero ejecutado", $"Dinero #{id}", r.Purpose);
            await db.SaveOperationAsync(); await tx.CommitAsync();
        }
        catch { storage.Delete(file.Key); throw; }
    }
    public async Task SettleAsync(int id, decimal spent, IFormFile? evidence, string actor)
    {
        if (spent < 0 || decimal.Round(spent, 2) != spent || evidence == null) throw new InvalidOperationException("Indica gasto real con dos decimales y adjunta rendición.");
        var file = await storage.SaveAsync(evidence);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var r = await Load(id);
            if (r.Kind != FinanceKind.Anticipo || r.Status != FinanceStatus.Pagada || spent > r.Amount) throw new InvalidOperationException("Rendición inválida; el gasto no debe superar el anticipo. Solicita por separado cualquier gasto adicional.");
            await EnsureDayOpenAsync(r.Channel, ChurchTime.Today);
            r.ActualSpent = spent; r.Status = FinanceStatus.Rendida; r.SettledOn = ChurchTime.Today;
            r.SettlementKey = file.Key; r.SettlementType = file.ContentType;
            if (spent < r.Amount) db.MoneyMovements.Add(new MoneyMovement { FundId = r.FundId, Channel = r.Channel, FinanceRequestId = id,
                Amount = r.Amount - spent, Kind = MoneyMovementKind.DevolucionAnticipo, ActorId = actor, Detail = "Saldo sobrante recibido de anticipo" });
            db.Audit(actor, "Anticipo rendido y devolución recibida", $"Dinero #{id}", $"Gasto real: {spent:0.00}; devolución: {r.Amount - spent:0.00}");
            await db.SaveOperationAsync(); await tx.CommitAsync();
        }
        catch { storage.Delete(file.Key); throw; }
    }
    public async Task CancelAsync(int id, string reason, string actor, bool administrator)
    {
        OperationRules.Reason(reason);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await Load(id);
        if (r.Status is not (FinanceStatus.Pendiente or FinanceStatus.Aprobada) || !administrator && (r.RequestedBy != actor || r.Status != FinanceStatus.Pendiente))
            throw new InvalidOperationException("La solicitud no puede cancelarse por este usuario o ya tiene movimientos.");
        r.Status = FinanceStatus.Cancelada; r.DecisionReason = reason;
        db.Audit(actor, "Solicitud de dinero cancelada", $"Dinero #{id}", reason);
        await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    public async Task CloseAsync(DateTime day, decimal counted, string notes, string actor, MoneyChannel channel, IFormFile? evidence)
    {
        if (!Enum.IsDefined(channel)) throw new InvalidOperationException("Medio inválido.");
        if (channel == MoneyChannel.Digital && evidence == null) throw new InvalidOperationException("Adjunta el estado de cuenta / constancia de conciliación digital.");
        if (day == default || day.Date > ChurchTime.Today || counted < 0 || counted > 9999999999.99m || decimal.Round(counted, 2) != counted) throw new InvalidOperationException("Fecha o conteo inválidos.");
        if (notes?.Length > 1000) throw new InvalidOperationException("Observaciones demasiado largas.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.CashClosings.AnyAsync(x => x.Channel == channel && x.Day >= day.Date)) throw new InvalidOperationException("El cierre debe ser posterior al último cierre y no puede repetirse.");
        var q = db.MoneyMovements.Where(x => x.Channel == channel);
        var opening = await q.Where(x => x.BusinessDate < day.Date).SumAsync(x => (decimal?)x.Amount) ?? 0;
        var income = await q.Where(x => x.BusinessDate == day.Date && x.Amount > 0).SumAsync(x => (decimal?)x.Amount) ?? 0;
        var outgoing = -(await q.Where(x => x.BusinessDate == day.Date && x.Amount < 0).SumAsync(x => (decimal?)x.Amount) ?? 0);
        var expected = opening + income - outgoing;
        if (counted != expected) OperationRules.Reason(notes);
        var file = evidence == null ? null : await storage.SaveAsync(evidence);
        db.CashClosings.Add(new CashClosing { Channel = channel, EvidenceKey = file?.Key, EvidenceType = file?.ContentType, Day = day.Date, Opening = opening, Income = income, Outgoing = outgoing,
            Expected = expected, Counted = counted, Notes = notes ?? "", PreparedBy = actor });
        db.Audit(actor, "Cierre / conciliación " + channel, day.ToString("yyyy-MM-dd"), $"Esperado {expected:0.00}; contado {counted:0.00}. {notes}");
        try { await db.SaveOperationAsync(); await tx.CommitAsync(); } catch { if (file != null) storage.Delete(file.Key); throw; }
    }
    public async Task ReviewClosingAsync(int id, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var close = await db.CashClosings.SingleOrDefaultAsync(x => x.Id == id) ?? throw new InvalidOperationException("Cierre inexistente.");
        OperationRules.OtherApprover(close.PreparedBy, actor);
        if (close.ReviewedBy != null) throw new InvalidOperationException("El cierre ya fue revisado.");
        var updated = await db.CashClosings.Where(x => x.Id == id && x.ReviewedBy == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedBy, actor));
        if (updated != 1) throw new InvalidOperationException("Otra persona ya revisó el cierre.");
        db.Audit(actor, "Cierre revisado", $"Caja #{id}"); await db.SaveOperationAsync(); await tx.CommitAsync();
    }
    private async Task<FinanceRequest> Load(int id) => await db.FinanceRequests.SingleOrDefaultAsync(x => x.Id == id) ?? throw new InvalidOperationException("Solicitud no encontrada.");
}
