using BlastPro.Mvc.Models.ViewModels.Projects;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers
{
    public class ProjectsController : Controller
    {
        // =========================================================
        // DASHBOARD REDIRECT
        // =========================================================

        // Where the dashboard lives. Change this if your dashboard
        // is rendered by a different controller/action.
        private const string DashboardController = "Home";
        private const string DashboardAction = "Index";


        // GET: /Projects
        // Sends /Projects to the dashboard so Cancel / Back links work.
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

            // TODO: save the project through your API/service, e.g.
            // await _projectsApi.CreateAsync(model.Name, model.SiteLocation, model.BlastType);
            await Task.CompletedTask;

            return RedirectToAction(DashboardAction, DashboardController);
        }


        // =========================================================
        // DETAILS (PATTERN DESIGN PAGE)
        // =========================================================

        // GET: /Projects/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(DashboardAction, DashboardController);
            }

            // TODO: load the project from your API/service instead of
            // using sample data, e.g.
            // var model = await _projectsApi.GetPatternDesignAsync(id);
            // if (model == null) return NotFound();
            await Task.CompletedTask;

            var model = BuildSampleDesign(id);

            return View(model);
        }


        // POST: /Projects/SaveDraft/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(PatternDesignViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Details", model);
            }

            // TODO: save the parameters + hole pattern through your API/service, e.g.
            // await _projectsApi.SavePatternDesignAsync(model);
            await Task.CompletedTask;

            ViewData["Success"] = "Draft layout saved.";

            return View("Details", model);
        }


        // POST: /Projects/CalculatePhysics/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculatePhysics(PatternDesignViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Details", model);
            }

            if (model.Holes.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one hole before calculating blast physics.");
                return View("Details", model);
            }

            // TODO: send the design to your API/service for the physics calculation
            // and redirect to (or return) the results page, e.g.
            // var results = await _projectsApi.CalculateAsync(model);
            await Task.CompletedTask;

            ViewData["Success"] = "Design submitted for blast physics calculation.";

            return View("Details", model);
        }


        // =========================================================
        // DELETE
        // =========================================================

        // POST: /Projects/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            // TODO: delete the project through your API/service, e.g.
            // await _projectsApi.DeleteAsync(id);
            await Task.CompletedTask;

            return RedirectToAction(DashboardAction, DashboardController);
        }


        // =========================================================
        // SAMPLE DATA (remove once the API is connected)
        // =========================================================

        private static PatternDesignViewModel BuildSampleDesign(string id)
        {
            var model = new PatternDesignViewModel
            {
                ProjectId = id,
                ProjectName = "Test 1 Project",
                Status = "Reviewed",
                RockType = "Granite",
                RockDensity = 2.65m,
                Burden = 3.00m,
                Spacing = 3.50m,
                VibrationThreshold = 10.0m
            };

            var positions = new (decimal X, decimal Y)[]
            {
                (12.50m, 4.50m), (15.50m, 4.50m), (18.50m, 4.50m), (21.50m, 4.50m),
                (12.50m, 8.00m), (15.50m, 8.00m), (18.50m, 8.00m), (21.50m, 8.00m)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                bool isEmulsion = i == 3 || i == 4;

                model.Holes.Add(new BlastHoleViewModel
                {
                    Number = i + 1,
                    X = positions[i].X,
                    Y = positions[i].Y,
                    Depth = i < 4 ? 12.0m : 12.5m,
                    Explosive = isEmulsion ? "Emulsion Max" : "ANFO Pack",
                    Charge = isEmulsion ? 9.0m : 8.5m,
                    Stemming = i < 3 ? 3.5m : (i == 3 ? 3.0m : (i == 4 ? 3.5m : 4.0m)),
                    Delay = (i + 1) * 25
                });
            }

            return model;
        }
    }
}