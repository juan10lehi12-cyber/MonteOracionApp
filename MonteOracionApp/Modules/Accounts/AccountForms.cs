using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Accounts;
public class LoginForm
{
    [Required, EmailAddress, Display(Name = "Correo")] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "Contraseña")] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class PasswordForm
{
    [Required, DataType(DataType.Password), Display(Name = "Contraseña actual")] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Nueva contraseña")] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Repetir nueva contraseña")] public string ConfirmPassword { get; set; } = "";
}
