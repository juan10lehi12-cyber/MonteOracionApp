namespace MonteOracionApp.Modelos;

public class ModeloErrorVista
{
    public string? id_solicitud { get; set; }
    public bool mostrar_id_solicitud => !string.IsNullOrEmpty(id_solicitud);
}
