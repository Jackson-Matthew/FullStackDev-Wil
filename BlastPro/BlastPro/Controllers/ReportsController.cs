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
        public async Task<IActionResult> Preview(int projectId)
        {
            if (projectId <= 0)
            {
                return View(new ReportViewModel { ProjectId = 0 });
            }

            // replace sample data with reportService
            await Task.CompletedTask;

            if (TempData["Info"] is string info)
            {
                ViewData["Info"] = info;
            }

            var model = BuildSampleReport(projectId);

            return View(model);
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


        // remove sample data once the api is ready

        private static ReportViewModel BuildSampleReport(int projectId)
        {
            var model = new ReportViewModel
            {
                ProjectId = projectId,
                ProjectName = "Test 1",
                ReportDateUtc = new DateTime(2026, 8, 13),
                CompanyName = "TestCompany",
                CalculationsUnit = "Metric System",
                HasResults = true,

                TotalDesignatedHoles = 24,
                TotalExplosiveLoadKg = 1280m,
                TotalDrillingLengthMetres = 312m,
                TotalEstimatedTonnageTonnes = 28400m,
                PowderFactorKgPerTonne = 0.42m,
                TotalEstimatedMaterialCost = 24600m,
                CurrencyCode = "ZAR",

                MaxChargePerDelayKg = 85m,
                PredictedPpvMmPerSecond = 8.2m,
                IdealizedFlyrockRangeMetres = 145m
            };

            // Same 4 across grid pattern used on Pattern Design's sample layout,
            for (int i = 0; i < model.TotalDesignatedHoles; i++)
            {
                int rowIndex = i / 4;
                int colIndex = i % 4;

                bool isEmulsion = colIndex == 3 || (colIndex == 0 && rowIndex > 0);

                decimal stemming = rowIndex == 0
                    ? (colIndex == 3 ? 3.0m : 3.5m)
                    : (rowIndex == 1 && colIndex == 0 ? 3.5m : 4.0m);

                model.Holes.Add(new ReportHoleViewModel
                {
                    Number = i + 1,
                    X = 12.50m + colIndex * 3.00m,
                    Y = 4.50m + rowIndex * 3.50m,
                    Depth = rowIndex == 0 ? 12.0m : 12.5m,
                    Explosive = isEmulsion ? "Emulsion Max" : "ANFO Pack",
                    Charge = isEmulsion ? 9.0m : 8.5m,
                    Stemming = stemming,
                    Delay = (i + 1) * 25
                });
            }

            model.Warnings.Add(new ReportWarningViewModel
            {
                Severity = "Critical",
                Message = "PPV exceeds threshold ."
            });

            model.Warnings.Add(new ReportWarningViewModel
            {
                Severity = "Warning",
                Message = "Powder factor is high (0.46 kg/tonne)."
            });

            model.Warnings.Add(new ReportWarningViewModel
            {
                Severity = "Warning",
                Message = "Hole 8 missing stemming   potential extra safety required."
            });

            return model;
        }
    }
}
