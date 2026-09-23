using BlastPro.Mvc.Models.ViewModels.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers
{
    [Authorize]
    public class ResultsController : Controller
    {
       

        [HttpGet]
        public async Task<IActionResult> Index(int projectId)
        {
            if (projectId <= 0)
            {
                return View(new ResultsViewModel { ProjectId = 0 });
            }

            //this is sample data that needs to be replaced later with APIcalc
            
            await Task.CompletedTask;

            if (TempData["Success"] is string success)
            {
                ViewData["Success"] = success;
            }

            var model = BuildSampleResults(projectId);

            return View(model);
        }


        //saving the results
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int projectId)
        {
            if (projectId <= 0)
            {
                return RedirectToAction(nameof(Index), new { projectId });
            }

            
            //need to save the calc results through api
            await Task.CompletedTask;

            TempData["Success"] = "Results saved.";

            return RedirectToAction(nameof(Index), new { projectId });
        }


        // this is all sample data !!!!! will remove later should be good for presentation

        private static ResultsViewModel BuildSampleResults(int projectId)
        {
            var model = new ResultsViewModel
            {
                ProjectId = projectId,
                ProjectName = "Test 1 Pro",
                HasResults = true,
                IsOutdated = false,
                CalculatedAtUtc = DateTime.UtcNow.AddHours(-2),
                CalculatedByName = "Matthew Pickle",

                TotalHoles = 24,
                TotalExplosiveKg = 1300m,
                TotalDrillingMetres = 300m,
                TotalCost = 25676m,
                CurrencyCode = "ZAR",

                MaxChargePerDelayKg = 85m,
                MaxChargeSafetyIndexPercent = 80,
                MaxChargeSeverityLabel = "High",

                PowderFactorKgPerTonne = 0.4m,
                PowderFactorScaleMin = 0.1m,
                PowderFactorScaleMax = 0.6m,
                PowderFactorTargetMin = 0.4m,
                PowderFactorTargetMax = 0.4m,

                EstimatedVolumeCubicMetres = 750m,
                EstimatedTonnageTonnes = 2000m,
                PredictedPpvMmPerSecond = 12.5m,
                IdealizedFlyrockRangeMetres = 150m
            };

            model.Warnings.Add(new ResultWarningViewModel
            {
                Code = "PPV_LIMIT_EXCEEDED",
                Severity = "Critical",
                Message = "exceeds threshold (12.5 > 10 mm/s) - vibration damage to building is a possability."
            });

            model.Warnings.Add(new ResultWarningViewModel
            {
                Code = "POWDER_FACTOR_HIGH",
                Severity = "Warning",
                Message = "Powder factor is high (0.46 kg/tonne) - A high risk of fly rockk."
            });

            model.Warnings.Add(new ResultWarningViewModel
            {
                Code = "HOLE_STEMMING_LOW",
                Severity = "Warning",
                Message = "Hole 8 missing stemming - Stemming height is below the threshold."
            });

            model.PreviousResults.Add(new PreviousResultViewModel
            {
                Id = 1002,
                CalculatedAtUtc = DateTime.UtcNow.AddDays(-1),
                CalculatedByName = "Matthew Pickle",
                TotalCost = 23950m,
                PowderFactorKgPerTonne = 0.40m,
                WarningCount = 2
            });

            model.PreviousResults.Add(new PreviousResultViewModel
            {
                Id = 1001,
                CalculatedAtUtc = DateTime.UtcNow.AddDays(-3),
                CalculatedByName = "Matthew Pickle",
                TotalCost = 22100m,
                PowderFactorKgPerTonne = 0.38m,
                WarningCount = 1
            });

            return model;
        }
    }
}
