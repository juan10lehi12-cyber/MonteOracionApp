using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Donations;
    public class OnlineDonationForm
    {
        // Ruta identificada: solo DNI; sin nombres, teléfonos ni correos. Anónimo oculta el DNI.
        // Destino único: General (por defecto) o un evento con recaudación activa.
        [RegularExpression("^[0-9]{8}$", ErrorMessage = "Ingresa tu DNI de 8 dígitos."), Display(Name = "DNI del congregante")] public string? Dni { get; set; }
        [Required, RegularExpression("^[A-Za-z0-9-]{4,20}$", ErrorMessage = "El número de operación admite de 4 a 20 caracteres (letras, números o guiones)."), Display(Name = "Número de operación")] public string OperationNumber { get; set; } = "";
        [Display(Name = "¿Para qué donas?")] public int? EventId { get; set; }
        [Display(Name = "Donar de forma anónima")] public bool Anonymous { get; set; }
        [Range(typeof(decimal), "0.01", "99999.99", ErrorMessage = "Ingresa un monto positivo hasta S/ 99 999.99 con dos decimales."), Display(Name = "Monto en soles")] public decimal Amount { get; set; }
        [Required, Display(Name = "Medio de pago")] public int? PaymentMethodId { get; set; }
        [DataType(DataType.Date), Display(Name = "Fecha del pago")] public DateTime PaidOn { get; set; } = DateTime.UtcNow.AddHours(-5).Date;
        [Required, Display(Name = "Captura del comprobante (JPG o PNG, máximo 5 MB)")] public IFormFile? Receipt { get; set; }
    }
public class ReceivedDonationForm
{
    // Fondo interno: el efectivo siempre entra al fondo activo General; la decisión fina la toma la administración.
    [Range(0, int.MaxValue)] public int FundId { get; set; } = 1;
    [Display(Name = "¿Qué se recibió?")] public DonationKind Kind { get; set; }
    [Display(Name = "¿Quién dona?")] public DonorKind DonorKind { get; set; }
    [Display(Name = "Miembro (DNI o nombre)")] public int? CongregantId { get; set; }
    [StringLength(200), Display(Name = "Nombre completo del nuevo miembro")] public string? NewMemberName { get; set; }
    [RegularExpression("^[0-9]{8}$", ErrorMessage = "El DNI debe tener ocho dígitos."), StringLength(8), Display(Name = "DNI del nuevo miembro (opcional)")] public string? NewMemberDni { get; set; }
    [RegularExpression("^9[0-9]{8}$", ErrorMessage = "El teléfono debe ser un celular de 9 dígitos que empiece en 9."), StringLength(9), Display(Name = "Teléfono del nuevo miembro (opcional)")] public string? NewMemberPhone { get; set; }
    [StringLength(180), Display(Name = "Nombre del externo")] public string? ExternalName { get; set; }
    [StringLength(150), Display(Name = "Contacto (opcional)")] public string? ExternalContact { get; set; }
    [Display(Name = "Monto recibido en efectivo (S/)")] public decimal? Amount { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha de recepción")] public DateTime ReceivedOn { get; set; } = DateTime.UtcNow.AddHours(-5).Date;
    public List<ReceivedItemForm> Items { get; set; } = [new()];
}
public class ReceivedItemForm
{
    public int? ProductId { get; set; }
    [StringLength(160), Display(Name = "Producto nuevo (si no está en el catálogo)")] public string? NewProductName { get; set; }
    [StringLength(40), Display(Name = "Unidad del producto nuevo")] public string? NewProductUnit { get; set; }
    [Display(Name = "El producto nuevo requiere vencimiento")] public bool NewProductRequiresExpiry { get; set; }
    public DateTime? ExpiresOn { get; set; }
    [StringLength(160)] public string? StorageLocation { get; set; }
    [StringLength(250)] public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    [StringLength(40)] public string? Unit { get; set; }
}
public enum DonationOrigin { Digital, Presencial }
public class DonationFilter
{
    public DonationStatus? Status { get; set; }
    public DonationKind? Kind { get; set; }
    public DonorKind? DonorType { get; set; }
    public DonationOrigin? Origin { get; set; }
    public string? Donor { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
}
public record DonationList(List<Donation> Rows, DonationFilter Filter, int Total, decimal ApprovedMoney);
