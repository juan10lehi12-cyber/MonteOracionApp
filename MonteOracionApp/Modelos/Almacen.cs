using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Modelos;

[Table("tb_producto")]
public class Producto
{
    public int id { get; set; }
    [Required, StringLength(160), Display(Name = "Producto")]
    public string nombre { get; set; } = "";
    [Required, StringLength(40), Display(Name = "Unidad base (kg, lata, unidad.)")]
    public string unidad { get; set; } = "unidad";
    public bool activo { get; set; } = true;
}

[Table("tb_lote")]
public class Lote
{
    public int id { get; set; }
    public int id_producto { get; set; }
    [ForeignKey(nameof(id_producto))]
    public Producto producto { get; set; } = null!;
    public int? id_donacion_bien { get; set; }
    [ForeignKey(nameof(id_donacion_bien))]
    public DonacionBien? donacion_bien { get; set; }
    [MaxLength(160)]
    public string ubicacion { get; set; } = "";
    [MaxLength(200)]
    public string origen { get; set; } = "";
    public decimal cantidad_actual { get; set; }
    [Display(Name = "Pendiente de visto bueno")]
    public bool revision_pendiente { get; set; }
    [MaxLength(450)]
    public string? id_usuario_pendiente { get; set; }
    public DateTimeOffset? fecha_pendiente { get; set; }
    [MaxLength(450)]
    public string? confirmado_por { get; set; }
    public DateTimeOffset? fecha_confirmacion { get; set; }
    [Timestamp]
    public byte[] version { get; set; } = [];
}

public enum TipoMovimientoAlmacen { Donacion = 0, Salida = 3, VistoBueno = 9 }

[Table("tb_movimiento_almacen")]
public class MovimientoAlmacen
{
    public long id { get; set; }
    public Guid? id_comando { get; set; }
    public int id_lote { get; set; }
    [ForeignKey(nameof(id_lote))]
    public Lote lote { get; set; } = null!;
    public TipoMovimientoAlmacen tipo { get; set; }
    public decimal cantidad { get; set; }
    [MaxLength(450)]
    public string id_usuario { get; set; } = "";
    [MaxLength(1000)]
    public string motivo { get; set; } = "";
    [MaxLength(160)]
    public string? destino { get; set; }
    public DateTimeOffset fecha_hora { get; set; } = DateTimeOffset.UtcNow;
    public DateTime fecha_operacion { get; set; } = HoraIglesia.Hoy;
}

public class FormularioSalidaAlmacen
{
    [Required, StringLength(160), Display(Name = "Destino (evento, actividad o motivo)")]
    public string destino { get; set; } = "";
    [StringLength(500), Display(Name = "Detalle (opcional)")]
    public string? detalle { get; set; }
    public Guid id_comando { get; set; } = Guid.NewGuid();
    public List<LineaSalidaAlmacen> lineas { get; set; } = [];
}

public class LineaSalidaAlmacen
{
    public int id_lote { get; set; }
    public decimal? cantidad { get; set; }
}
