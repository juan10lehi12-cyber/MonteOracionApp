using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Data;
public static class DatabaseCommands
{
    public static async Task<bool> RunAsync(WebApplication app, string[] args)
    {
        if (!args.Contains("--initialize") && !args.Contains("--create-admin")) return false;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { Roles.Administrator, Roles.Treasurer, Roles.Warehouse })
            if (!await roles.RoleExistsAsync(name)) Ensure(await roles.CreateAsync(new IdentityRole(name)));
        if (args.Contains("--create-admin"))
        {
            Console.Write("Correo del administrador: "); var email = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)) throw new InvalidOperationException("Correo inválido.");
            Console.Write("Contraseña (mínimo 10 caracteres): ");
            var password = ReadPassword(); Console.WriteLine();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            if (await users.FindByEmailAsync(email) != null) throw new InvalidOperationException("Ese correo ya tiene una cuenta. No se modificó.");
            await using var transaction = await db.Database.BeginTransactionAsync();
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            Ensure(await users.CreateAsync(user, password)); Ensure(await users.AddToRoleAsync(user, Roles.Administrator));
            db.StaffProfiles.Add(new Modules.Staff.StaffProfile { UserId = user.Id, FullName = email[..Math.Min(160,email.Length)] });
            await db.SaveChangesAsync();
            await transaction.CommitAsync(); Console.WriteLine("Administrador creado.");
        }
        Console.WriteLine("Base de datos y roles listos."); return true;
    }
    private static string ReadPassword()
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var text = new System.Text.StringBuilder();
        while (true) { var key = Console.ReadKey(true); if (key.Key == ConsoleKey.Enter) return text.ToString(); if (key.Key == ConsoleKey.Backspace) { if (text.Length > 0) text.Length--; } else if (!char.IsControl(key.KeyChar)) text.Append(key.KeyChar); }
    }
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description))); }
}

