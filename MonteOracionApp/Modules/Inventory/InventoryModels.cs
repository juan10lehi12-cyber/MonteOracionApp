using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Inventory;

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(160), Display(Name = "Producto")] public string Name { get; set; } = "";
    [Required, StringLength(40), Display(Name = "Unidad base (kg, lata, unidad…)")] public string Unit { get; set; } = "unidad";
    [Required, StringLength(100), Display(Name = "Categoría")] public string Category { get; set; } = "Alimentos";
    [Display(Name = "Requiere vencimiento")] public bool RequiresExpiry { get; set; }
    public bool IsActive { get; set; } = true;
}
public class StockLot
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? DonationItemId { get; set; }
    public Donations.DonationItem? DonationItem { get; set; }
    [MaxLength(160)] public string Location { get; set; } = "";
    [MaxLength(200)] public string Origin { get; set; } = "";
    public DateTime? ExpiresOn { get; set; }
    public decimal OnHand { get; set; }
    public decimal Reserved { get; set; }
    [Display(Name = "Pendiente de visto bueno")] public bool PendingReview { get; set; }
    [MaxLength(450)] public string? PendingActorId { get; set; }
    public DateTimeOffset? PendingAt { get; set; }
    [MaxLength(450)] public string? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
public class Beneficiary
{
    public int Id { get; set; }
    [Required, StringLength(160), Display(Name = "Persona o institución")] public string Name { get; set; } = "";
    [Required, StringLength(250), Display(Name = "Dirección / distrito")] public string Address { get; set; } = "";
    [Required, StringLength(160), Display(Name = "Responsable / contacto")] public string Contact { get; set; } = "";
    [StringLength(30), Display(Name = "Teléfono")] public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
public enum StockRequestKind { Entrega, UsoInterno, Prestamo }
public enum StockRequestStatus { Pendiente, Aprobada, Parcial, Entregada, Rechazada, Cancelada }
public enum StockPriority { Normal, Alta, Urgente }
public class StockRequest
{
    public int Id { get; set; }
    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;
    [MaxLength(160)] public string BeneficiaryName { get; set; } = "";
    [MaxLength(250)] public string BeneficiaryAddress { get; set; } = "";
    [MaxLength(160)] public string BeneficiaryContact { get; set; } = "";
    [MaxLength(1000)] public string Purpose { get; set; } = "";
    public StockPriority Priority { get; set; }
    public StockRequestKind Kind { get; set; }
    public StockRequestStatus Status { get; set; }
    public DateTime NeededOn { get; set; }
    public DateTime? ReturnDueOn { get; set; }
    [MaxLength(450)] public string RequestedBy { get; set; } = "";
    [MaxLength(450)] public string? ApprovedBy { get; set; }
    [MaxLength(1000)] public string? DecisionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [Timestamp] public byte[] Version { get; set; } = [];
    public List<StockRequestLine> Lines { get; set; } = [];
}
public class StockRequestLine
{
    public int Id { get; set; }
    public int StockRequestId { get; set; }
    public StockRequest StockRequest { get; set; } = null!;
    public int StockLotId { get; set; }
    public StockLot StockLot { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal Delivered { get; set; }
    public decimal Returned { get; set; }
    public decimal WrittenOff { get; set; }
}
public enum StockMovementKind { Donacion, Reserva, Liberacion, Entrega, Reposicion, Merma, AjustePositivo, AjusteNegativo, Condonacion, VistoBueno }
public class StockMovement
{
    public long Id { get; set; }
    public Guid? CommandId { get; set; }
    public int StockLotId { get; set; }
    public StockLot StockLot { get; set; } = null!;
    public int? StockRequestLineId { get; set; }
    public StockRequestLine? StockRequestLine { get; set; }
    public StockMovementKind Kind { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedChange { get; set; }
    [MaxLength(450)] public string ActorId { get; set; } = "";
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(160)] public string? Receiver { get; set; }
    public string? EvidenceKey { get; set; }
    public string? EvidenceType { get; set; }
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
    public DateTime BusinessDate { get; set; } = Shared.ChurchTime.Today;
}
public class StockRequestForm
{
    [Range(1,int.MaxValue), Display(Name = "Beneficiario / responsable del préstamo")] public int BeneficiaryId { get; set; }
    [Required, StringLength(1000), Display(Name = "Motivo y justificación de prioridad")] public string Purpose { get; set; } = "";
    [Display(Name = "Tipo")] public StockRequestKind Kind { get; set; }
    [Display(Name = "Prioridad")] public StockPriority Priority { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha necesaria")] public DateTime NeededOn { get; set; } = Shared.ChurchTime.Today;
    [DataType(DataType.Date), Display(Name = "Fecha límite de reposición")] public DateTime? ReturnDueOn { get; set; }
    public List<StockLineForm> Lines { get; set; } = [new()];
}
public class StockLineForm
{
    public int StockLotId { get; set; }
    public decimal Quantity { get; set; }
}
