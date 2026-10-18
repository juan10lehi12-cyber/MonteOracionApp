using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Modules.Congregants;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Modules.Publications;
using MonteOracionApp.Modules.Settings;

namespace MonteOracionApp.Data;

// SQL names are centralized here; C# entities keep their existing names.
internal static class TableNames
{
    internal static void Configure(ModelBuilder model)
    {
        model.Entity<IdentityUser>().ToTable("tb_usuario");
        model.Entity<IdentityRole>().ToTable("tb_rol");
        model.Entity<IdentityUserRole<string>>().ToTable("tb_usuario_rol");
        model.Entity<IdentityUserClaim<string>>().ToTable("tb_usuario_atributo");
        model.Entity<IdentityRoleClaim<string>>().ToTable("tb_rol_atributo");
        model.Entity<IdentityUserLogin<string>>().ToTable("tb_usuario_acceso_externo");
        model.Entity<IdentityUserToken<string>>().ToTable("tb_usuario_token");
        model.Entity<Congregant>().ToTable("tb_congregante");
        model.Entity<Donation>().ToTable("tb_donacion");
        model.Entity<DonationItem>().ToTable("tb_donacion_bien");
        model.Entity<DonationDecision>().ToTable("tb_donacion_historial");
        model.Entity<Publication>().ToTable("tb_publicacion");
        model.Entity<PaymentMethod>().ToTable("tb_medio_pago");
        model.Entity<ChurchSettings>().ToTable("tb_iglesia");
    }
}
