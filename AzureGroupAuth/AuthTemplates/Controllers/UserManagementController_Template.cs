using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOWTracker.Services;

namespace SOWTracker.Controllers
{
    [Authorize(Policy = "CanEdit")]
    public class UserManagementController : Controller
    {
        private readonly GraphService _graphService;

        public UserManagementController(GraphService graphService)
        {
            _graphService = graphService;
        }

        public async Task<IActionResult> Index()
        {
            var groupMembers = await _graphService.GetAllGroupMembersAsync();
            return View(groupMembers);
        }
    }
}
