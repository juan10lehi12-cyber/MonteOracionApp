using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MonteOracionApp.Datos;

/// <summary>
/// Permite ejecutar las herramientas de EF Core (migraciones) sin arrancar
/// el pipeline web ni tocar la base de datos.
/// </summary>
public sealed class ContextoBaseDatosFactory : IDesignTimeDbContextFactory<ContextoBaseDatos>
{
    public ContextoBaseDatos CreateDbContext(string[] args)
    {
        var configuracion = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cadena = configuracion.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=MonteOracionApp;Trusted_Connection=True;TrustServerCertificate=True";

        return new ContextoBaseDatos(
            new DbContextOptionsBuilder<ContextoBaseDatos>()
                .UseSqlServer(cadena)
                .Options);
    }
}
