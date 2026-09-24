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
        public IActionResult Details(int id)
        {
            if (id <= 0)
                return RedirectToAction(DashboardAction, DashboardController);

            return RedirectToAction("Index", "PatternDesign", new { projectId = id });
        }


        // POST: /Projects/SaveDraft/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(PatternDesignViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // TODO: save the parameters + hole pattern through your API/service, e.g.
            // await _projectsApi.SavePatternDesignAsync(model);
            await Task.CompletedTask;

            ViewData["Success"] = "Draft layout saved.";

            return View("Index", model);
        }


        // POST: /Projects/CalculatePhysics/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculatePhysics(PatternDesignViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (model.Holes.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one hole before calculating blast physics.");
                return View("Index", model);
            }

            // TODO: send the design to your API/service for the physics calculation
            // and redirect to (or return) the results page, e.g.
            // var results = await _projectsApi.CalculateAsync(model);
            await Task.CompletedTask;

            ViewData["Success"] = "Design submitted for blast physics calculation.";

            return View("Index", model);
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
    }
}
