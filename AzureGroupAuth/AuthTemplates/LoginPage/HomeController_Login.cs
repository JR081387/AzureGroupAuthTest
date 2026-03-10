// ============================================================================
// HOME CONTROLLER - LOGIN LANDING PAGE + LOGOUT
// ============================================================================
// Copy this to: Controllers/HomeController.cs
//
// This controller handles:
//   - Displaying the login/landing page to unauthenticated users
//   - Redirecting already-authenticated users to the main app page
//   - Logout flow (clears session, signs out of cookie auth)
//
// IMPORTANT: This controller uses [AllowAnonymous] so the login page
// is accessible without authentication. All other controllers should
// use [Authorize] to require login.
// ============================================================================

using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YourAppNamespace.Controllers;  // <-- CHANGE to your namespace

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // If user is already authenticated, skip the login page
        // and send them straight to the main app page.
        if (User.Identity?.IsAuthenticated == true)
        {
            // =============================================================
            // >>> CHANGE THIS to your app's main page after login <<<
            // =============================================================
            // Examples:
            //   return RedirectToAction("Index", "Dashboard");
            //   return RedirectToAction("Index", "Products");
            //   return RedirectToAction("Home", "Admin");
            // =============================================================
            return RedirectToAction("Index", "SOW");  // <-- CHANGE THIS
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
        // Sign out from the local cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect back to the login/landing page
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

// ============================================================================
// NOTE: You'll need an ErrorViewModel class in your Models folder.
// If you don't have one, create Models/ErrorViewModel.cs:
//
//   namespace YourAppNamespace.Models;
//   public class ErrorViewModel
//   {
//       public string? RequestId { get; set; }
//       public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
//   }
// ============================================================================
