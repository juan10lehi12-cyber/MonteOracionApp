using System.ComponentModel.DataAnnotations;
using MonteOracionApp.Modules.Congregants;
namespace MonteOracionApp.Modules.Publications;
public enum AttendanceStatus { Pendiente, Asistio, NoAsistio, Justificada }
public class EventParticipant
{
    public int Id { get; set; }
    public int PublicationId { get; set; }
    public Publication Publication { get; set; } = null!;
    public int CongregantId { get; set; }
    public Congregant Congregant { get; set; } = null!;
    public AttendanceStatus Attendance { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(1000)] public string? RemovalReason { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
    [MaxLength(1000)] public string? InternalNote { get; set; }
    [MaxLength(450)] public string RecordedBy { get; set; } = "";
}
