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

            TempData["Success"] = $"Project '{result.Data.Name}' created.";
            return RedirectToAction(DashboardAction, DashboardController);
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
        public IActionResult Details(int id)
        {
            if (id <= 0)
                return RedirectToAction(DashboardAction, DashboardController);

            return RedirectToAction("Index", "PatternDesign", new { projectId = id });
        }


        // =========================================================
        // PATTERN DESIGN SAVE
        // =========================================================

        // POST: /Projects/SaveDraft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(PatternDesignViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RestoreExplosiveProducts(model);
                return View("~/Views/Projects/Index.cshtml", model);
            }

            if (model.ProjectId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Create a project before saving this pattern design.");
                await RestoreExplosiveProducts(model);
                return View("~/Views/Projects/Index.cshtml", model);
            }

            var result = await _api.PutAsync<PatternDesignViewModel>(
                $"api/projects/{model.ProjectId}/pattern-design",
                new
                {
                    model.RockType,
                    model.RockDensity,
                    model.Burden,
                    model.Spacing,
                    model.VibrationThreshold,
                    model.RowVersion,
                    model.Holes
                });

            if (!result.Success || result.Data is null)
            {
                _logger.LogWarning("Save pattern design failed: {Error}", result.Error);
                ModelState.AddModelError(string.Empty,
                    result.Error ?? "Could not save the pattern design.");
                await RestoreExplosiveProducts(model);
                return View("~/Views/Projects/Index.cshtml", model);
            }

            ViewData["Success"] = "Draft layout saved.";
            return View("~/Views/Projects/Index.cshtml", result.Data);
        }


        // POST: /Projects/CalculatePhysics/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculatePhysics(PatternDesignViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RestoreExplosiveProducts(model);
                return View("~/Views/Projects/Index.cshtml", model);
            }

            if (model.Holes.Count == 0)
            {
                ModelState.AddModelError(string.Empty,
                    "Add at least one hole before calculating blast physics.");
                await RestoreExplosiveProducts(model);
                return View("~/Views/Projects/Index.cshtml", model);
            }

            // Save first, then route to Results placeholder
            var saveResult = await _api.PutAsync<PatternDesignViewModel>(
                $"api/projects/{model.ProjectId}/pattern-design",
                new
                {
                    model.RockType,
                    model.RockDensity,
                    model.Burden,
                    model.Spacing,
                    model.VibrationThreshold,
                    model.RowVersion,
                    model.Holes
                });

            if (!saveResult.Success)
            {
                ModelState.AddModelError(string.Empty,
                    saveResult.Error ?? "Could not save the pattern before calculation.");
                await RestoreExplosiveProducts(model);
                return View("~/Views/Projects/Index.cshtml", model);
            }

            TempData["Success"] = "Design saved. Calculation is not yet wired to the API.";
            return RedirectToAction("Index", "Results", new { projectId = model.ProjectId });
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


        // =========================================================
        // HELPERS
        // =========================================================

        private async Task RestoreExplosiveProducts(PatternDesignViewModel model)
        {
            if (model.ProjectId <= 0) return;

            var current = await _api.GetAsync<PatternDesignViewModel>(
                $"api/projects/{model.ProjectId}/pattern-design");

            if (current.Success && current.Data is not null)
                model.ExplosiveProducts = current.Data.ExplosiveProducts;
        }
    }
}