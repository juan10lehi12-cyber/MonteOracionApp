using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Donations;
public class OnlineDonationForm
{
    [RegularExpression("^[0-9]{8}$"), Display(Name = "DNI del congregante (opcional)")] public string? Dni { get; set; }
    [Required, StringLength(80), Display(Name = "Número de operación")] public string OperationNumber { get; set; } = "";
    [Range(1,int.MaxValue), Display(Name = "Destino / fondo")] public int FundId { get; set; } = 1;
    [Display(Name = "Donar de forma anónima")] public bool Anonymous { get; set; }
    [StringLength(180), Display(Name = "Nombre completo")] public string? Name { get; set; }
    [StringLength(150), Display(Name = "Teléfono o correo (opcional)")] public string? Contact { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999.99"), Display(Name = "Monto en soles")] public decimal Amount { get; set; }
    [Required, Display(Name = "Medio de pago")] public int? PaymentMethodId { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha del pago")] public DateTime PaidOn { get; set; } = DateTime.UtcNow.AddHours(-5).Date;
    [Required, Display(Name = "Captura del comprobante (JPG o PNG, máximo 5 MB)")] public IFormFile? Receipt { get; set; }
}
public class ReceivedDonationForm
{
    [Range(1,int.MaxValue), Display(Name = "Fondo del efectivo")] public int FundId { get; set; } = 1;
    [Display(Name = "Tipo de donación")] public DonationKind Kind { get; set; }
    [Display(Name = "Tipo de donante")] public DonorKind DonorKind { get; set; }
    [Display(Name = "Congregante")] public int? CongregantId { get; set; }
    [StringLength(180), Display(Name = "Nombre del externo")] public string? ExternalName { get; set; }
    [StringLength(150), Display(Name = "Contacto (opcional)")] public string? ExternalContact { get; set; }
    [Display(Name = "Monto recibido en efectivo (S/)")] public decimal? Amount { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha de recepción")] public DateTime ReceivedOn { get; set; } = DateTime.UtcNow.AddHours(-5).Date;
    [Display(Name = "Foto del bien recibido (JPG o PNG, máximo 5 MB)")] public IFormFile? ItemPhoto { get; set; }
    public List<ReceivedItemForm> Items { get; set; } = [new()];
    [Display(Name = "Confirmo que recibí el dinero o los bienes")] public bool ConfirmReceived { get; set; }
}
public class ReceivedItemForm
{
    public int? ProductId { get; set; }
    public DateTime? ExpiresOn { get; set; }
    [StringLength(160)] public string? StorageLocation { get; set; }
    [StringLength(250)] public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    [StringLength(40)] public string? Unit { get; set; }
}
public class DonationFilter
{
    public DonationStatus? Status { get; set; }
    public DonationKind? Kind { get; set; }
    public string? Donor { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
}
public record DonationList(List<Donation> Rows, DonationFilter Filter, int Total, decimal ApprovedMoney);
