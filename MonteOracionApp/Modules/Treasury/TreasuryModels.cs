using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Treasury;
public enum MoneyChannel { Efectivo, Digital }
public class Fund
{
    public int Id { get; set; }
    [Required, StringLength(120), Display(Name = "Nombre del fondo")] public string Name { get; set; } = "";
    [Required, StringLength(500), Display(Name = "Finalidad / restricciones")] public string Purpose { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public enum FinanceKind { Gasto, Anticipo, SaldoInicial, AjusteIngreso, AjusteSalida }
public enum FinanceStatus { Pendiente, Aprobada, Pagada, Rechazada, Cancelada, Rendida }
public class FinanceRequest
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public Fund Fund { get; set; } = null!;
    public MoneyChannel Channel { get; set; }
    public FinanceKind Kind { get; set; }
    public FinanceStatus Status { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(1000)] public string Purpose { get; set; } = "";
    [MaxLength(160)] public string Payee { get; set; } = "";
    [MaxLength(450)] public string RequestedBy { get; set; } = "";
    [MaxLength(450)] public string? ApprovedBy { get; set; }
    [MaxLength(450)] public string? PaidBy { get; set; }
    [MaxLength(1000)] public string? DecisionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTime? DueOn { get; set; }
    public DateTime? PaidOn { get; set; }
    public DateTime? SettledOn { get; set; }
    public decimal? ActualSpent { get; set; }
    public string? EvidenceKey { get; set; }
    public string? EvidenceType { get; set; }
    public string? SettlementKey { get; set; }
    public string? SettlementType { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
public enum MoneyMovementKind { Donacion, AnulacionDonacion, Gasto, Anticipo, DevolucionAnticipo, SaldoInicial, Ajuste }
public class MoneyMovement
{
    public long Id { get; set; }
    public int FundId { get; set; }
    public Fund Fund { get; set; } = null!;
    public MoneyChannel Channel { get; set; }
    public MoneyMovementKind Kind { get; set; }
    public decimal Amount { get; set; }
    public int? DonationId { get; set; }
    public Donations.Donation? Donation { get; set; }
    public int? FinanceRequestId { get; set; }
    public FinanceRequest? FinanceRequest { get; set; }
    [MaxLength(450)] public string ActorId { get; set; } = "";
    [MaxLength(1000)] public string Detail { get; set; } = "";
    public DateTime BusinessDate { get; set; } = Shared.ChurchTime.Today;
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
public class CashClosing
{
    public MoneyChannel Channel { get; set; }
    public string? EvidenceKey { get; set; }
    public string? EvidenceType { get; set; }
    public int Id { get; set; }
    public DateTime Day { get; set; }
    public decimal Opening { get; set; }
    public decimal Income { get; set; }
    public decimal Outgoing { get; set; }
    public decimal Expected { get; set; }
    public decimal Counted { get; set; }
    [MaxLength(1000)] public string Notes { get; set; } = "";
    [MaxLength(450)] public string PreparedBy { get; set; } = "";
    [MaxLength(450)] public string? ReviewedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class FinanceForm
{
    [Range(1,int.MaxValue), Display(Name = "Fondo")] public int FundId { get; set; } = 1;
    [Display(Name = "Medio")] public MoneyChannel Channel { get; set; }
    [Display(Name = "Tipo")] public FinanceKind Kind { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999.99"), Display(Name = "Importe (S/)")] public decimal Amount { get; set; }
    [Required, StringLength(1000), Display(Name = "Propósito / motivo")] public string Purpose { get; set; } = "";
    [Required, StringLength(160), Display(Name = "Proveedor / responsable")] public string Payee { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "Fecha límite de rendición")] public DateTime? DueOn { get; set; }
}
public record FundBalance(Fund Fund, MoneyChannel Channel, decimal Balance, decimal Reserved);
