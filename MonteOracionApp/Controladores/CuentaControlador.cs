using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Controladores;

public class AccountController(SignInManager<IdentityUser> acceso, UserManager<IdentityUser> usuarios, ContextoBaseDatos db) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl) => View(new FormularioAcceso { url_retorno = returnUrl });

    [HttpPost, EnableRateLimiting("forms")]
    public async Task<IActionResult> Login(FormularioAcceso formulario)
    {
        if (!ModelState.IsValid) return View(formulario);
        var usuario = await usuarios.FindByEmailAsync(formulario.correo.Trim());
        var activo = usuario != null && await db.PerfilesPersonal.AnyAsync(x => x.id_usuario == usuario.Id && x.activo) && (await usuarios.IsInRoleAsync(usuario, Roles.Administrador) || await usuarios.IsInRoleAsync(usuario, Roles.Personal));
        var resultado = activo ? await acceso.PasswordSignInAsync(usuario!, formulario.clave, false, lockoutOnFailure: true) : Microsoft.AspNetCore.Identity.SignInResult.Failed;
        if (!resultado.Succeeded) { ModelState.AddModelError("", "No se pudo iniciar sesión. Revisa tus datos o consulta al administrador si tu cuenta está desactivada."); return View(formulario); }
        if (Url.IsLocalUrl(formulario.url_retorno)) return LocalRedirect(formulario.url_retorno!);
        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize, HttpPost] public async Task<IActionResult> Logout() { await acceso.SignOutAsync(); return RedirectToAction("Index", "Home"); }

    [Authorize, HttpGet] public IActionResult Password() => View(new FormularioClave());

    [Authorize, HttpPost]
    public async Task<IActionResult> Password(FormularioClave formulario)
    {
        if (!ModelState.IsValid) return View(formulario);
        var usuario = (await usuarios.GetUserAsync(User))!;
        var resultado = await usuarios.ChangePasswordAsync(usuario, formulario.clave_actual, formulario.clave_nueva);
        if (!resultado.Succeeded) { foreach (var error in resultado.Errors) ModelState.AddModelError("", error.Description); return View(formulario); }
        await acceso.RefreshSignInAsync(usuario); TempData["Success"] = "Contraseña actualizada."; return RedirectToAction(nameof(Password));
    }

    public IActionResult Denied() { Response.StatusCode = 403; return View(); }
}
