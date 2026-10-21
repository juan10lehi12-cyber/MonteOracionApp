using System.Security.Claims;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Data;
using MonteOracionApp.Shared;
namespace MonteOracionApp.Modules.Staff;
[Authorize(Roles = Roles.Administrator)]
public class StaffController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var memberships=await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id select new {ur.UserId,role.Name}).ToListAsync();
        ViewBag.RoleNames=memberships.GroupBy(x=>x.UserId).ToDictionary(x=>x.Key,x=>string.Join(" · ",x.Select(r=>r.Name)));
        return View(await db.StaffProfiles.Include(x=>x.User).OrderBy(x=>x.FullName).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Edit(string? id)
    {
        if (id == null) return View(new StaffForm());
        var p = await db.StaffProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.UserId == id);
        if (p == null) return NotFound();
        return View(new StaffForm { UserId = id, FullName = p.FullName, Position = p.Position, Email = p.User.Email!, IsActive = p.IsActive,
            Administrator = await users.IsInRoleAsync(p.User, Roles.Administrator), Treasurer = await users.IsInRoleAsync(p.User, Roles.Treasurer), Warehouse = await users.IsInRoleAsync(p.User, Roles.Warehouse) });
    }
    [HttpPost] public async Task<IActionResult> Edit(StaffForm form)
    {
        if (!form.Administrator && !form.Treasurer && !form.Warehouse) ModelState.AddModelError("", "Selecciona al menos un rol.");
        if (form.UserId == null && string.IsNullOrWhiteSpace(form.Password)) ModelState.AddModelError("Password", "La contraseña inicial es obligatoria.");
        if (form.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier) && (!form.IsActive || !form.Administrator)) ModelState.AddModelError("", "Conserva tu acceso de administrador; otro administrador puede cambiarlo.");
        if (!ModelState.IsValid) return View(form);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            IdentityUser user;
            StaffProfile profile;
            if (form.UserId == null)
            {
                user = new IdentityUser { UserName = form.Email.Trim(), Email = form.Email.Trim(), EmailConfirmed = true };
                Ensure(await users.CreateAsync(user, form.Password!));
                profile = new StaffProfile { UserId = user.Id }; db.StaffProfiles.Add(profile);
            }
            else
            {
                profile = await db.StaffProfiles.Include(x => x.User).SingleAsync(x => x.UserId == form.UserId); user = profile.User;
                Ensure(await users.SetEmailAsync(user, form.Email.Trim())); Ensure(await users.SetUserNameAsync(user, form.Email.Trim()));
                if (!string.IsNullOrWhiteSpace(form.Password)) Ensure(await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), form.Password));
            }
            var oldRoles = await users.GetRolesAsync(user);
            if (oldRoles.Count > 0) Ensure(await users.RemoveFromRolesAsync(user, oldRoles));
            var selected = new List<string>();
            if (form.Administrator) selected.Add(Roles.Administrator);
            if (form.Treasurer) selected.Add(Roles.Treasurer);
            if (form.Warehouse) selected.Add(Roles.Warehouse);
            Ensure(await users.AddToRolesAsync(user, selected));
            profile.FullName = form.FullName.Trim(); profile.Position = form.Position?.Trim(); profile.IsActive = form.IsActive;
            var oldClaims = (await users.GetClaimsAsync(user)).Where(x => x.Type == "staff_name").ToList();
            if (oldClaims.Count > 0) Ensure(await users.RemoveClaimsAsync(user, oldClaims));
            Ensure(await users.AddClaimAsync(user, new Claim("staff_name", profile.FullName)));
            Ensure(await users.UpdateSecurityStampAsync(user));
            db.Audit(User.FindFirstValue(ClaimTypes.NameIdentifier)!, "Personal actualizado", user.Id, "Roles: " + string.Join(", ", selected));
            await db.SaveOperationAsync(); await tx.CommitAsync();
            TempData["Success"] = "Personal actualizado. Los cambios de permisos se aplican al renovar la sesión."; return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(form); }
    }
    private static void Ensure(IdentityResult r) { if (!r.Succeeded) throw new InvalidOperationException(string.Join(" ", r.Errors.Select(x => x.Description))); }
}
