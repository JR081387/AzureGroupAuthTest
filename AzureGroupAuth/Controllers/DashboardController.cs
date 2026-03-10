using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureGroupAuth.Controllers;

[Authorize(Policy = "CanView")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
