using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOWTracker.Models;
using SOWTracker.Services;

namespace SOWTracker.Controllers
{
    [Authorize(Policy = "CanView")]
    public class SOWController : Controller
    {
        private readonly SOWService _sowService;
        private readonly CSVService _csvService;
        private readonly IAuthorizationService _authService;
        private readonly ILogger<SOWController> _logger;

        public SOWController(
            SOWService sowService,
            CSVService csvService,
            IAuthorizationService authService,
            ILogger<SOWController> logger)
        {
            _sowService = sowService;
            _csvService = csvService;
            _authService = authService;
            _logger = logger;
        }

        private async Task<bool> UserCanEdit()
        {
            return (await _authService.AuthorizeAsync(User, "CanEdit")).Succeeded;
        }

        // GET: SOW/Index (Pipeline view)
        public async Task<IActionResult> Index(string? status, string? lgrmif, string? search)
        {
            _logger.LogInformation("*** SOWController.Index ACTION HIT ***");
            _logger.LogInformation("*** User: {User}, IsAuthenticated: {IsAuth}",
                User.Identity?.Name ?? "Unknown",
                User.Identity?.IsAuthenticated ?? false);
            _logger.LogInformation("*** Filters - Status: {Status}, LGRMIF: {LGRMIF}, Search: {Search}",
                status ?? "none", lgrmif ?? "none", search ?? "none");

            try
            {
                _logger.LogInformation("*** Fetching SOWs from database...");

                var sows = await _sowService.GetAllSOWsAsync();

                _logger.LogInformation("*** Retrieved {Count} SOWs from database", sows.Count());

            // Apply filters
            if (!string.IsNullOrWhiteSpace(status))
            {
                sows = sows.Where(s => s.Status == status).ToList();
            }

            if (!string.IsNullOrWhiteSpace(lgrmif))
            {
                if (lgrmif == "(blank)")
                {
                    sows = sows.Where(s => string.IsNullOrWhiteSpace(s.LGRMIF)).ToList();
                }
                else
                {
                    sows = sows.Where(s => s.LGRMIF == lgrmif).ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                sows = sows.Where(s =>
                    s.CASONumber.ToLower().Contains(searchLower) ||
                    (s.Client?.ToLower().Contains(searchLower) ?? false) ||
                    (s.Project?.ToLower().Contains(searchLower) ?? false) ||
                    (s.PONumber?.ToLower().Contains(searchLower) ?? false) ||
                    (s.Notes?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }

                // Get KPIs for filtered data
                _logger.LogInformation("*** Calculating KPIs...");
                ViewBag.KPIs = await _sowService.GetKPIsAsync(sows);
                ViewBag.FilteredCount = sows.Count;
                ViewBag.TotalCount = (await _sowService.GetAllSOWsAsync()).Count;

                ViewBag.Status = status;
                ViewBag.LGRMIF = lgrmif;
                ViewBag.Search = search;
                ViewBag.NextCASONumber = await _sowService.GenerateNextCASONNumberAsync();

                _logger.LogInformation("*** Returning view with {Count} SOWs", sows.Count);
                return View(sows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "*** ERROR in SOWController.Index: {Message}", ex.Message);
                throw;
            }
        }

        // GET: SOW/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sow = await _sowService.GetSOWByIdAsync(id.Value);
            if (sow == null)
            {
                return NotFound();
            }

            return View(sow);
        }

        // GET: SOW/Create
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> Create()
        {
            var nextNumber = await _sowService.GenerateNextCASONNumberAsync();
            ViewBag.NextCASONumber = nextNumber;

            var model = new SOWTrackerModel
            {
                Status = "Open",
                Probability = 50,
                SOWVersion = 1,
                Dollars = 0,
                InvoicedToDate = 0
            };

            return View(model);
        }

        // POST: SOW/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> Create(SOWTrackerModel sow)
        {
            var validationError = _sowService.ValidateSOW(sow);
            if (!string.IsNullOrEmpty(validationError))
            {
                ModelState.AddModelError("", validationError);
                ViewBag.NextCASONumber = await _sowService.GenerateNextCASONNumberAsync();
                return View(sow);
            }

            try
            {
                await _sowService.CreateSOWAsync(sow);
                TempData["SuccessMessage"] = "SOW created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating SOW: {ex.Message}");
                ViewBag.NextCASONumber = await _sowService.GenerateNextCASONNumberAsync();
                return View(sow);
            }
        }

        // GET: SOW/Edit/5
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sow = await _sowService.GetSOWByIdAsync(id.Value);
            if (sow == null)
            {
                return NotFound();
            }

            return View(sow);
        }

        // POST: SOW/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> Edit(int id, SOWTrackerModel sow)
        {
            if (id != sow.Id)
            {
                return NotFound();
            }

            var validationError = _sowService.ValidateSOW(sow);
            if (!string.IsNullOrEmpty(validationError))
            {
                ModelState.AddModelError("", validationError);
                return View(sow);
            }

            try
            {
                await _sowService.UpdateSOWAsync(sow);
                TempData["SuccessMessage"] = "SOW updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating SOW: {ex.Message}");
                return View(sow);
            }
        }

        // GET: SOW/Delete/5
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sow = await _sowService.GetSOWByIdAsync(id.Value);
            if (sow == null)
            {
                return NotFound();
            }

            return View(sow);
        }

        // POST: SOW/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _sowService.DeleteSOWAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "SOW deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting SOW.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: SOW/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> Import(IFormFile? csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a CSV file to import.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = csvFile.OpenReadStream();
                var records = await _csvService.ImportFromCsvAsync(stream);

                int added = 0, updated = 0, skipped = 0;

                foreach (var record in records)
                {
                    var existing = await _sowService.GetSOWByCASONumberAsync(record.CASONumber);

                    if (existing != null)
                    {
                        existing.Status = record.Status;
                        existing.Client = record.Client;
                        existing.Project = record.Project;
                        existing.Dollars = record.Dollars;
                        existing.Probability = record.Probability;
                        existing.Weighted = record.Weighted;
                        existing.InvoicedToDate = record.InvoicedToDate;
                        existing.SOWVersion = record.SOWVersion;
                        existing.MainContactName = record.MainContactName;
                        existing.LGRMIF = record.LGRMIF;
                        existing.ProjectNumber = record.ProjectNumber;
                        existing.Form3 = record.Form3;
                        existing.PONumber = record.PONumber;
                        existing.ATP = record.ATP;
                        existing.Notes = record.Notes;

                        await _sowService.UpdateSOWAsync(existing);
                        updated++;
                    }
                    else
                    {
                        await _sowService.CreateSOWAsync(record);
                        added++;
                    }
                }

                TempData["SuccessMessage"] = $"Import completed: {added} added, {updated} updated, {skipped} skipped.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error importing CSV: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: SOW/Export
        public async Task<IActionResult> Export()
        {
            try
            {
                var sows = await _sowService.GetAllSOWsAsync();
                var csvData = _csvService.ExportToCsv(sows);

                var fileName = $"CASO-NYSID_SOW_Tracker_Export_{DateTime.Now:yyyyMMdd}.csv";
                return File(csvData, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error exporting CSV: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
