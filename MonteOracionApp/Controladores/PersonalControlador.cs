using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

[Authorize(Roles = Roles.Administrador)]
public class StaffController(ContextoBaseDatos db, UserManager<IdentityUser> usuarios) : Controller
{
    public async Task<IActionResult> Index()
    {
        var membresias = await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id select new { ur.UserId, role.Name }).ToListAsync();
        ViewBag.UserRoles = membresias.GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => (IList<string>)x.Select(r => r.Name ?? "").ToList());
        return View(await db.PerfilesPersonal.Include(x => x.usuario).OrderBy(x => x.nombre_completo).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null) return View(new FormularioPersonal());
        var p = await db.PerfilesPersonal.Include(x => x.usuario).SingleOrDefaultAsync(x => x.id_usuario == id);
        if (p == null) return NotFound();
        return View(new FormularioPersonal
        {
            id_usuario = id, nombre_completo = p.nombre_completo, cargo = p.cargo, correo = p.usuario.Email!, activo = p.activo,
            administrador = await usuarios.IsInRoleAsync(p.usuario, Roles.Administrador), personal = await usuarios.IsInRoleAsync(p.usuario, Roles.Personal)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(FormularioPersonal formulario)
    {
        if (!formulario.administrador && !formulario.personal) ModelState.AddModelError("", "Selecciona al menos un rol.");
        if (formulario.id_usuario == null && string.IsNullOrWhiteSpace(formulario.clave)) ModelState.AddModelError("clave", "La contraseña inicial es obligatoria.");
        if (formulario.id_usuario == User.FindFirstValue(ClaimTypes.NameIdentifier) && (!formulario.activo || !formulario.administrador)) ModelState.AddModelError("", "Conserva tu acceso de administrador; otro administrador puede cambiarlo.");
        if (!ModelState.IsValid) return View(formulario);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            IdentityUser usuario;
            PerfilPersonal perfil;
            if (formulario.id_usuario == null)
            {
                usuario = new IdentityUser { UserName = formulario.correo.Trim(), Email = formulario.correo.Trim(), EmailConfirmed = true };
                Asegurar(await usuarios.CreateAsync(usuario, formulario.clave!));
                perfil = new PerfilPersonal { id_usuario = usuario.Id }; db.PerfilesPersonal.Add(perfil);
            }
            else
            {
                perfil = await db.PerfilesPersonal.Include(x => x.usuario).SingleAsync(x => x.id_usuario == formulario.id_usuario); usuario = perfil.usuario;
                Asegurar(await usuarios.SetEmailAsync(usuario, formulario.correo.Trim())); Asegurar(await usuarios.SetUserNameAsync(usuario, formulario.correo.Trim()));
                if (!string.IsNullOrWhiteSpace(formulario.clave)) Asegurar(await usuarios.ResetPasswordAsync(usuario, await usuarios.GeneratePasswordResetTokenAsync(usuario), formulario.clave));
            }
            var rolesAnteriores = await usuarios.GetRolesAsync(usuario);
            if (rolesAnteriores.Count > 0) Asegurar(await usuarios.RemoveFromRolesAsync(usuario, rolesAnteriores));
            var seleccionados = new List<string>();
            if (formulario.administrador) seleccionados.Add(Roles.Administrador);
            if (formulario.personal) seleccionados.Add(Roles.Personal);
            Asegurar(await usuarios.AddToRolesAsync(usuario, seleccionados));
            perfil.nombre_completo = formulario.nombre_completo.Trim(); perfil.cargo = formulario.cargo?.Trim(); perfil.activo = formulario.activo;
            var claimsAnteriores = (await usuarios.GetClaimsAsync(usuario)).Where(x => x.Type == "staff_name").ToList();
            if (claimsAnteriores.Count > 0) Asegurar(await usuarios.RemoveClaimsAsync(usuario, claimsAnteriores));
            Asegurar(await usuarios.AddClaimAsync(usuario, new Claim("staff_name", perfil.nombre_completo)));
            Asegurar(await usuarios.UpdateSecurityStampAsync(usuario));
            await db.GuardarOperacionAsync(); await tx.CommitAsync();
            TempData["Success"] = "Personal actualizado. Los cambios de permisos se aplican al renovar la sesión."; return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(formulario); }
    }

    private static void Asegurar(IdentityResult r) { if (!r.Succeeded) throw new InvalidOperationException(string.Join(" ", r.Errors.Select(x => x.Description))); }
}
