using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MonteOracionApp.Modelos;

[Table("tb_iglesia")]
public class ConfiguracionIglesia
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int id { get; set; } = 1;
    [Required, StringLength(150), Display(Name = "Nombre de la iglesia")]
    public string nombre { get; set; } = "Iglesia Monte de Oración";
    [Required, StringLength(3000), Display(Name = "Información de la iglesia")]
    public string informacion { get; set; } = "Bienvenidos a nuestra comunidad de fe en Comas, Lima.";
    [StringLength(300), Display(Name = "Dirección")]
    public string? direccion { get; set; }
    [StringLength(100), Display(Name = "Contacto")]
    public string? contacto { get; set; }
    [StringLength(600), Display(Name = "Horarios y reuniones")]
    public string? horarios { get; set; }
}

[Table("tb_medio_pago")]
public class MedioPago
{
    public int id { get; set; }
    [Required, StringLength(100), Display(Name = "Medio de pago")]
    public string nombre { get; set; } = "";
    [Required, StringLength(500), Display(Name = "Cuenta, número, titular e instrucciones")]
    public string instrucciones { get; set; } = "";
    [StringLength(50), Display(Name = "Número visible (teléfono o cuenta a copiar)")]
    public string? numero_cuenta { get; set; }
    public string? clave_qr { get; set; }
    public string? tipo_contenido_qr { get; set; }
    [Display(Name = "Disponible para donar")]
    public bool activo { get; set; } = true;
}
