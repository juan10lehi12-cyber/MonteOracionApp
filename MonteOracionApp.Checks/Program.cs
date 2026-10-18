using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Shared;

var passed = 0;
void Check(string name, Action check) { check(); passed++; Console.WriteLine("OK " + name); }
void ExpectFailure(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new Exception("La operación debía rechazarse."); }
void Assert(bool condition) { if (!condition) throw new Exception("Verificación fallida."); }
var today = new DateTime(2026, 10, 11);
Check("No aceptar fechas futuras", () => ExpectFailure(() => DonationRules.ValidateDate(today.AddDays(1), today)));
Check("No aceptar fecha vacía", () => ExpectFailure(() => DonationRules.ValidateDate(default, today)));
Check("Aceptar fecha de recepción anterior", () => DonationRules.ValidateDate(today.AddDays(-1), today));
Check("No aceptar monto nulo", () => ExpectFailure(() => DonationRules.ValidateAmount(null)));
Check("No aceptar monto cero", () => ExpectFailure(() => DonationRules.ValidateAmount(0)));
Check("No aceptar monto negativo", () => ExpectFailure(() => DonationRules.ValidateAmount(-1)));
Check("No redondear silenciosamente importes", () => ExpectFailure(() => DonationRules.ValidateAmount(10.001m)));
Check("No desbordar precisión SQL", () => ExpectFailure(() => DonationRules.ValidateAmount(10000000000m)));
Check("Aceptar céntimos", () => DonationRules.ValidateAmount(0.01m));
Check("Aceptar monto máximo SQL", () => DonationRules.ValidateAmount(9999999999.99m));
Check("Aprobar pendiente", () => DonationRules.ValidateDecision(DonationStatus.Pendiente, DonationStatus.Aprobada, null));
Check("Rechazar con motivo", () => DonationRules.ValidateDecision(DonationStatus.Pendiente, DonationStatus.Rechazada, "No se recibió el pago."));
Check("Rechazo requiere motivo", () => ExpectFailure(() => DonationRules.ValidateDecision(DonationStatus.Pendiente, DonationStatus.Rechazada, " ")));
Check("Anulación requiere motivo", () => ExpectFailure(() => DonationRules.ValidateDecision(DonationStatus.Aprobada, DonationStatus.Anulada, null)));
Check("No aprobar dos veces", () => ExpectFailure(() => DonationRules.ValidateDecision(DonationStatus.Aprobada, DonationStatus.Aprobada, null)));
Check("No reabrir anulada", () => ExpectFailure(() => DonationRules.ValidateDecision(DonationStatus.Anulada, DonationStatus.Aprobada, null)));
Check("No reabrir rechazada", () => ExpectFailure(() => DonationRules.ValidateDecision(DonationStatus.Rechazada, DonationStatus.Pendiente, null)));
Check("No aceptar estado inventado", () => ExpectFailure(() => DonationRules.ValidateDecision(DonationStatus.Pendiente, (DonationStatus)99, null)));
Check("Motivo limitado", () => ExpectFailure(() => DonationRules.ValidateDecision(DonationStatus.Pendiente, DonationStatus.Rechazada, new string('a', 1001))));
Check("Todas las transiciones cerradas salvo cuatro", () => {
    var count = Enum.GetValues<DonationStatus>().Sum(a => Enum.GetValues<DonationStatus>().Count(b => DonationRules.CanTransition(a, b)));
    Assert(count == 4);
});
var storagePath = Path.Combine(AppContext.BaseDirectory, "file-checks", Guid.NewGuid().ToString("N"));
var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:Path"] = storagePath }).Build();
var storage = new FileStorage(new TestEnvironment(), configuration);
IFormFile Upload(byte[] bytes, string filename) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Receipt", filename);
var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jfXkAAAAASUVORK5CYII=");
Check("Rechazar ejecutable renombrado", () => ExpectFailure(() => storage.SaveAsync(Upload("MZfake"u8.ToArray(), "comprobante.png")).GetAwaiter().GetResult()));
Check("Rechazar extensión que no coincide", () => ExpectFailure(() => storage.SaveAsync(Upload(png, "comprobante.pdf")).GetAwaiter().GetResult()));
Check("Rechazar archivo vacío", () => ExpectFailure(() => storage.SaveAsync(Upload([], "comprobante.png")).GetAwaiter().GetResult()));
Check("Rechazar archivo mayor de 5MB", () => ExpectFailure(() => storage.SaveAsync(Upload(new byte[5 * 1024 * 1024 + 1], "comprobante.png")).GetAwaiter().GetResult()));
Check("Rechazar PNG incompleto", () => ExpectFailure(() => storage.SaveAsync(Upload(png[..24], "comprobante.png")).GetAwaiter().GetResult()));
Check("QR no admite PDF", () => ExpectFailure(() => storage.SaveAsync(Upload("%PDF-1.4\n%%EOF"u8.ToArray(), "qr.pdf"), true).GetAwaiter().GetResult()));
Check("Guardar imagen con nombre generado", () => {
    var stored = storage.SaveAsync(Upload(png, "../../comprobante.png")).GetAwaiter().GetResult();
    Assert(stored.Key.Length == 36 && stored.ContentType == "image/png" && !stored.Key.Contains("comprobante"));
    using (var content = storage.Open(stored.Key)) { Assert(content.Length == png.Length); }
    storage.Delete(stored.Key);
});
Check("No permitir escape de carpeta", () => ExpectFailure(() => storage.Open("../secret.txt")));
Console.WriteLine($"{passed} comprobaciones superadas.");

sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Checks";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = AppContext.BaseDirectory;
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
