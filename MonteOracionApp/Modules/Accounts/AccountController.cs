using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Accounts;
public class AccountController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, ApplicationDbContext db) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });
    [HttpPost, EnableRateLimiting("forms")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var user = await users.FindByEmailAsync(form.Email.Trim());
        var active = user != null && await db.StaffProfiles.AnyAsync(x => x.UserId == user.Id && x.IsActive) && (await users.IsInRoleAsync(user, Roles.Administrator) || await users.IsInRoleAsync(user, Roles.Treasurer) || await users.IsInRoleAsync(user, Roles.Warehouse));
        var result = active ? await signIn.PasswordSignInAsync(user!, form.Password, false, lockoutOnFailure: true) : Microsoft.AspNetCore.Identity.SignInResult.Failed;
        if (!result.Succeeded) { ModelState.AddModelError("", "No se pudo iniciar sesión. Revisa tus datos o consulta al administrador si tu cuenta está desactivada."); return View(form); }
        if (Url.IsLocalUrl(form.ReturnUrl)) return LocalRedirect(form.ReturnUrl!);
        return RedirectToAction("Index", "Dashboard");
    }
    [Authorize, HttpPost] public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    [Authorize, HttpGet] public IActionResult Password() => View(new PasswordForm());
    [Authorize, HttpPost]
    public async Task<IActionResult> Password(PasswordForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var user = (await users.GetUserAsync(User))!;
        var result = await users.ChangePasswordAsync(user, form.CurrentPassword, form.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(form); }
        await signIn.RefreshSignInAsync(user); TempData["Success"] = "Contraseña actualizada."; return RedirectToAction(nameof(Password));
    }
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }
}
