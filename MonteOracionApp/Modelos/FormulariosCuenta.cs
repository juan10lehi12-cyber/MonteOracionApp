using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modelos;

public class FormularioAcceso
{
    [Required, Display(Name = "Usuario")]
    public string usuario { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "Contraseña")]
    public string clave { get; set; } = "";
    public string? url_retorno { get; set; }
}

public class FormularioClave
{
    [Required, DataType(DataType.Password), Display(Name = "Contraseña actual")]
    public string clave_actual { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 10, ErrorMessage = "La nueva contraseña debe tener al menos 10 caracteres."), DataType(DataType.Password), Display(Name = "Nueva contraseña")]
    public string clave_nueva { get; set; } = "";
    [Compare(nameof(clave_nueva)), DataType(DataType.Password), Display(Name = "Repetir nueva contraseña")]
    public string confirmar_clave { get; set; } = "";
}
