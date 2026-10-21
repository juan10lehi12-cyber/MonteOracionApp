using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Inventory;
using MonteOracionApp.Modules.Treasury;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Shared;

var builder = WebApplication.CreateBuilder(args.Where(a => a is not "--initialize" and not "--create-admin").ToArray());
builder.Logging.ClearProviders(); builder.Logging.AddConsole(); builder.Logging.AddDebug();
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Falta DefaultConnection.")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(o => {
    o.User.RequireUniqueEmail = true; o.Stores.MaxLengthForKeys = 128;
    o.Password.RequiredLength = 10;
    o.Password.RequireUppercase = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireDigit = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o => {
    o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied";
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30); o.SlidingExpiration = true;
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.AddControllersWithViews(o => { o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); o.Filters.Add<ConcurrentOperationFilter>(); });
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<TreasuryService>();
builder.Services.AddSingleton<IFileStorage, FileStorage>();
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("consulta", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0, AutoReplenishment = true }));
    o.AddPolicy("forms", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions {
        PermitLimit = 15, Window = TimeSpan.FromMinutes(5), QueueLimit = 0, AutoReplenishment = true
    }));
});
var app = builder.Build();
try { if (await DatabaseCommands.RunAsync(app, args)) return; }
catch (Exception ex) when (args.Contains("--initialize") || args.Contains("--create-admin"))
{
    Console.Error.WriteLine("No se pudo preparar la base o la cuenta: " + ex.Message);
    Environment.ExitCode = 1;
    return;
}
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.Use(async (context, next) => {
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    await next();
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (context, next) => {
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var uid = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var database = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        if (!await database.StaffProfiles.AnyAsync(x => x.UserId == uid && x.IsActive) || !new[] { Roles.Administrator, Roles.Treasurer, Roles.Warehouse }.Any(context.User.IsInRole))
        {
            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context, IdentityConstants.ApplicationScheme);
            context.Response.Redirect("/Account/Login"); return;
        }
    }
    await next();
});
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();




