using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Modules.Congregants;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Modules.Publications;
using MonteOracionApp.Modules.Settings;
using MonteOracionApp.Modules.Inventory;
using MonteOracionApp.Modules.Treasury;
using MonteOracionApp.Modules.Staff;
using MonteOracionApp.Modules.Audit;
namespace MonteOracionApp.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Congregant> Congregants => Set<Congregant>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<ChurchSettings> ChurchSettings => Set<ChurchSettings>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<StockRequest> StockRequests => Set<StockRequest>();
    public DbSet<StockRequestLine> StockRequestLines => Set<StockRequestLine>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<FinanceRequest> FinanceRequests => Set<FinanceRequest>();
    public DbSet<MoneyMovement> MoneyMovements => Set<MoneyMovement>();
    public DbSet<CashClosing> CashClosings => Set<CashClosing>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        // Keep the design-time and standalone verification models identical to Identity's configured key lengths.
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);
        TableNames.Configure(b); b.Entity<ChurchSettings>().Property(x => x.Id).ValueGeneratedNever(); b.Entity<ChurchSettings>().ToTable(t => t.HasCheckConstraint("CK_ChurchSettings_Singleton", "[Id] = 1"));
        b.Entity<Congregant>().Ignore(x => x.FullName);
        b.Entity<Congregant>().HasIndex(x => x.Dni).IsUnique().HasFilter("[Dni] IS NOT NULL");
        b.Entity<Congregant>().HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        b.Entity<Congregant>().HasOne(x => x.User).WithOne().HasForeignKey<Congregant>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donation>().Ignore(x => x.DonorLabel);
        b.Entity<Donation>().HasIndex(x => x.TrackingCode).IsUnique();
        b.Entity<Donation>().HasIndex(x => new { x.PaymentMethodId, x.ReceivedOrPaidOn, x.OperationNumber }).IsUnique().HasFilter("[OperationNumber] IS NOT NULL");
        b.Entity<Donation>().HasOne(x => x.Fund).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donation>().HasIndex(x => new { x.Status, x.CreatedAt });
        b.Entity<Donation>().HasOne(x => x.Congregant).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donation>().HasOne(x => x.PaymentMethod).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donation>().Property(x => x.Amount).HasPrecision(12, 2);
        b.Entity<Donation>().ToTable(t => {
            t.HasCheckConstraint("CK_Donation_Amount", "([Kind] = 0 AND [Amount] > 0 AND [Amount] IS NOT NULL) OR ([Kind] = 1 AND [Amount] IS NULL)");
            t.HasCheckConstraint("CK_Donation_Donor", "([DonorKind] = 0 AND [CongregantId] IS NOT NULL) OR ([DonorKind] = 1 AND [CongregantId] IS NULL AND [ExternalName] IS NOT NULL) OR ([DonorKind] = 2 AND [CongregantId] IS NULL AND [ExternalName] IS NULL)");
        });
        b.Entity<DonationItem>().Property(x => x.Quantity).HasPrecision(12, 2);
        b.Entity<DonationItem>().ToTable(t => t.HasCheckConstraint("CK_DonationItem_Quantity", "[Quantity] > 0"));
        b.Entity<DonationDecision>().HasOne<Microsoft.AspNetCore.Identity.IdentityUser>().WithMany().HasForeignKey(x => x.AdministratorId).OnDelete(DeleteBehavior.Restrict);
        ConfigureOperations(b);
    }
    private static void ConfigureOperations(ModelBuilder b)
    {
        b.Entity<StaffProfile>().ToTable("tb_personal").HasOne(x => x.User).WithOne().HasForeignKey<StaffProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditEntry>().ToTable("tb_auditoria").HasIndex(x => x.At);
        b.Entity<Product>().ToTable("tb_producto").HasIndex(x => new { x.Name, x.Unit }).IsUnique();
        b.Entity<StockLot>().ToTable("tb_lote", t => t.HasCheckConstraint("CK_Lote_Saldos", "[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]"));
        b.Entity<StockLot>().HasIndex(x => x.DonationItemId).IsUnique().HasFilter("[DonationItemId] IS NOT NULL");
        b.Entity<StockLot>().HasOne(x => x.DonationItem).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockLot>().HasOne(x => x.Product).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Beneficiary>().ToTable("tb_beneficiario");
        b.Entity<StockRequest>().ToTable("tb_solicitud_bienes").HasOne(x => x.Beneficiary).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockRequestLine>().ToTable("tb_solicitud_bien", t => t.HasCheckConstraint("CK_SolicitudBien_Saldos", "[Quantity] > 0 AND [Delivered] >= 0 AND [Delivered] <= [Quantity] AND [Returned] >= 0 AND [WrittenOff] >= 0 AND [Returned] + [WrittenOff] <= [Delivered]"));
        b.Entity<StockRequestLine>().HasOne(x => x.StockLot).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockRequestLine>().HasIndex(x => new { x.StockRequestId, x.StockLotId }).IsUnique();
        b.Entity<StockMovement>().ToTable("tb_movimiento_almacen").HasOne(x => x.StockLot).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>().HasOne(x => x.StockRequestLine).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>().HasIndex(x => x.BusinessDate);
        b.Entity<StockMovement>().HasIndex(x => x.CommandId).IsUnique().HasFilter("[CommandId] IS NOT NULL");
        b.Entity<Fund>().ToTable("tb_fondo");
        b.Entity<Fund>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Fund>().HasData(new Fund { Id = 1, Name = "General", Purpose = "Obra y necesidades de la iglesia", IsActive = true });
        b.Entity<FinanceRequest>().ToTable("tb_solicitud_dinero").HasOne(x => x.Fund).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<FinanceRequest>().HasIndex(x => new { x.FundId, x.Channel, x.Status });
        b.Entity<MoneyMovement>().ToTable("tb_movimiento_dinero").HasOne(x => x.Fund).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<MoneyMovement>().HasOne(x => x.Donation).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<MoneyMovement>().HasOne(x => x.FinanceRequest).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<MoneyMovement>().HasIndex(x => new { x.FundId, x.Channel, x.BusinessDate });
        b.Entity<MoneyMovement>().HasIndex(x => new { x.DonationId, x.Kind }).IsUnique().HasFilter("[DonationId] IS NOT NULL");
        b.Entity<CashClosing>().ToTable("tb_cierre_caja").HasIndex(x => new { x.Day, x.Channel }).IsUnique();
        b.Entity<EventParticipant>().ToTable("tb_participante_evento").HasIndex(x => new { x.PublicationId, x.CongregantId }).IsUnique();
        b.Entity<EventParticipant>().Property(x => x.IsActive).HasDefaultValue(true);
        b.Entity<EventParticipant>().HasOne(x => x.Publication).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventParticipant>().HasOne(x => x.Congregant).WithMany().OnDelete(DeleteBehavior.Restrict);
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            { property.SetPrecision(12); property.SetScale(2); }
    }
}


