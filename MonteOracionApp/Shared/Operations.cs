using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Audit;
namespace MonteOracionApp.Shared;
public static class Operations
{
    public static void Audit(this ApplicationDbContext db, string actor, string action, string reference, string detail = "") =>
        db.AuditEntries.Add(new AuditEntry { ActorId = actor, Action = action, Reference = reference, Detail = detail });
    public static async Task SaveOperationAsync(this ApplicationDbContext db)
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Otra persona modificó el registro. Actualiza y vuelve a intentar."); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { throw new InvalidOperationException("La operación ya fue registrada o el dato está duplicado. Actualiza la página."); }
    }
}
