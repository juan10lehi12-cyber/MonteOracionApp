using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace MonteOracionApp.Modules.Congregants;
public class Congregant
{
    public int Id { get; set; }
    [RegularExpression("^[0-9]{8}$", ErrorMessage = "El DNI debe tener ocho dígitos."), StringLength(8), Display(Name = "DNI")] public string? Dni { get; set; }
    [Required, StringLength(100), Display(Name = "Nombres")] public string FirstName { get; set; } = "";
    [Required, StringLength(100), Display(Name = "Apellidos")] public string LastName { get; set; } = "";
    [StringLength(30), Phone, Display(Name = "Teléfono")] public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}
