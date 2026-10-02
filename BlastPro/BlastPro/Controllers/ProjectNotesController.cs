using BlastPro.Mvc.Models.Dtos;
using BlastPro.Mvc.Models.ViewModels;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
public sealed class ProjectNotesController(IApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int projectId)
    {
        var project = await api.GetAsync<ProjectDetailDto>($"api/projects/{projectId}");
        var notes = await api.GetAsync<List<ProjectNoteViewModel>>($"api/projects/{projectId}/notes");
        if (!project.Success || project.Data is null || !notes.Success)
            return NotFound();
        return View(new ProjectNotesPageViewModel
        {
            ProjectId = projectId, ProjectName = project.Data.Name, Notes = notes.Data ?? []
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SaveNoteForm form)
    {
        if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(form.Title) && string.IsNullOrWhiteSpace(form.Body)))
        {
            TempData["Error"] = "Enter a title or note text (up to 150 and 10,000 characters).";
            return RedirectToAction(nameof(Index), new { form.ProjectId });
        }
        var path = $"api/projects/{form.ProjectId}/notes";
        var result = form.NoteId == 0
            ? await api.PostAsync<object>(path, new { form.Title, form.Body })
            : await api.PutAsync<object>($"{path}/{form.NoteId}", new { form.Title, form.Body });
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Note saved." : result.Error ?? "Could not save the note.";
        return RedirectToAction(nameof(Index), new { form.ProjectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int projectId, int noteId)
    {
        var result = await api.DeleteAsync($"api/projects/{projectId}/notes/{noteId}");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Note deleted." : result.Error ?? "Could not delete the note.";
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int projectId, int noteId, string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > 2000)
        {
            TempData["Error"] = "Enter a comment of at most 2,000 characters.";
            return RedirectToAction(nameof(Index), new { projectId });
        }
        var result = await api.PostAsync<object>($"api/projects/{projectId}/notes/{noteId}/comments", new { body });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Comment added." : result.Error ?? "Could not add the comment.";
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int projectId, int noteId, IFormFile file)
    {
        if (file is null || file.Length is <= 0 or > 5_000_000)
        {
            TempData["Error"] = "Choose a photo smaller than 5 MB.";
            return RedirectToAction(nameof(Index), new { projectId });
        }
        var result = await api.PostFileAsync($"api/projects/{projectId}/notes/{noteId}/photos", file);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Photo uploaded." : result.Error ?? "Could not upload the photo.";
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int projectId, int noteId, int photoId)
    {
        var result = await api.GetFileAsync($"api/projects/{projectId}/notes/{noteId}/photos/{photoId}");
        return result.Success && result.Data is not null
            ? File(result.Data.Data, result.Data.ContentType)
            : NotFound();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int projectId, int noteId, int photoId)
    {
        var result = await api.DeleteAsync($"api/projects/{projectId}/notes/{noteId}/photos/{photoId}");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Photo removed." : result.Error ?? "Could not remove the photo.";
        return RedirectToAction(nameof(Index), new { projectId });
    }
}
