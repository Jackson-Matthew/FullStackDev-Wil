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
        // Sends /Projects to the dashboard so Cancel links work.
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
        // DETAILS
        // =========================================================

        // GET: /Projects/Details/{id}
        [HttpGet]
        public IActionResult Details(string id)
        {
            // TODO: load the project and return View(project)
            // once a Details.cshtml page has been built.
            return RedirectToAction(DashboardAction, DashboardController);
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