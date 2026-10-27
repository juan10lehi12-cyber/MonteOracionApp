using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MonteOracionApp.Modelos;

public enum TipoDonacion { Dinero, Bienes }
public enum EstadoDonacion { Pendiente, Aprobada, Rechazada, Anulada }
public enum TipoDonante { Congregante, Externo, Anonimo }

[Table("tb_donacion")]
public class Donacion
{
    public int id { get; set; }
    public Guid codigo_seguimiento { get; set; } = Guid.NewGuid();
    public TipoDonacion tipo { get; set; }
    public EstadoDonacion estado { get; set; }
    public TipoDonante tipo_donante { get; set; }
    public int? id_congregante { get; set; }
    [ForeignKey(nameof(id_congregante))]
    public Congregante? congregante { get; set; }
    [StringLength(180)]
    public string? nombre_externo { get; set; }
    [StringLength(150)]
    public string? contacto_externo { get; set; }
    public decimal? monto { get; set; }
    [MaxLength(80)]
    public string? numero_operacion { get; set; }
    [Display(Name = "Evento")]
    public int? id_evento { get; set; }
    [ForeignKey(nameof(id_evento))]
    public Publicacion? evento { get; set; }
    public int? id_medio_pago { get; set; }
    [ForeignKey(nameof(id_medio_pago))]
    public MedioPago? medio_pago { get; set; }
    [StringLength(100)]
    public string? nombre_medio_pago { get; set; }
    public DateTime fecha_recepcion_pago { get; set; }
    public DateTimeOffset fecha_creacion { get; set; } = DateTimeOffset.UtcNow;
    public string? clave_comprobante { get; set; }
    public string? tipo_contenido_comprobante { get; set; }
    public string? id_usuario_registro { get; set; }
    public bool digital { get; set; }
    [Timestamp]
    public byte[] version { get; set; } = [];
    public List<DonacionBien> bienes { get; set; } = [];
    public List<DonacionHistorial> decisiones { get; set; } = [];
    [NotMapped]
    public string etiqueta_donante => tipo_donante switch
    {
        TipoDonante.Congregante => congregante?.nombre_completo ?? "Miembro",
        TipoDonante.Externo => nombre_externo ?? "Externo",
        _ => "Anónimo"
    };
}

[Table("tb_donacion_bien")]
public class DonacionBien
{
    public int id { get; set; }
    public int id_donacion { get; set; }
    [ForeignKey(nameof(id_donacion))]
    public Donacion? donacion { get; set; }
    public int? id_producto { get; set; }
    [ForeignKey(nameof(id_producto))]
    public Producto? producto { get; set; }
    [MaxLength(160)]
    public string? ubicacion_almacen { get; set; }
    [Required, StringLength(250)]
    public string descripcion { get; set; } = "";
    public decimal cantidad { get; set; }
    [Required, StringLength(40)]
    public string unidad { get; set; } = "";
}

[Table("tb_donacion_historial")]
public class DonacionHistorial
{
    public int id { get; set; }
    public int id_donacion { get; set; }
    [ForeignKey(nameof(id_donacion))]
    public Donacion? donacion { get; set; }
    public EstadoDonacion estado { get; set; }
    [Required]
    public string id_administrador { get; set; } = "";
    public DateTimeOffset fecha { get; set; } = DateTimeOffset.UtcNow;
    [StringLength(1000)]
    public string? motivo { get; set; }
}
