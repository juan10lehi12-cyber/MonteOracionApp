using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Settings;
public class ChurchSettings
{
    public int Id { get; set; } = 1;
    [Required, StringLength(150), Display(Name = "Nombre de la iglesia")] public string Name { get; set; } = "Iglesia Monte de Oración";
    [Required, StringLength(3000), Display(Name = "Información de la iglesia")] public string About { get; set; } = "Bienvenidos a nuestra comunidad de fe en Comas, Lima.";
    [StringLength(300), Display(Name = "Dirección")] public string? Address { get; set; }
    [StringLength(100), Display(Name = "Contacto")] public string? Contact { get; set; }
}
public class PaymentMethod
{
    public int Id { get; set; }
    [Required, StringLength(100), Display(Name = "Medio de pago")] public string Name { get; set; } = "";
    [Required, StringLength(500), Display(Name = "Cuenta, número, titular e instrucciones")] public string Instructions { get; set; } = "";
    public string? QrStorageKey { get; set; }
    public string? QrContentType { get; set; }
    [Display(Name = "Disponible para donar")] public bool IsActive { get; set; } = true;
}
