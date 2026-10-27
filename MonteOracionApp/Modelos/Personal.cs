using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
namespace MonteOracionApp.Modelos;

[Table("tb_personal")]
public class PerfilPersonal
{
    [Key]
    public string id_usuario { get; set; } = "";
    [ForeignKey(nameof(id_usuario))]
    public IdentityUser usuario { get; set; } = null!;
    [Required, StringLength(160), Display(Name = "Nombre completo")]
    public string nombre_completo { get; set; } = "";
    [StringLength(100), Display(Name = "Cargo")]
    public string? cargo { get; set; }
    public bool activo { get; set; } = true;
}

public class FormularioPersonal
{
    public string? id_usuario { get; set; }
    [Required, StringLength(160), Display(Name = "Nombre completo")]
    public string nombre_completo { get; set; } = "";
    [StringLength(100), Display(Name = "Cargo")]
    public string? cargo { get; set; }
    [Required, EmailAddress, StringLength(256), Display(Name = "Correo")]
    public string correo { get; set; } = "";
    [StringLength(100, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Contraseña inicial / nueva (opcional al editar)")]
    public string? clave { get; set; }
    [Display(Name = "Activo")]
    public bool activo { get; set; } = true;
    public bool administrador { get; set; }
    public bool personal { get; set; }
}
