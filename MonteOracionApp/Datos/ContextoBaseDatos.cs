using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Modelos;
namespace MonteOracionApp.Datos;

public class AlmacenUsuarios(ContextoBaseDatos context, IdentityErrorDescriber? describer = null)
    : UserStore<IdentityUser, IdentityRole, ContextoBaseDatos>(context, describer)
{
    public override Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken cancellationToken = default) =>
        Task.FromResult<IList<Claim>>([]);

    public override Task AddClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public override Task ReplaceClaimAsync(IdentityUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public override Task RemoveClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public override Task<IList<IdentityUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default) =>
        Task.FromResult<IList<IdentityUser>>([]);

    public override Task AddLoginAsync(IdentityUser user, UserLoginInfo login, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public override Task RemoveLoginAsync(IdentityUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public override Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user, CancellationToken cancellationToken = default) =>
        Task.FromResult<IList<UserLoginInfo>>([]);

    public override Task<IdentityUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IdentityUser?>(null);

    protected override Task<IdentityUserLogin<string>?> FindUserLoginAsync(string userId, string loginProvider, string providerKey, CancellationToken cancellationToken) =>
        Task.FromResult<IdentityUserLogin<string>?>(null);

    protected override Task<IdentityUserLogin<string>?> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken) =>
        Task.FromResult<IdentityUserLogin<string>?>(null);

    protected override Task<IdentityUserToken<string>?> FindTokenAsync(IdentityUser user, string loginProvider, string name, CancellationToken cancellationToken) =>
        Task.FromResult<IdentityUserToken<string>?>(null);

    protected override Task AddUserTokenAsync(IdentityUserToken<string> token) =>
        Task.CompletedTask;

    protected override Task RemoveUserTokenAsync(IdentityUserToken<string> token) =>
        Task.CompletedTask;
}

