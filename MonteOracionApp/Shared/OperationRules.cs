using MonteOracionApp.Modules.Donations;
namespace MonteOracionApp.Shared;
public static class OperationRules
{
    public static void Quantity(decimal value) => DonationRules.ValidateAmount(value);
    public static void OtherApprover(string requester, string approver)
    { if (requester == approver) throw new InvalidOperationException("Otra persona debe aprobar tu solicitud."); }
    public static void Available(decimal available, decimal requested)
    { Quantity(requested); if (requested > available) throw new InvalidOperationException("El saldo disponible es insuficiente. Revisa las reservas existentes."); }
    public static void Reason(string? reason)
    { if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) throw new InvalidOperationException("Indica un motivo de hasta 1000 caracteres."); }
}
