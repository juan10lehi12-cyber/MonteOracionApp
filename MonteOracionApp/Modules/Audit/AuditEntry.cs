using System.ComponentModel.DataAnnotations;
namespace MonteOracionApp.Modules.Audit;
public class AuditEntry
{
    public long Id { get; set; }
    [MaxLength(450)] public string ActorId { get; set; } = "";
    [MaxLength(100)] public string Action { get; set; } = "";
    [MaxLength(100)] public string Reference { get; set; } = "";
    [MaxLength(2000)] public string Detail { get; set; } = "";
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
