using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MonteOracionApp.Modelos;

public enum TipoPublicacion { Evento, Aviso }

[Table("tb_publicacion")]
public class Publicacion
{
    public int id { get; set; }
    [Display(Name = "Tipo")]
    public TipoPublicacion tipo { get; set; }
    [Required, StringLength(160), Display(Name = "Título")]
    public string titulo { get; set; } = "";
    [Required, StringLength(6000), Display(Name = "Contenido")]
    public string contenido { get; set; } = "";
    [Display(Name = "Fecha y hora (Lima)")]
    public DateTime? fecha_inicio_evento { get; set; }
    [Display(Name = "Fin del evento (Lima)")]
    public DateTime? fecha_fin_evento { get; set; }
    public string? clave_imagen { get; set; }
    public string? tipo_imagen { get; set; }
    [StringLength(250), Display(Name = "Lugar")]
    public string? lugar { get; set; }
    [Display(Name = "Meta de recaudación (S/)")]
    public decimal? meta_recaudacion { get; set; }
    [Display(Name = "Publicado")]
    public bool publicado { get; set; }
    public bool cancelado { get; set; }
    public DateTimeOffset fecha_creacion { get; set; } = DateTimeOffset.UtcNow;
}
