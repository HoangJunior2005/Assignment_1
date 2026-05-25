using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningDocumentSystem.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IDocumentService documentService, ILogger<HomeController> logger)
        {
            _documentService = documentService;
            _logger          = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Teacher"))
            {
                return RedirectToAction("Index", "Document");
            }

            try
            {
                var dashboard = await _documentService.GetDashboardAsync();
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard.");
                return View("Error");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ViewModels.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
