using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Servicios;

var builder = WebApplication.CreateBuilder(args.Where(a =>
    a is not "--initialize" and not "--create-admin" and not "--security-cleanup").ToArray());
builder.Logging.ClearProviders(); builder.Logging.AddConsole(); builder.Logging.AddDebug();

builder.Services.AddDbContext<ContextoBaseDatos>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Falta la cadena de conexión DefaultConnection.")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(o => {
    o.User.RequireUniqueEmail = true; o.Stores.MaxLengthForKeys = 128;
    o.Password.RequiredLength = 10;
    o.Password.RequireUppercase = true;
    o.Password.RequireLowercase = true;
    o.Password.RequireDigit = true;
    o.Password.RequireNonAlphanumeric = true;
    o.Password.RequiredUniqueChars = 4;
    o.Lockout.AllowedForNewUsers = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<ContextoBaseDatos>()
  .AddUserStore<AlmacenUsuarios>()
  .AddRoleStore<AlmacenRoles>()
  .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o => {
    o.Cookie.Name = ".MonteOracion.Auth";
    o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied";
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30); o.SlidingExpiration = true;
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));
builder.Services.AddHsts(o => {
    o.MaxAge = TimeSpan.FromDays(365);
    o.IncludeSubDomains = true;
    o.Preload = false;
});

builder.Services.AddControllersWithViews(o => {
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.Filters.Add<FiltroOperacionConcurrente>();
}).AddRazorOptions(r => {
    r.ViewLocationExpanders.Add(new ExpansorVistasEspanol());
});

builder.Services.AddScoped<IServicioDonaciones, ServicioDonaciones>();
builder.Services.AddScoped<ServicioAlmacen>();
builder.Services.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoArchivos>();
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("forms", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 15, Window = TimeSpan.FromMinutes(5), QueueLimit = 0, AutoReplenishment = true
        }));
});

var app = builder.Build();

try
{
    if (await ComandosBaseDatos.EjecutarAsync(app, args)) return;
}
catch (Exception ex) when (args.Contains("--initialize") || args.Contains("--create-admin") || args.Contains("--security-cleanup"))
{
    Console.Error.WriteLine("No se pudo completar el comando: " + ex.Message);
    Environment.ExitCode = 1;
    return;
}

try
{
    using var scopeInicio = app.Services.CreateScope();
    var dbInicio = scopeInicio.ServiceProvider.GetRequiredService<ContextoBaseDatos>();
    await ComandosBaseDatos.PrepararBaseDatosAsync(dbInicio, permitirCrear: false);
    app.Logger.LogInformation("Base de datos verificada y migraciones al día.");
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "No se pudo preparar la base de datos. Ejecuta 'dotnet run -- --initialize' o revisa DefaultConnection.");
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) => {
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-src https://www.google.com https://maps.google.com;";
    await next();
});

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health")
        && !context.Request.Path.StartsWithSegments("/Account/Denied")
        && !context.Request.Path.StartsWithSegments("/Home/Error"),
    branch => branch.UseStatusCodePagesWithReExecute("/no-encontrado", "?code={0}"));

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (context, next) => {
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var uid = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var baseDatos = context.RequestServices.GetRequiredService<ContextoBaseDatos>();
        if (!await baseDatos.PerfilesPersonal.AnyAsync(x => x.id_usuario == uid && x.activo) || !Roles.EsEquipo(context.User))
        {
            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context, IdentityConstants.ApplicationScheme);
            context.Response.Redirect("/Account/Login"); return;
        }
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
    }
    await next();
});
app.UseAuthorization();
app.MapStaticAssets();

app.MapGet("/health", async (ContextoBaseDatos db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { estado = "ok" })
            : Results.Json(new { estado = "error" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { estado = "error" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
