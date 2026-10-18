using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Publications;
public enum PublicationKind { Evento, Aviso }
public class Publication
{
    public int Id { get; set; }
    [Display(Name = "Tipo")] public PublicationKind Kind { get; set; }
    [Required, StringLength(160), Display(Name = "Título")] public string Title { get; set; } = "";
    [Required, StringLength(6000), Display(Name = "Contenido")] public string Body { get; set; } = "";
    [Display(Name = "Fecha y hora (Lima)")] public DateTime? EventAt { get; set; }
    [StringLength(250), Display(Name = "Lugar")] public string? Location { get; set; }
    [Display(Name = "Publicado")] public bool IsPublished { get; set; }
    public bool IsCancelled { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
