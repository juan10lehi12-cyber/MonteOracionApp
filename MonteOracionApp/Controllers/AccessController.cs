using Microsoft.AspNetCore.Mvc;
namespace MonteOracionApp.Controllers;
public class AccessController:Controller
{
    public IActionResult Index()=>View();
}
