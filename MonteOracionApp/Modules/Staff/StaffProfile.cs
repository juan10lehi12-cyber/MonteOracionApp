using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace MonteOracionApp.Modules.Staff;

public class StaffProfile
{
    [Key] public string UserId { get; set; } = "";
    public IdentityUser User { get; set; } = null!;
    [Required, StringLength(160), Display(Name = "Nombre completo")] public string FullName { get; set; } = "";
    [StringLength(100), Display(Name = "Cargo")] public string? Position { get; set; }
    public bool IsActive { get; set; } = true;
}
public class StaffForm
{
    public string? UserId { get; set; }
    [Required, StringLength(160), Display(Name = "Nombre completo")] public string FullName { get; set; } = "";
    [StringLength(100), Display(Name = "Cargo")] public string? Position { get; set; }
    [Required, EmailAddress, StringLength(256), Display(Name = "Correo")] public string Email { get; set; } = "";
    [StringLength(100, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Contraseña inicial / nueva (opcional al editar)")] public string? Password { get; set; }
    [Display(Name = "Activo")] public bool IsActive { get; set; } = true;
    public bool Administrator { get; set; }
    public bool Treasurer { get; set; }
    public bool Warehouse { get; set; }
}
