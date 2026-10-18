using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Modules.Congregants;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Modules.Publications;
using MonteOracionApp.Modules.Settings;
namespace MonteOracionApp.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Congregant> Congregants => Set<Congregant>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<ChurchSettings> ChurchSettings => Set<ChurchSettings>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        TableNames.Configure(b); b.Entity<ChurchSettings>().Property(x => x.Id).ValueGeneratedNever(); b.Entity<ChurchSettings>().ToTable(t => t.HasCheckConstraint("CK_ChurchSettings_Singleton", "[Id] = 1"));
        b.Entity<Congregant>().Ignore(x => x.FullName);
        b.Entity<Congregant>().HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        b.Entity<Congregant>().HasOne(x => x.User).WithOne().HasForeignKey<Congregant>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Donation>().Ignore(x => x.DonorLabel);
        b.Entity<Donation>().HasIndex(x => x.TrackingCode).IsUnique();
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
    }
}


