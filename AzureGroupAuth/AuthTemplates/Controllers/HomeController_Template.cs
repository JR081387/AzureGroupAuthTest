using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOWTracker.Models;
using SOWTracker.Services;

namespace SOWTracker.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SessionTrackingService _sessionTracker;

    public HomeController(ILogger<HomeController> logger, SessionTrackingService sessionTracker)
    {
        _logger = logger;
        _sessionTracker = sessionTracker;
    }

    public IActionResult Index()
    {
        // If user is already authenticated, redirect to Pipeline
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "SOW");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        // Remove active session before signing out
        var oid = User.FindFirst("oid")?.Value
            ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        if (!string.IsNullOrEmpty(oid))
        {
            _sessionTracker.RemoveSession(oid);
        }

        // Sign out from the local cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect to the landing page
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
