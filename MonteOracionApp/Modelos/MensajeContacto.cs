using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace MonteOracionApp.Modelos;

public enum TipoMotivoContacto
{
    [Display(Name = "Petición de oración")]
    PeticionOracion = 0,

    [Display(Name = "Consejería pastoral")]
    Consejeria = 1,

    [Display(Name = "Información sobre cultos y reuniones")]
    Informacion = 2,

    [Display(Name = "Deseo apoyar o servir en un ministerio")]
    Servir = 3,

    [Display(Name = "Otro motivo")]
    Otro = 4
}

public enum PreferenciaContacto
{
    [Display(Name = "Por llamada telefónica")]
    Llamada = 0,

    [Display(Name = "Por mensaje de WhatsApp")]
    WhatsApp = 1,

    [Display(Name = "Cualquiera de las dos")]
    Indiferente = 2
}

public enum EstadoMensajeContacto
{
    [Display(Name = "Pendiente")]
    Pendiente = 0,

    [Display(Name = "Atendido")]
    Atendido = 1,

    [Display(Name = "Archivado")]
    Archivado = 2
}

[Table("tb_mensaje_contacto")]
public class MensajeContacto
{
    public int id { get; set; }
    public DateTimeOffset fecha_creacion { get; set; } = DateTimeOffset.UtcNow;

    public int? id_congregante { get; set; }
    [ForeignKey(nameof(id_congregante))]
    public Congregante? congregante { get; set; }

    [Required, StringLength(150), Display(Name = "Nombre completo")]
    public string nombre { get; set; } = "";

    [Required, RegularExpression(@"^[\d\s\+\-\(\)]{7,30}$", ErrorMessage = "Ingresa un número telefónico o celular válido."), StringLength(30), Display(Name = "Teléfono o celular")]
    public string telefono { get; set; } = "";

    [EmailAddress, StringLength(256), Display(Name = "Correo electrónico (opcional)")]
    public string? correo { get; set; }

    [Display(Name = "Motivo del mensaje")]
    public TipoMotivoContacto motivo { get; set; } = TipoMotivoContacto.PeticionOracion;

    [Display(Name = "Preferencia de contacto")]
    public PreferenciaContacto preferencia { get; set; } = PreferenciaContacto.WhatsApp;

    [Required, StringLength(2000, MinimumLength = 10, ErrorMessage = "Escribe un mensaje de al menos 10 caracteres."), Display(Name = "Mensaje o motivo de oración")]
    public string mensaje { get; set; } = "";

    public EstadoMensajeContacto estado { get; set; } = EstadoMensajeContacto.Pendiente;
    public DateTimeOffset? fecha_atencion { get; set; }

    [StringLength(450)]
    public string? id_usuario { get; set; }
    [ForeignKey(nameof(id_usuario))]
    public IdentityUser? usuario { get; set; }

    [StringLength(1000), Display(Name = "Notas de atención pastoral")]
    public string? notas_atencion { get; set; }

    [NotMapped]
    public string? UrlWhatsApp
    {
        get
        {
            if (string.IsNullOrWhiteSpace(telefono)) return null;
            var soloDigitos = System.Text.RegularExpressions.Regex.Replace(telefono, "[^0-9]", "");
            if (soloDigitos.Length == 11 && soloDigitos.StartsWith("519"))
                return $"https://wa.me/{soloDigitos}";
            if (soloDigitos.Length == 9 && soloDigitos.StartsWith("9"))
                return $"https://wa.me/51{soloDigitos}";
            if (soloDigitos.Length >= 8)
                return $"https://wa.me/{soloDigitos}";
            return null;
        }
    }
}

public class FormularioContacto
{
    [Required(ErrorMessage = "Por favor ingresa tu nombre y apellido."), StringLength(150), Display(Name = "¿Cómo te llamas?")]
    public string nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu número de teléfono o celular para que podamos comunicarnos."), RegularExpression(@"^[\d\s\+\-\(\)]{7,30}$", ErrorMessage = "Ingresa un número telefónico o celular válido (mínimo 7 dígitos)."), StringLength(30), Display(Name = "Tu número de teléfono o celular")]
    public string telefono { get; set; } = "";

    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido."), StringLength(256), Display(Name = "Correo electrónico (opcional)")]
    public string? correo { get; set; }

    [Display(Name = "¿En qué te podemos ayudar? (Motivo)")]
    public TipoMotivoContacto motivo { get; set; } = TipoMotivoContacto.PeticionOracion;

    [Display(Name = "¿Cómo prefieres que nos comuniquemos contigo?")]
    public PreferenciaContacto preferencia { get; set; } = PreferenciaContacto.WhatsApp;

    [Required(ErrorMessage = "Por favor escribe tu mensaje o motivo de oración."), StringLength(2000, MinimumLength = 10, ErrorMessage = "Escribe un mensaje de al menos 10 caracteres."), Display(Name = "Cuéntanos tu mensaje o motivo de oración")]
    public string mensaje { get; set; } = "";
}

public class FiltroMensajeContacto
{
    public EstadoMensajeContacto? estado { get; set; }
    public TipoMotivoContacto? motivo { get; set; }
    public string? buscar { get; set; }
    public DateTime? desde { get; set; }
    public DateTime? hasta { get; set; }
    public int pagina { get; set; } = 1;
}
