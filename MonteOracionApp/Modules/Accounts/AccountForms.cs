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
public class CreateAccountForm
{
    public int CongregantId { get; set; }
    [Required, EmailAddress, StringLength(256), Display(Name = "Correo de acceso")] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Contraseña inicial")] public string Password { get; set; } = "";
}
public class ResetPasswordForm
{
    public int CongregantId { get; set; }
    [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password), Display(Name = "Nueva contraseña")] public string Password { get; set; } = "";
}
