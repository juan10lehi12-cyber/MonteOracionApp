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
    public async Task<IActionResult> Index(string? search, string? rol, bool? active)
    {
        var membresias = await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id select new { ur.UserId, role.Name }).ToListAsync();
        var rolesPorUsuario = membresias.GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => (IList<string>)x.Select(r => r.Name ?? "").ToList());
        ViewBag.UserRoles = rolesPorUsuario;

        var q = db.PerfilesPersonal.Include(x => x.usuario).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(x => x.nombre_completo.Contains(term)
                             || (x.cargo != null && x.cargo.Contains(term))
                             || (x.usuario.UserName != null && x.usuario.UserName.Contains(term))
                             || (x.usuario.Email != null && x.usuario.Email.Contains(term)));
        }

        if (active.HasValue)
            q = q.Where(x => x.activo == active.Value);

        var lista = await q.OrderBy(x => x.nombre_completo).ToListAsync();
        if (!string.IsNullOrWhiteSpace(rol))
            lista = lista.Where(p => rolesPorUsuario.TryGetValue(p.id_usuario, out var rs) && rs.Contains(rol, StringComparer.OrdinalIgnoreCase)).ToList();

        ViewBag.Search = search;
        ViewBag.Rol = rol;
        ViewBag.Active = active;
        return View(lista);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null) return View(new FormularioPersonal());
        var p = await db.PerfilesPersonal.Include(x => x.usuario).SingleOrDefaultAsync(x => x.id_usuario == id);
        if (p == null) return NotFound();

        var esCorreoSintetico = p.usuario.Email?.EndsWith("@monteoracion.local") == true;
        var tieneGeneral = await usuarios.IsInRoleAsync(p.usuario, Roles.Personal);
        return View(new FormularioPersonal
        {
            id_usuario = id,
            nombre_completo = p.nombre_completo,
            cargo = p.cargo,
            usuario = p.usuario.UserName ?? p.usuario.Email ?? "",
            correo = esCorreoSintetico ? null : p.usuario.Email,
            activo = p.activo,
            administrador = await usuarios.IsInRoleAsync(p.usuario, Roles.Administrador),
            recepcion = await usuarios.IsInRoleAsync(p.usuario, Roles.Recepcion) || tieneGeneral,
            almacen = await usuarios.IsInRoleAsync(p.usuario, Roles.Almacen) || tieneGeneral,
            personal = tieneGeneral
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(FormularioPersonal formulario)
    {
        if (!formulario.administrador && !formulario.recepcion && !formulario.almacen && !formulario.personal) ModelState.AddModelError("", "Selecciona al menos un rol para el usuario.");
        if (string.IsNullOrWhiteSpace(formulario.usuario)) ModelState.AddModelError(nameof(formulario.usuario), "El nombre de usuario es obligatorio.");
        if (formulario.id_usuario == null && string.IsNullOrWhiteSpace(formulario.clave)) ModelState.AddModelError(nameof(formulario.clave), "La contraseña inicial es obligatoria.");
        if (formulario.id_usuario == User.FindFirstValue(ClaimTypes.NameIdentifier) && (!formulario.activo || !formulario.administrador)) ModelState.AddModelError("", "Conserva tu acceso de administrador; otro administrador puede cambiarlo.");
        if (!ModelState.IsValid) return View(formulario);

        var userNameLimpio = formulario.usuario.Trim().ToLowerInvariant();
        var emailLimpio = !string.IsNullOrWhiteSpace(formulario.correo)
            ? formulario.correo.Trim().ToLowerInvariant()
            : $"{userNameLimpio}@monteoracion.local";

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            IdentityUser usuario;
            PerfilPersonal perfil;
            if (formulario.id_usuario == null)
            {
                var usuarioExistente = await usuarios.FindByNameAsync(userNameLimpio);
                if (usuarioExistente != null)
                {
                    ModelState.AddModelError(nameof(formulario.usuario), $"El usuario «{userNameLimpio}» ya existe. Elige otro nombre de usuario.");
                    return View(formulario);
                }

                var emailExistente = await usuarios.FindByEmailAsync(emailLimpio);
                if (emailExistente != null)
                {
                    emailLimpio = $"{userNameLimpio}.{Guid.NewGuid().ToString("N")[..4]}@monteoracion.local";
                }

                usuario = new IdentityUser { UserName = userNameLimpio, Email = emailLimpio, EmailConfirmed = true };
                Asegurar(await usuarios.CreateAsync(usuario, formulario.clave!));
                perfil = new PerfilPersonal { id_usuario = usuario.Id };
                db.PerfilesPersonal.Add(perfil);
            }
            else
            {
                perfil = await db.PerfilesPersonal.Include(x => x.usuario).SingleAsync(x => x.id_usuario == formulario.id_usuario);
                usuario = perfil.usuario;

                var existenteConEseNombre = await usuarios.FindByNameAsync(userNameLimpio);
                if (existenteConEseNombre != null && existenteConEseNombre.Id != usuario.Id)
                {
                    ModelState.AddModelError(nameof(formulario.usuario), $"El nombre de usuario «{userNameLimpio}» ya está en uso por otra persona.");
                    return View(formulario);
                }

                var existenteConEseEmail = await usuarios.FindByEmailAsync(emailLimpio);
                if (existenteConEseEmail != null && existenteConEseEmail.Id != usuario.Id)
                {
                    emailLimpio = $"{userNameLimpio}.{Guid.NewGuid().ToString("N")[..4]}@monteoracion.local";
                }

                Asegurar(await usuarios.SetUserNameAsync(usuario, userNameLimpio));
                Asegurar(await usuarios.SetEmailAsync(usuario, emailLimpio));

                if (!string.IsNullOrWhiteSpace(formulario.clave))
                    Asegurar(await usuarios.ResetPasswordAsync(usuario, await usuarios.GeneratePasswordResetTokenAsync(usuario), formulario.clave));
            }

            var rolesManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var nombreRol in Roles.Todos)
                if (!await rolesManager.RoleExistsAsync(nombreRol))
                    Asegurar(await rolesManager.CreateAsync(new IdentityRole(nombreRol)));

            var rolesAnteriores = await usuarios.GetRolesAsync(usuario);
            if (rolesAnteriores.Count > 0) Asegurar(await usuarios.RemoveFromRolesAsync(usuario, rolesAnteriores));
            var seleccionados = new List<string>();
            if (formulario.administrador) seleccionados.Add(Roles.Administrador);
            if (formulario.recepcion) seleccionados.Add(Roles.Recepcion);
            if (formulario.almacen) seleccionados.Add(Roles.Almacen);
            if (seleccionados.Count == 0 && formulario.personal) seleccionados.Add(Roles.Personal);
            Asegurar(await usuarios.AddToRolesAsync(usuario, seleccionados));

            perfil.nombre_completo = formulario.nombre_completo.Trim();
            perfil.cargo = formulario.cargo?.Trim();
            perfil.activo = formulario.activo;

            Asegurar(await usuarios.UpdateSecurityStampAsync(usuario));

            await db.GuardarOperacionAsync();
            await tx.CommitAsync();

            TempData["Success"] = $"Usuario «{userNameLimpio}» guardado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(formulario); }
    }

    private static void Asegurar(IdentityResult r) { if (!r.Succeeded) throw new InvalidOperationException(string.Join(" ", r.Errors.Select(x => x.Description))); }
}
