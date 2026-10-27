using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;

Console.WriteLine("Ejecutando verificación funcional de Monte de Oración...");

var dbName = "MonteOracionTest_" + Guid.NewGuid().ToString("N")[..8];
var connStr = $"Server=DESKTOP-PP9ER99;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
var opts = new DbContextOptionsBuilder<ContextoBaseDatos>().UseSqlServer(connStr).Options;
await using var db = new ContextoBaseDatos(opts);
await db.Database.EnsureCreatedAsync();

try
{
    var user = new IdentityUser { Id = "staff-1", UserName = "staff@iglesia.pe", Email = "staff@iglesia.pe" };
    db.Users.Add(user);
    db.PerfilesPersonal.Add(new PerfilPersonal { id_usuario = user.Id, nombre_completo = "Operador Prueba", cargo = "Tesorero" });
    var prod = new Producto { nombre = "Arroz", unidad = "kg" };
    db.Productos.Add(prod);
    var medio = new MedioPago { nombre = "Yape", activo = true };
    db.MediosPago.Add(medio);
    await db.SaveChangesAsync();

    var storage = new AlmacenamientoMemoria();
    var warehouse = new ServicioAlmacen(db);
    var donations = new ServicioDonaciones(db, storage, warehouse);

    byte[] jpg = [255, 216, 255, 0, 255, 217];
    await using var msFoto = new MemoryStream(jpg);
    var fotoBien = new FormFile(msFoto, 0, jpg.Length, "Photo", "bien.jpg")
    {
        Headers = new HeaderDictionary(),
        ContentType = "image/jpeg"
    };

    // 1. Donación en bienes -> recepción -> confirmación de entrada -> salida
    var donacionBienes = await donations.RegistrarRecepcionAsync(new FormularioRecepcionDonacion
    {
        Kind = TipoDonacion.Bienes,
        DonorKind = TipoDonante.Anonimo,
        ReceivedOn = HoraIglesia.Hoy,
        Items = [new ItemRecepcionForm { ProductId = prod.id, Quantity = 15 }]
    }, user.Id);

    var lot = await db.Lotes.SingleAsync();
    Assert(lot.cantidad_actual == 15m && lot.revision_pendiente, "La recepción de bienes debe crear lote pendiente con 15 kg");

    await warehouse.ConfirmarEntradaAsync(lot.id, "Estante A1", user.Id, Guid.NewGuid());
    await db.Entry(lot).ReloadAsync();
    Assert(!lot.revision_pendiente && lot.ubicacion == "Estante A1", "La confirmación debe activar el lote en Estante A1");

    await warehouse.RegistrarSalidaAsync(new FormularioSalidaAlmacen
    {
        id_comando = Guid.NewGuid(),
        destino = "Comedor parroquial",
        lineas = [new LineaSalidaAlmacen { id_lote = lot.id, cantidad = 6 }]
    }, user.Id);
    await db.Entry(lot).ReloadAsync();
    Assert(lot.cantidad_actual == 9m, "La salida de almacén debe descontar a 9 kg");

    // 2. Donación monetaria con voucher -> validación
    await using var msVoucher = new MemoryStream(jpg);
    var formFile = new FormFile(msVoucher, 0, jpg.Length, "Voucher", "voucher.jpg")
    {
        Headers = new HeaderDictionary(),
        ContentType = "image/jpeg"
    };

    var donacionDinero = await donations.RegistrarWebAsync(new FormularioDonacionWeb
    {
        anonimo = true,
        monto = 150m,
        id_medio_pago = medio.id,
        numero_operacion = "OP-12345",
        fecha_pago = HoraIglesia.Hoy,
        comprobante = formFile
    }, null);

    await donations.DecidirAsync(donacionDinero.id, EstadoDonacion.Aprobada, null, user.Id, donacionDinero.version);
    await db.Entry(donacionDinero).ReloadAsync();
    Assert(donacionDinero.estado == EstadoDonacion.Aprobada && donacionDinero.monto == 150m, "Donación aprobada por S/ 150");

    Console.WriteLine("TODAS LAS PRUEBAS PASARON.");
}
finally
{
    await db.Database.EnsureDeletedAsync();
}

static void Assert(bool cond, string msg)
{
    if (!cond) throw new InvalidOperationException("Verificación falló: " + msg);
    Console.WriteLine("  [OK] " + msg);
}

sealed class AlmacenamientoMemoria : IAlmacenamientoArchivos
{
    public Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, bool soloImagen = false)
        => Task.FromResult(new ArchivoGuardado(Guid.NewGuid().ToString("N") + ".jpg", "image/jpeg"));
    public Stream Abrir(string clave) => new MemoryStream();
    public void Eliminar(string clave) { }
}
