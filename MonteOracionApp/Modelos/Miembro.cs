using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
namespace MonteOracionApp.Modelos;

[Table("tb_congregante")]
public class Congregante
{
    public int id { get; set; }
    [RegularExpression("^[0-9]{8}$", ErrorMessage = "El DNI debe tener ocho dígitos."), StringLength(8), Display(Name = "DNI")]
    public string? dni { get; set; }
    [Required, RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' \-\.]+$", ErrorMessage = "Los nombres solo admiten letras, espacios, punto, apóstrofe o guion."), StringLength(100), Display(Name = "Nombres")]
    public string nombres { get; set; } = "";
    [Required, RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' \-\.]+$", ErrorMessage = "Los apellidos solo admiten letras, espacios, punto, apóstrofe o guion."), StringLength(100), Display(Name = "Apellidos")]
    public string apellidos { get; set; } = "";
    [RegularExpression("^9[0-9]{8}$", ErrorMessage = "El teléfono debe ser un celular de 9 dígitos que empiece en 9."), StringLength(9), Display(Name = "Teléfono (9 dígitos)")]
    public string? telefono { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha de nacimiento")]
    public DateTime? fecha_nacimiento { get; set; }
    [StringLength(250), Display(Name = "Dirección (opcional)")]
    public string? direccion { get; set; }
    public bool activo { get; set; } = true;
    public string? id_usuario { get; set; }
    [ForeignKey(nameof(id_usuario))]
    public IdentityUser? usuario { get; set; }
    [NotMapped]
    public string nombre_completo => $"{nombres} {apellidos}";
}
