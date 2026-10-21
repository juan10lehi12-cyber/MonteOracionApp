using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace MonteOracionApp.Shared;
public sealed class ConcurrentOperationFilter(ITempDataDictionaryFactory tempData) : IExceptionFilter
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
