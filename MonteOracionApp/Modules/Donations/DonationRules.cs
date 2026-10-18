namespace MonteOracionApp.Modules.Donations;
// Pure business rules. No HTTP, files or database dependencies.
public static class DonationRules
{
    public static bool CanTransition(DonationStatus current, DonationStatus next) =>
        current == DonationStatus.Pendiente && next is DonationStatus.Aprobada or DonationStatus.Rechazada or DonationStatus.Anulada
        || current == DonationStatus.Aprobada && next == DonationStatus.Anulada;

    public static void ValidateDate(DateTime date, DateTime todayInLima)
    {
        if (date == default || date.Date > todayInLima.Date)
            throw new InvalidOperationException("La fecha debe ser válida y no puede estar en el futuro.");
    }
    public static void ValidateAmount(decimal? amount)
    {
        if (amount is null or <= 0 or > 9999999999.99m || decimal.Round(amount.Value, 2) != amount)
            throw new InvalidOperationException("Ingresa un monto positivo con máximo dos decimales.");
    }
    public static void ValidateDecision(DonationStatus current, DonationStatus next, string? reason)
    {
        if (!CanTransition(current, next)) throw new InvalidOperationException("El cambio de estado ya no está permitido. Actualiza la página.");
        if (next is DonationStatus.Rechazada or DonationStatus.Anulada && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Debes indicar el motivo.");
        if (reason?.Length > 1000) throw new InvalidOperationException("El motivo admite hasta 1000 caracteres.");
    }
}
