using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Accounts;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Congregants;
public class CongregantService(ApplicationDbContext db, UserManager<IdentityUser> users)
{
    public async Task SetActiveAsync(int id, bool active)
    {
        var c = await db.Congregants.SingleAsync(x => x.Id == id);
        await using var transaction = await db.Database.BeginTransactionAsync();
        c.IsActive = active;
        if (c.UserId != null)
        {
            var user = await users.FindByIdAsync(c.UserId);
            if (user != null) Ensure(await users.UpdateSecurityStampAsync(user));
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
    public async Task CreateAccountAsync(CreateAccountForm form)
    {
        var c = await db.Congregants.SingleAsync(x => x.Id == form.CongregantId);
        if (!c.IsActive || c.UserId != null) throw new InvalidOperationException("El congregante debe estar activo y no tener una cuenta.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new IdentityUser { UserName = form.Email.Trim(), Email = form.Email.Trim(), EmailConfirmed = true };
        Ensure(await users.CreateAsync(user, form.Password));
        Ensure(await users.AddToRoleAsync(user, Roles.Congregant));
        c.UserId = user.Id; await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
    public async Task ResetPasswordAsync(ResetPasswordForm form)
    {
        var c = await db.Congregants.SingleAsync(x => x.Id == form.CongregantId);
        if (c.UserId == null) throw new InvalidOperationException("El congregante no tiene cuenta.");
        var user = await users.FindByIdAsync(c.UserId) ?? throw new InvalidOperationException("Cuenta no encontrada.");
        var token = await users.GeneratePasswordResetTokenAsync(user);
        Ensure(await users.ResetPasswordAsync(user, token, form.Password));
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }
}
