using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Modelos;
namespace MonteOracionApp.Datos;

public class ContextoBaseDatos(DbContextOptions<ContextoBaseDatos> opciones) : IdentityDbContext(opciones)
{
    public DbSet<Congregante> Congregantes => Set<Congregante>();
    public DbSet<Donacion> Donaciones => Set<Donacion>();
    public DbSet<Publicacion> Publicaciones => Set<Publicacion>();
    public DbSet<ConfiguracionIglesia> ConfiguracionesIglesia => Set<ConfiguracionIglesia>();
    public DbSet<MedioPago> MediosPago => Set<MedioPago>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<MovimientoAlmacen> MovimientosAlmacen => Set<MovimientoAlmacen>();
    public DbSet<PerfilPersonal> PerfilesPersonal => Set<PerfilPersonal>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Solo las clases internas de Microsoft Identity necesitan mapeo porque vienen en inglés en la librería de .NET
        b.Entity<IdentityUser>(e =>
        {
            e.ToTable("tb_usuario");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserName).HasColumnName("nombre_usuario");
            e.Property(x => x.NormalizedUserName).HasColumnName("nombre_usuario_normalizado");
            e.Property(x => x.Email).HasColumnName("correo");
            e.Property(x => x.NormalizedEmail).HasColumnName("correo_normalizado");
            e.Property(x => x.EmailConfirmed).HasColumnName("correo_confirmado");
            e.Property(x => x.PasswordHash).HasColumnName("clave_hash");
            e.Property(x => x.SecurityStamp).HasColumnName("sello_seguridad");
            e.Property(x => x.ConcurrencyStamp).HasColumnName("sello_concurrencia");
            e.Property(x => x.PhoneNumber).HasColumnName("telefono");
            e.Property(x => x.PhoneNumberConfirmed).HasColumnName("telefono_confirmado");
            e.Property(x => x.TwoFactorEnabled).HasColumnName("doble_factor_activo");
            e.Property(x => x.LockoutEnd).HasColumnName("fin_bloqueo");
            e.Property(x => x.LockoutEnabled).HasColumnName("bloqueo_activo");
            e.Property(x => x.AccessFailedCount).HasColumnName("intentos_fallidos");
        });

        b.Entity<IdentityRole>(e =>
        {
            e.ToTable("tb_rol");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("nombre");
            e.Property(x => x.NormalizedName).HasColumnName("nombre_normalizado");
            e.Property(x => x.ConcurrencyStamp).HasColumnName("sello_concurrencia");
        });

        b.Entity<IdentityUserRole<string>>(e => { e.ToTable("tb_usuario_rol"); e.Property(x => x.UserId).HasColumnName("id_usuario"); e.Property(x => x.RoleId).HasColumnName("id_rol"); });
        b.Entity<IdentityUserClaim<string>>(e => { e.ToTable("tb_usuario_atributo"); e.Property(x => x.Id).HasColumnName("id"); e.Property(x => x.UserId).HasColumnName("id_usuario"); e.Property(x => x.ClaimType).HasColumnName("tipo_atributo"); e.Property(x => x.ClaimValue).HasColumnName("valor_atributo"); });
        b.Entity<IdentityRoleClaim<string>>(e => { e.ToTable("tb_rol_atributo"); e.Property(x => x.Id).HasColumnName("id"); e.Property(x => x.RoleId).HasColumnName("id_rol"); e.Property(x => x.ClaimType).HasColumnName("tipo_atributo"); e.Property(x => x.ClaimValue).HasColumnName("valor_atributo"); });
        b.Entity<IdentityUserLogin<string>>(e => { e.ToTable("tb_usuario_acceso_externo"); e.Property(x => x.LoginProvider).HasColumnName("proveedor_acceso").HasMaxLength(128); e.Property(x => x.ProviderKey).HasColumnName("clave_proveedor").HasMaxLength(128); e.Property(x => x.ProviderDisplayName).HasColumnName("nombre_visible_proveedor"); e.Property(x => x.UserId).HasColumnName("id_usuario"); });
        b.Entity<IdentityUserToken<string>>(e => { e.ToTable("tb_usuario_token"); e.Property(x => x.UserId).HasColumnName("id_usuario"); e.Property(x => x.LoginProvider).HasColumnName("proveedor_acceso").HasMaxLength(128); e.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(128); e.Property(x => x.Value).HasColumnName("valor"); });

        // Relaciones y precisión decimal de nuestros modelos en español
        b.Entity<PerfilPersonal>().HasOne(x => x.usuario).WithOne().HasForeignKey<PerfilPersonal>(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Congregante>().HasOne(x => x.usuario).WithOne().HasForeignKey<Congregante>(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donacion>().HasOne(x => x.evento).WithMany().HasForeignKey(x => x.id_evento).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donacion>().HasOne(x => x.congregante).WithMany().HasForeignKey(x => x.id_congregante).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donacion>().HasOne(x => x.medio_pago).WithMany().HasForeignKey(x => x.id_medio_pago).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DonacionBien>().HasOne(x => x.donacion).WithMany(x => x.bienes).HasForeignKey(x => x.id_donacion).OnDelete(DeleteBehavior.Cascade);
        b.Entity<DonacionHistorial>().HasOne(x => x.donacion).WithMany(x => x.decisiones).HasForeignKey(x => x.id_donacion).OnDelete(DeleteBehavior.Cascade);
        b.Entity<DonacionHistorial>().HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.id_administrador).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Lote>().HasOne(x => x.donacion_bien).WithMany().HasForeignKey(x => x.id_donacion_bien).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Lote>().HasOne(x => x.producto).WithMany().HasForeignKey(x => x.id_producto).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MovimientoAlmacen>().HasOne(x => x.lote).WithMany().HasForeignKey(x => x.id_lote).OnDelete(DeleteBehavior.Restrict);

        foreach (var entidad in b.Model.GetEntityTypes())
            foreach (var propiedad in entidad.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            { propiedad.SetPrecision(12); propiedad.SetScale(2); }
    }
}
