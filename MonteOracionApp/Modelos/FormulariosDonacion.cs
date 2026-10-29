using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modelos;

public class FormularioDonacionWeb
{
    [RegularExpression("^[0-9]{8}$", ErrorMessage = "Ingresa tu DNI de 8 dígitos."), Display(Name = "DNI del congregante")]
    public string? dni { get; set; }
    [Required, RegularExpression("^[A-Za-z0-9-]{4,20}$", ErrorMessage = "El número de operación admite de 4 a 20 caracteres (letras, números o guiones)."), Display(Name = "Número de operación")]
    public string numero_operacion { get; set; } = "";
    [Display(Name = "¿Para qué donas?")]
    public int? id_evento { get; set; }
    [Display(Name = "Donar de forma anónima")]
    public bool anonimo { get; set; }
    [Range(typeof(decimal), "0.01", "99999.99", ErrorMessage = "Ingresa un monto positivo hasta S/ 99 999.99 con dos decimales."), Display(Name = "Monto en soles")]
    public decimal monto { get; set; }
    [Required, Display(Name = "Medio de pago")]
    public int? id_medio_pago { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha del pago")]
    public DateTime fecha_pago { get; set; } = DateTime.UtcNow.AddHours(-5).Date;
    [Required, Display(Name = "Captura del comprobante (JPG o PNG, máximo 5 MB)")]
    public IFormFile? comprobante { get; set; }
}

public class FormularioRecepcionDonacion
{
    [Display(Name = "¿Qué se recibió?")]
    public TipoDonacion Kind { get; set; }
    [Display(Name = "¿Quién dona?")]
    public TipoDonante DonorKind { get; set; }
    [Display(Name = "Miembro (DNI o nombre)")]
    public int? CongregantId { get; set; }
    [StringLength(200), Display(Name = "Nombre completo del nuevo miembro")]
    public string? NewMemberName { get; set; }
    [RegularExpression("^[0-9]{8}$", ErrorMessage = "El DNI debe tener ocho dígitos."), StringLength(8), Display(Name = "DNI del nuevo miembro (opcional)")]
    public string? NewMemberDni { get; set; }
    [RegularExpression("^9[0-9]{8}$", ErrorMessage = "El teléfono debe ser un celular de 9 dígitos que empiece en 9."), StringLength(9), Display(Name = "Teléfono del nuevo miembro (opcional)")]
    public string? NewMemberPhone { get; set; }
    [StringLength(180), Display(Name = "Nombre del externo")]
    public string? ExternalName { get; set; }
    [StringLength(150), Display(Name = "Contacto (opcional)")]
    public string? ExternalContact { get; set; }
    [Display(Name = "Monto recibido en efectivo (S/)")]
    public decimal? Amount { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha de recepción")]
    public DateTime ReceivedOn { get; set; } = DateTime.UtcNow.AddHours(-5).Date;
    public List<ItemRecepcionForm> Items { get; set; } = [new()];
}

public class ItemRecepcionForm
{
    public int? ProductId { get; set; }
    [StringLength(160), Display(Name = "Producto nuevo (si no está en el catálogo)")]
    public string? NewProductName { get; set; }
    [StringLength(40), Display(Name = "Unidad del producto nuevo")]
    public string? NewProductUnit { get; set; }
    [StringLength(160)]
    public string? StorageLocation { get; set; }
    [StringLength(250)]
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    [StringLength(40)]
    public string? Unit { get; set; }
}

public enum OrigenDonacion { Digital, Presencial }

public class FiltroDonacion
{
    public EstadoDonacion? estado { get; set; }
    public TipoDonacion? tipo { get; set; }
    public TipoDonante? tipo_donante { get; set; }
    public OrigenDonacion? origen { get; set; }
    public string? donante { get; set; }
    public DateTime? desde { get; set; }
    public DateTime? hasta { get; set; }
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "page")]
    public int pagina { get; set; } = 1;
}

public record ListaDonaciones(List<Donacion> filas, FiltroDonacion filtro, int total, decimal dinero_aprobado);

public static class ReglasDonacion
{
    public static bool PuedeCambiarEstado(EstadoDonacion actual, EstadoDonacion siguiente) =>
        actual == EstadoDonacion.Pendiente && siguiente is EstadoDonacion.Aprobada or EstadoDonacion.Rechazada or EstadoDonacion.Anulada
        || actual == EstadoDonacion.Aprobada && siguiente == EstadoDonacion.Anulada;

    public static void ValidarFecha(DateTime fecha, DateTime hoyLima)
    {
        if (fecha == default || fecha.Date > hoyLima.Date)
            throw new InvalidOperationException("La fecha debe ser válida y no puede estar en el futuro.");
    }

    public static void ValidarMonto(decimal? monto)
    {
        if (monto is null or <= 0 or > 9999999999.99m || decimal.Round(monto.Value, 2) != monto)
            throw new InvalidOperationException("Ingresa un monto positivo con máximo dos decimales.");
    }

    public static void ValidarDecision(EstadoDonacion actual, EstadoDonacion siguiente, string? motivo)
    {
        if (!PuedeCambiarEstado(actual, siguiente)) throw new InvalidOperationException("El cambio de estado ya no está permitido. Actualiza la página.");
        if (siguiente is EstadoDonacion.Rechazada or EstadoDonacion.Anulada && string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Debes indicar el motivo.");
        if (motivo?.Length > 1000) throw new InvalidOperationException("El motivo admite hasta 1000 caracteres.");
    }
}
