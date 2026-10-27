using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Datos;

public static class ComandosBaseDatos
{
    public static async Task<bool> EjecutarAsync(WebApplication app, string[] args)
    {
        if (!args.Contains("--initialize") && !args.Contains("--create-admin")) return false;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContextoBaseDatos>();
        await db.Database.EnsureCreatedAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var nombre in new[] { Roles.Administrador, Roles.Personal })
            if (!await roles.RoleExistsAsync(nombre)) Asegurar(await roles.CreateAsync(new IdentityRole(nombre)));
        if (args.Contains("--create-admin"))
        {
            Console.Write("Correo del administrador: "); var correo = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(correo) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(correo)) throw new InvalidOperationException("Correo inválido.");
            Console.Write("Contraseña (mínimo 10 caracteres): ");
            var clave = LeerClave(); Console.WriteLine();
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            if (await usuarios.FindByEmailAsync(correo) != null) throw new InvalidOperationException("Ese correo ya tiene una cuenta. No se modificó.");
            await using var transaccion = await db.Database.BeginTransactionAsync();
            var usuario = new IdentityUser { UserName = correo, Email = correo, EmailConfirmed = true };
            Asegurar(await usuarios.CreateAsync(usuario, clave)); Asegurar(await usuarios.AddToRoleAsync(usuario, Roles.Administrador));
            db.PerfilesPersonal.Add(new PerfilPersonal { id_usuario = usuario.Id, nombre_completo = correo[..Math.Min(160, correo.Length)] });
            await db.SaveChangesAsync();
            await transaccion.CommitAsync(); Console.WriteLine("Administrador creado.");
        }
        Console.WriteLine("Base de datos y roles listos."); return true;
    }

    private static string LeerClave()
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var texto = new System.Text.StringBuilder();
        while (true) { var tecla = Console.ReadKey(true); if (tecla.Key == ConsoleKey.Enter) return texto.ToString(); if (tecla.Key == ConsoleKey.Backspace) { if (texto.Length > 0) texto.Length--; } else if (!char.IsControl(tecla.KeyChar)) texto.Append(tecla.KeyChar); }
    }

    private static void Asegurar(IdentityResult resultado) { if (!resultado.Succeeded) throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(x => x.Description))); }
}
