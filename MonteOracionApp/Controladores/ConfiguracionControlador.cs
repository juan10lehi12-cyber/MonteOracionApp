using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.Administrador)]
public class SettingsController(ContextoBaseDatos db, IAlmacenamientoArchivos almacenamiento) : Controller
{
    [HttpGet] public async Task<IActionResult> Index() => View(await db.ConfiguracionesIglesia.SingleOrDefaultAsync() ?? new ConfiguracionIglesia());

    [HttpPost]
    public async Task<IActionResult> Index([Bind("nombre,informacion,direccion,contacto,horarios")] ConfiguracionIglesia formulario)
    {
        if (!ModelState.IsValid) return View(formulario);
        var s = await db.ConfiguracionesIglesia.SingleOrDefaultAsync(); if (s == null) { s = new(); db.ConfiguracionesIglesia.Add(s); }
        s.nombre = formulario.nombre.Trim(); s.informacion = formulario.informacion.Trim(); s.direccion = formulario.direccion?.Trim(); s.contacto = formulario.contacto?.Trim(); s.horarios = formulario.horarios?.Trim();
        await db.SaveChangesAsync(); TempData["Success"] = "Información actualizada."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Payments() => View(await db.MediosPago.AsNoTracking().OrderBy(x => x.nombre).ToListAsync());

    [HttpGet] public async Task<IActionResult> Payment(int? id) { if (id == null) return View(new MedioPago()); var p = await db.MediosPago.FindAsync(id); return p == null ? NotFound() : View(p); }

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Payment(int id, [Bind("nombre,instrucciones,numero_cuenta,activo")] MedioPago formulario, IFormFile? qr)
    {
        formulario.id = id; if (!ModelState.IsValid) return View(formulario);
        var p = id == 0 ? new MedioPago() : await db.MediosPago.FindAsync(id); if (p == null) return NotFound();
        ArchivoGuardado? subido = null; var anterior = p.clave_qr;
        if (qr != null)
        {
            try { subido = await almacenamiento.GuardarAsync(qr, soloImagen: true); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(formulario); }
            p.clave_qr = subido.clave; p.tipo_contenido_qr = subido.tipo_contenido;
        }
        p.nombre = formulario.nombre.Trim(); p.instrucciones = formulario.instrucciones.Trim(); p.numero_cuenta = formulario.numero_cuenta?.Trim(); p.activo = formulario.activo;
        if (id == 0) db.MediosPago.Add(p);
        try { await db.SaveChangesAsync(); } catch { if (subido != null) almacenamiento.Eliminar(subido.clave); throw; }
        if (subido != null && anterior != null) almacenamiento.Eliminar(anterior);
        TempData["Success"] = "Medio de pago guardado."; return RedirectToAction(nameof(Payments));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Qr(int id)
    {
        var p = await db.MediosPago.AsNoTracking().SingleOrDefaultAsync(x => x.id == id && x.activo);
        if (p?.clave_qr == null) return NotFound();
        Response.Headers.CacheControl = "no-store"; return File(almacenamiento.Abrir(p.clave_qr), p.tipo_contenido_qr!);
    }
}
