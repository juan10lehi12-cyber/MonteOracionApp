using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
namespace MonteOracionApp.Servicios;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Personal = "Personal";
    public const string Equipo = Administrador + "," + Personal;
    public const string Almacen = Equipo;
}

public static class HoraIglesia
{
    public static DateTime Ahora => DateTime.UtcNow.AddHours(-5);
    public static DateTime Hoy => Ahora.Date;
}

public static class Operaciones
{
    public static async Task GuardarOperacionAsync(this ContextoBaseDatos db)
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Otra persona modificó el registro. Actualiza y vuelve a intentar."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { throw new InvalidOperationException("La operación ya fue registrada o el dato está duplicado. Actualiza la página."); }
    }
}

public static class ReglasOperacion
{
    public static void Cantidad(decimal valor) => ReglasDonacion.ValidarMonto(valor);
    public static void Motivo(string? motivo)
    { if (string.IsNullOrWhiteSpace(motivo) || motivo.Length > 1000) throw new InvalidOperationException("Indica un motivo de hasta 1000 caracteres."); }
}

public sealed class FiltroOperacionConcurrente(ITempDataDictionaryFactory tempData) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var sql = context.Exception as SqlException ?? context.Exception.InnerException as SqlException;
        if (context.Exception is DbUpdateConcurrencyException || sql?.Number is 1205 or 1222)
        {
            tempData.GetTempData(context.HttpContext)["Error"] = "Otra operación modificó estos recursos al mismo tiempo. Revisa el registro y vuelve a intentar; no se confirmó la operación interrumpida.";
            context.Result = new RedirectToActionResult("Index", "Dashboard", null);
            context.ExceptionHandled = true;
        }
    }
}

public sealed class ExpansorVistasEspanol : IViewLocationExpander
{
    private static readonly Dictionary<string, string> Controladores = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Home"] = "Inicio",
        ["Dashboard"] = "Panel",
        ["Account"] = "Cuenta",
        ["Donations"] = "Donaciones",
        ["Inventory"] = "Almacen",
        ["Publications"] = "Publicaciones",
        ["Congregants"] = "Miembros",
        ["Staff"] = "Personal",
        ["Settings"] = "Configuracion"
    };

    private static readonly Dictionary<string, string> Vistas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Index"] = "Indice",
        ["Privacy"] = "Privacidad",
        ["Login"] = "Acceso",
        ["Password"] = "Clave",
        ["Denied"] = "Denegado",
        ["Create"] = "Crear",
        ["Submitted"] = "Enviada",
        ["Receive"] = "Recibir",
        ["Details"] = "Detalle",
        ["Pending"] = "Pendientes",
        ["Exit"] = "Salida",
        ["Movements"] = "Movimientos",
        ["Edit"] = "Editar",
        ["Payments"] = "MediosPago",
        ["Payment"] = "MedioPago",
        ["_Layout"] = "_Diseno",
        ["_LoginPartial"] = "_AccesoParcial",
        ["_Pagination"] = "_Paginacion",
        ["_DonationTable"] = "_TablaDonaciones",
        ["_EventModal"] = "_ModalEvento",
        ["_ValidationScriptsPartial"] = "_ScriptsValidacion"
    };

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        if (context.ControllerName != null && Controladores.TryGetValue(context.ControllerName, out var c))
            context.Values["es_ctrl"] = c;
        if (context.ViewName != null && Vistas.TryGetValue(context.ViewName, out var v))
            context.Values["es_view"] = v;
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var ctrl = context.Values.TryGetValue("es_ctrl", out var c) && c != null ? c : context.ControllerName;
        var view = context.Values.TryGetValue("es_view", out var v) && v != null ? v : context.ViewName;
        return
        [
            $"/Vistas/{ctrl}/{view}.cshtml",
            $"/Vistas/Compartido/{view}.cshtml",
            "/Vistas/{1}/{0}.cshtml",
            "/Vistas/Compartido/{0}.cshtml"
        ];
    }
}
