using BlastPro.Mvc.Models.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {

        // Reports is opened per project
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Preview), new { projectId = 0 });
        }


        [HttpGet]
        public IActionResult Preview(int projectId)
        {
            if (projectId <= 0)
            {
                return View(new ReportViewModel { ProjectId = 0 });
            }

            if (TempData["Info"] is string info)
            {
                ViewData["Info"] = info;
            }

            return View(new ReportViewModel
            {
                ProjectId = projectId,
                HasResults = false
            });
        }



        // DOWNLOAD PDF

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int projectId)
        {
            if (projectId <= 0)
            {
                return RedirectToAction(nameof(Preview), new { projectId });
            }

            await Task.CompletedTask;

            TempData["Info"] = "PDF generation isn't connected yet — this will download the report once the backend is ready.";

            return RedirectToAction(nameof(Preview), new { projectId });
        }
    }
}
