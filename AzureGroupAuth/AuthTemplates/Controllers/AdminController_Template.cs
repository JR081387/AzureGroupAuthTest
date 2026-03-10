using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOWTracker.Services;

namespace SOWTracker.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly SessionTrackingService _sessionTracker;

    public AdminController(SessionTrackingService sessionTracker)
    {
        _sessionTracker = sessionTracker;
    }

    public IActionResult ActiveSessions()
    {
        var activeSessions = _sessionTracker.GetActiveSessions(15);
        var allSessions = _sessionTracker.GetAllSessions();

        ViewBag.ActiveCount = activeSessions.Count;
        ViewBag.TotalTracked = allSessions.Count;

        return View(activeSessions);
    }
}
