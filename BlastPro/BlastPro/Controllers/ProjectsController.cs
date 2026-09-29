using BlastPro.Mvc.Models.Dtos;
using BlastPro.Mvc.Models.ViewModels.Projects;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IApiClient _api;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IApiClient api, ILogger<ProjectsController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // =========================================================
        // DASHBOARD REDIRECT
        // =========================================================

        private const string DashboardController = "Dashboard";
        private const string DashboardAction = "Index";

        // GET: /Projects
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(DashboardAction, DashboardController);
        }


        // =========================================================
        // CREATE
        // =========================================================

        // GET: /Projects/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateProjectViewModel());
        }


        // POST: /Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _api.PostAsync<ProjectDetailDto>("api/projects", new
            {
                name = model.Name,
                siteLocation = model.SiteLocation,
                blastType = model.BlastType
            });

            if (!result.Success || result.Data is null)
            {
                _logger.LogWarning("Create project failed: {Error}", result.Error);
                ModelState.AddModelError(string.Empty,
                    result.Error ?? "Could not create the project. Please try again.");
                return View(model);
            }

            TempData["Success"] = $"Project '{result.Data.Name}' created. Start with bench dimensions and hole counts.";
            return RedirectToAction("Index", "PatternDesign", new { projectId = result.Data.Id });
        }


        // =========================================================
        // EDIT
        // =========================================================

        // GET: /Projects/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return RedirectToAction(DashboardAction, DashboardController);

            var result = await _api.GetAsync<ProjectDetailDto>($"api/projects/{id}");

            if (!result.Success || result.Data is null)
            {
                TempData["Error"] = result.Error ?? "Could not load the project.";
                return RedirectToAction(DashboardAction, DashboardController);
            }

            var model = new EditProjectViewModel
            {
                Id = result.Data.Id,
                Name = result.Data.Name,
                SiteLocation = result.Data.SiteLocation,
                BlastType = result.Data.BlastType
            };

            return View(model);
        }


        // POST: /Projects/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditProjectViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _api.PutAsync<object>($"api/projects/{id}", new
            {
                name = model.Name,
                siteLocation = model.SiteLocation,
                blastType = model.BlastType
            });

            if (!result.Success)
            {
                _logger.LogWarning("Update project failed: {Error}", result.Error);
                ModelState.AddModelError(string.Empty,
                    result.Error ?? "Could not save the project. Please try again.");
                return View(model);
            }

            TempData["Success"] = $"Project '{model.Name}' updated.";
            return RedirectToAction(DashboardAction, DashboardController);
        }


        // =========================================================
        // DETAILS (PATTERN DESIGN PAGE)
        // =========================================================

        // GET: /Projects/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return RedirectToAction(DashboardAction, DashboardController);

            var projectResult = await _api.GetAsync<ProjectDetailDto>($"api/projects/{id}");
            var designResult = await _api.GetAsync<PatternDesignViewModel>(
                $"api/projects/{id}/pattern-design");

            if (!projectResult.Success || projectResult.Data is null ||
                !designResult.Success || designResult.Data is null)
            {
                TempData["Error"] = projectResult.Error ?? designResult.Error ??
                    "Could not load the project.";
                return RedirectToAction(DashboardAction, DashboardController);
            }

            return View(new ProjectDetailsViewModel
            {
                Project = projectResult.Data,
                Holes = designResult.Data.Holes
            });
        }


        // Calculate the saved draft from the Results page.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculatePhysics(int projectId)
        {
            if (projectId <= 0) return RedirectToAction(DashboardAction, DashboardController);
            var saved = await _api.GetAsync<PatternDesignViewModel>(
                $"api/projects/{projectId}/pattern-design");
            if (!saved.Success || saved.Data is null)
            {
                TempData["Error"] = saved.Error ?? "Could not load the saved pattern.";
                return RedirectToAction(DashboardAction, DashboardController);
            }

            var model = saved.Data;
            var missing = new List<string>();
            if (model.Holes.Count == 0) missing.Add("at least one hole");
            if (model.Holes.Any(hole => hole.Charge <= 0 || hole.Stemming <= 0))
                missing.Add("positive charge and stemming for every hole");
            if (model.Calculation.DelayWindowMilliseconds is null or <= 0)
                missing.Add("a site charge grouping window");
            if (model.RockDensity <= 0 || model.Burden <= 0 || model.Spacing <= 0)
                missing.Add("rock density, burden and spacing");
            if (model.VibrationThreshold <= 0 || model.Calculation.ExclusionRadiusMetres is null or <= 0)
                missing.Add("vibration and exclusion limits");
            if (missing.Count > 0)
            {
                TempData["Error"] = "Complete the saved draft before calculating: " +
                    string.Join("; ", missing) + ".";
                return RedirectToAction("Index", "Results", new { projectId });
            }

            var calculation = await _api.PostAsync<object>(
                $"api/projects/{projectId}/calculations",
                new
                {
                    DelayWindowMilliseconds = model.Calculation.DelayWindowMilliseconds.GetValueOrDefault(),
                    model.Calculation.SubdrillMetres,
                    model.Calculation.ReceptorDistanceMetres,
                    model.Calculation.PpvSiteCoefficient,
                    model.Calculation.PpvDecayExponent,
                    model.Calculation.FlyrockLaunchSpeedMetresPerSecond,
                    model.Calculation.FlyrockLaunchAngleDegrees,
                    model.Calculation.FlyrockLaunchHeightMetres,
                    model.Calculation.ExclusionRadiusMetres
                });
            if (!calculation.Success)
            {
                TempData["Error"] = calculation.Error ?? "The calculation could not be saved.";
                return RedirectToAction("Index", "Results", new { projectId });
            }

            TempData["Success"] = "Calculation saved.";
            return RedirectToAction("Index", "Results", new { projectId });
        }


        // =========================================================
        // DELETE
        // =========================================================

        // POST: /Projects/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return RedirectToAction(DashboardAction, DashboardController);

            var result = await _api.DeleteAsync($"api/projects/{id}");

            if (!result.Success)
            {
                _logger.LogWarning("Delete project failed: {Error}", result.Error);
                TempData["Error"] = result.Error ?? "Could not delete the project.";
            }
            else
            {
                TempData["Success"] = "Project deleted.";
            }

            return RedirectToAction(DashboardAction, DashboardController);
        }


    }
}
