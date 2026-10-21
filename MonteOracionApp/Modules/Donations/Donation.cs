using System.ComponentModel.DataAnnotations;
using MonteOracionApp.Modules.Congregants;
using MonteOracionApp.Modules.Settings;
namespace MonteOracionApp.Modules.Donations;
public enum DonationKind { Dinero, Bienes }
public enum DonationStatus { Pendiente, Aprobada, Rechazada, Anulada }
public enum DonorKind { Congregante, Externo, Anonimo }
public class Donation
{
    public int Id { get; set; }
    public Guid TrackingCode { get; set; } = Guid.NewGuid();
    public DonationKind Kind { get; set; }
    public DonationStatus Status { get; set; }
    public DonorKind DonorKind { get; set; }
    public int? CongregantId { get; set; }
    public Congregant? Congregant { get; set; }
    [StringLength(180)] public string? ExternalName { get; set; }
    [StringLength(150)] public string? ExternalContact { get; set; }
    public decimal? Amount { get; set; }
    [MaxLength(80)] public string? OperationNumber { get; set; }
    public int FundId { get; set; } = 1;
    public Treasury.Fund Fund { get; set; } = null!;
    public int? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    [StringLength(100)] public string? PaymentMethodName { get; set; }
    public DateTime ReceivedOrPaidOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ReceiptStorageKey { get; set; }
    public string? ReceiptContentType { get; set; }
    public string? RecordedByUserId { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
    public List<DonationItem> Items { get; set; } = [];
    public List<DonationDecision> Decisions { get; set; } = [];
    public string DonorLabel => DonorKind switch { DonorKind.Congregante => Congregant?.FullName ?? "Congregante", DonorKind.Externo => ExternalName ?? "Externo", _ => "Anónimo" };
}
public class DonationItem
{
    public int Id { get; set; }
    public int DonationId { get; set; }
    public int? ProductId { get; set; }
    public Inventory.Product? Product { get; set; }
    public DateTime? ExpiresOn { get; set; }
    [MaxLength(160)] public string? StorageLocation { get; set; }
    [Required, StringLength(250)] public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    [Required, StringLength(40)] public string Unit { get; set; } = "";
}
public class DonationDecision
{
    public int Id { get; set; }
    public int DonationId { get; set; }
    public DonationStatus Status { get; set; }
    [Required] public string AdministratorId { get; set; } = "";
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
    [StringLength(1000)] public string? Reason { get; set; }
}