public class AlmacenRoles(ContextoBaseDatos context, IdentityErrorDescriber? describer = null)
    : RoleStore<IdentityRole, ContextoBaseDatos>(context, describer)
{
    public override Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult<IList<Claim>>([]);

    public override Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public override Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

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
    public DbSet<MensajeContacto> MensajesContacto => Set<MensajeContacto>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Mapeo de las tablas de seguridad (Identity) a español snake_case
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
        b.Ignore<IdentityUserClaim<string>>();
        b.Ignore<IdentityRoleClaim<string>>();
        b.Ignore<IdentityUserLogin<string>>();
        b.Ignore<IdentityUserToken<string>>();

        // Relaciones completas (100% de tablas conectadas mediante Foreign Keys) y Restricciones CHECK
        b.Entity<PerfilPersonal>(e =>
        {
            e.HasOne(x => x.usuario).WithOne().HasForeignKey<PerfilPersonal>(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("tb_personal", t => t.HasCheckConstraint("CK_tb_personal_nombre_no_vacio", "LEN(LTRIM(RTRIM([nombre_completo]))) >= 3"));
        });

        b.Entity<Congregante>(e =>
        {
            e.HasOne(x => x.usuario).WithOne().HasForeignKey<Congregante>(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.dni).IsUnique().HasFilter("[dni] IS NOT NULL").HasDatabaseName("IX_tb_congregante_dni");
            e.HasIndex(x => x.id_usuario).IsUnique().HasFilter("[id_usuario] IS NOT NULL").HasDatabaseName("IX_tb_congregante_id_usuario");
            e.ToTable("tb_congregante", t =>
            {
                t.HasCheckConstraint("CK_tb_congregante_dni", "[dni] IS NULL OR [dni] LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
                t.HasCheckConstraint("CK_tb_congregante_telefono", "[telefono] IS NULL OR [telefono] LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
            });
        });

        b.Entity<ConfiguracionIglesia>(e =>
        {
            e.HasOne(x => x.usuario).WithMany().HasForeignKey(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("tb_iglesia", t => t.HasCheckConstraint("CK_ChurchSettings_Singleton", "[id] = 1"));
        });

        b.Entity<MedioPago>(e =>
        {
            e.HasOne(x => x.usuario).WithMany().HasForeignKey(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Publicacion>(e =>
        {
            e.HasOne(x => x.usuario).WithMany().HasForeignKey(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("tb_publicacion", t =>
            {
                t.HasCheckConstraint("CK_tb_publicacion_tipo", "[tipo] IN (0, 1)");
                t.HasCheckConstraint("CK_tb_publicacion_meta", "[meta_recaudacion] IS NULL OR [meta_recaudacion] > 0");
                t.HasCheckConstraint("CK_tb_publicacion_fechas", "[fecha_fin_evento] IS NULL OR [fecha_inicio_evento] IS NULL OR [fecha_fin_evento] >= [fecha_inicio_evento]");
            });
        });

        b.Entity<Donacion>(e =>
        {
            e.HasOne(x => x.evento).WithMany().HasForeignKey(x => x.id_evento).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.congregante).WithMany().HasForeignKey(x => x.id_congregante).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.medio_pago).WithMany().HasForeignKey(x => x.id_medio_pago).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.usuario).WithMany().HasForeignKey(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.codigo_seguimiento).IsUnique().HasDatabaseName("IX_tb_donacion_codigo_seguimiento");
            e.HasIndex(x => new { x.id_medio_pago, x.numero_operacion })
                .IsUnique()
                .HasFilter("[numero_operacion] IS NOT NULL AND [id_medio_pago] IS NOT NULL")
                .HasDatabaseName("IX_tb_donacion_operacion_unica");
            e.HasIndex(x => new { x.estado, x.fecha_creacion }).HasDatabaseName("IX_tb_donacion_estado_fecha");
            e.ToTable("tb_donacion", t =>
            {
                t.HasCheckConstraint("CK_tb_donacion_monto", "[monto] IS NULL OR [monto] > 0");
                t.HasCheckConstraint("CK_tb_donacion_estado", "[estado] IN (0, 1, 2, 3)");
            });
        });

        b.Entity<DonacionBien>(e =>
        {
            e.HasOne(x => x.donacion).WithMany(x => x.bienes).HasForeignKey(x => x.id_donacion).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.producto).WithMany().HasForeignKey(x => x.id_producto).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("tb_donacion_bien", t => t.HasCheckConstraint("CK_DonationItem_Quantity", "[cantidad] > 0"));
        });

        b.Entity<DonacionHistorial>(e =>
        {
            e.HasOne(x => x.donacion).WithMany(x => x.decisiones).HasForeignKey(x => x.id_donacion).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.administrador).WithMany().HasForeignKey(x => x.id_administrador).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("tb_donacion_historial", t => t.HasCheckConstraint("CK_tb_donacion_historial_estado", "[estado] IN (0, 1, 2, 3)"));
        });

        b.Entity<Lote>(e =>
        {
            e.HasOne(x => x.donacion_bien).WithMany().HasForeignKey(x => x.id_donacion_bien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.producto).WithMany().HasForeignKey(x => x.id_producto).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.usuario_pendiente).WithMany().HasForeignKey(x => x.id_usuario_pendiente).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.usuario_confirmacion).WithMany().HasForeignKey(x => x.confirmado_por).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.id_donacion_bien).IsUnique().HasFilter("[id_donacion_bien] IS NOT NULL").HasDatabaseName("IX_tb_lote_id_donacion_bien");
            e.ToTable("tb_lote", t => t.HasCheckConstraint("CK_Lote_Saldos", "[cantidad_actual] >= 0"));
        });

        b.Entity<MovimientoAlmacen>(e =>
        {
            e.HasOne(x => x.lote).WithMany().HasForeignKey(x => x.id_lote).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.usuario).WithMany().HasForeignKey(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.fecha_operacion).HasDatabaseName("IX_tb_movimiento_almacen_fecha_operacion");
            e.ToTable("tb_movimiento_almacen", t =>
            {
                t.HasCheckConstraint("CK_tb_movimiento_almacen_cantidad", "[cantidad] <> 0");
                t.HasCheckConstraint("CK_tb_movimiento_almacen_tipo", "[tipo] IN (0, 3, 9)");
            });
        });

        b.Entity<MensajeContacto>(e =>
        {
            e.HasOne(x => x.congregante).WithMany().HasForeignKey(x => x.id_congregante).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.usuario).WithMany().HasForeignKey(x => x.id_usuario).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.estado, x.fecha_creacion }).HasDatabaseName("IX_tb_mensaje_contacto_estado_fecha");
            e.ToTable("tb_mensaje_contacto", t =>
            {
                t.HasCheckConstraint("CK_tb_mensaje_contacto_motivo", "[motivo] IN (0, 1, 2, 3, 4)");
                t.HasCheckConstraint("CK_tb_mensaje_contacto_preferencia", "[preferencia] IN (0, 1, 2)");
                t.HasCheckConstraint("CK_tb_mensaje_contacto_estado", "[estado] IN (0, 1, 2)");
                t.HasCheckConstraint("CK_tb_mensaje_contacto_longitud", "LEN(LTRIM(RTRIM([mensaje]))) >= 10");
            });
        });

        b.Entity<Producto>(e =>
        {
            e.HasIndex(x => new { x.nombre, x.unidad }).IsUnique().HasDatabaseName("IX_tb_producto_nombre_unidad");
            e.ToTable("tb_producto", t => t.HasCheckConstraint("CK_tb_producto_nombre", "LEN(LTRIM(RTRIM([nombre]))) >= 2"));
        });

        foreach (var entidad in b.Model.GetEntityTypes())
            foreach (var propiedad in entidad.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            { propiedad.SetPrecision(12); propiedad.SetScale(2); }
    }
}
