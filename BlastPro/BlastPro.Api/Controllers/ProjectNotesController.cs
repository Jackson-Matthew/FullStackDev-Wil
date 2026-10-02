using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController, Authorize, Route("api/projects/{projectId:int}/notes")]
public sealed class ProjectNotesController(ApplicationDbContext db) : ControllerBase
{
    private async Task<bool> CanAccess(int projectId)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        return userId is not null && companyId is not null && await db.BlastProjects.AnyAsync(p =>
            p.Id == projectId && p.CompanyId == companyId &&
            (p.OwnerId == userId || User.IsInRole(DatabaseSeeder.MainCompanyUserRole)));
    }

    private Task<ProjectNote?> FindNote(int projectId, int noteId) => db.ProjectNotes
        .Include(n => n.Author).Include(n => n.Photos)
        .Include(n => n.Comments).ThenInclude(c => c.Author)
        .FirstOrDefaultAsync(n => n.Id == noteId && n.BlastProjectId == projectId);

    private static ProjectNoteDto ToDto(ProjectNote note) => new(
        note.Id, note.BlastProjectId, note.Title, note.Body, note.Author.FullName,
        note.CreatedAtUtc, note.UpdatedAtUtc,
        note.Photos.OrderBy(p => p.CreatedAtUtc).Select(p =>
            new ProjectNotePhotoDto(p.Id, p.FileName, p.ContentType, p.CreatedAtUtc)).ToList(),
        note.Comments.OrderBy(c => c.CreatedAtUtc).Select(c =>
            new ProjectNoteCommentDto(c.Id, c.Body, c.Author.FullName, c.CreatedAtUtc)).ToList());

    [HttpGet]
    public async Task<ActionResult<List<ProjectNoteDto>>> Get(int projectId)
    {
        if (!await CanAccess(projectId)) return NotFound();
        var notes = await db.ProjectNotes.AsNoTracking().AsSplitQuery()
            .Include(n => n.Author).Include(n => n.Photos)
            .Include(n => n.Comments).ThenInclude(c => c.Author)
            .Where(n => n.BlastProjectId == projectId)
            .OrderByDescending(n => n.UpdatedAtUtc).ToListAsync();
        return Ok(notes.Select(ToDto).ToList());
    }

    [HttpGet("{noteId:int}")]
    public async Task<ActionResult<ProjectNoteDto>> GetOne(int projectId, int noteId)
    {
        if (!await CanAccess(projectId)) return NotFound();
        var note = await FindNote(projectId, noteId);
        return note is null ? NotFound() : Ok(ToDto(note));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectNoteDto>> Create(int projectId, SaveProjectNoteRequest request)
    {
        if (!await CanAccess(projectId)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { message = "Enter a title or note text." });
        var now = DateTime.UtcNow;
        var note = new ProjectNote
        {
            BlastProjectId = projectId, AuthorId = User.GetUserId()!, Title = request.Title.Trim(),
            Body = request.Body.Trim(), CreatedAtUtc = now, UpdatedAtUtc = now
        };
        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync();
        note = (await FindNote(projectId, note.Id))!;
        return CreatedAtAction(nameof(GetOne), new { projectId, noteId = note.Id }, ToDto(note));
    }

    [HttpPut("{noteId:int}")]
    public async Task<IActionResult> Update(int projectId, int noteId, SaveProjectNoteRequest request)
    {
        if (!await CanAccess(projectId)) return NotFound();
        var note = await db.ProjectNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.BlastProjectId == projectId);
        if (note is null) return NotFound();
        if (note.AuthorId != User.GetUserId() && !User.IsInRole(DatabaseSeeder.MainCompanyUserRole)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { message = "Enter a title or note text." });
        note.Title = request.Title.Trim();
        note.Body = request.Body.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{noteId:int}")]
    public async Task<IActionResult> Delete(int projectId, int noteId)
    {
        if (!await CanAccess(projectId)) return NotFound();
        var note = await db.ProjectNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.BlastProjectId == projectId);
        if (note is null) return NotFound();
        if (note.AuthorId != User.GetUserId() && !User.IsInRole(DatabaseSeeder.MainCompanyUserRole)) return Forbid();
        db.ProjectNotes.Remove(note);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{noteId:int}/comments")]
    public async Task<ActionResult<ProjectNoteCommentDto>> AddComment(int projectId, int noteId,
        AddProjectNoteCommentRequest request)
    {
        if (!await CanAccess(projectId)) return NotFound();
        if (!await db.ProjectNotes.AnyAsync(n => n.Id == noteId && n.BlastProjectId == projectId)) return NotFound();
        var comment = new ProjectNoteComment
        {
            ProjectNoteId = noteId, AuthorId = User.GetUserId()!, Body = request.Body.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        if (comment.Body.Length == 0) return BadRequest(new { message = "Enter a comment." });
        db.ProjectNoteComments.Add(comment);
        await db.SaveChangesAsync();
        var authorName = await db.Users.Where(u => u.Id == comment.AuthorId).Select(u => u.FullName).SingleAsync();
        return Ok(new ProjectNoteCommentDto(comment.Id, comment.Body, authorName, comment.CreatedAtUtc));
    }

    [HttpPost("{noteId:int}/photos")]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<ProjectNotePhotoDto>> AddPhoto(int projectId, int noteId, IFormFile file)
    {
        if (!await CanAccess(projectId)) return NotFound();
        var note = await db.ProjectNotes.Include(n => n.Photos)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.BlastProjectId == projectId);
        if (note is null) return NotFound();
        if (note.AuthorId != User.GetUserId() && !User.IsInRole(DatabaseSeeder.MainCompanyUserRole)) return Forbid();
        if (note.Photos.Count >= 3) return BadRequest(new { message = "A note can have at most three photos." });
        if (file is null || file.Length is <= 0 or > 5_000_000)
            return BadRequest(new { message = "Choose an image smaller than 5 MB." });
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var data = stream.ToArray();
        var contentType = DetectImageType(data);
        if (contentType is null) return BadRequest(new { message = "Only JPEG, PNG, and WebP images are supported." });
        var photo = new ProjectNotePhoto
        {
            ProjectNoteId = noteId, FileName = Path.GetFileName(file.FileName)[..Math.Min(200, Path.GetFileName(file.FileName).Length)],
            ContentType = contentType, Data = data, CreatedAtUtc = DateTime.UtcNow
        };
        db.ProjectNotePhotos.Add(photo);
        note.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new ProjectNotePhotoDto(photo.Id, photo.FileName, photo.ContentType, photo.CreatedAtUtc));
    }

    [HttpGet("{noteId:int}/photos/{photoId:int}")]
    public async Task<IActionResult> GetPhoto(int projectId, int noteId, int photoId)
    {
        if (!await CanAccess(projectId)) return NotFound();
        var photo = await db.ProjectNotePhotos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.ProjectNoteId == noteId && p.Note.BlastProjectId == projectId);
        return photo is null ? NotFound() : File(photo.Data, photo.ContentType);
    }

    [HttpDelete("{noteId:int}/photos/{photoId:int}")]
    public async Task<IActionResult> DeletePhoto(int projectId, int noteId, int photoId)
    {
        if (!await CanAccess(projectId)) return NotFound();
        var note = await db.ProjectNotes.Include(n => n.Photos)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.BlastProjectId == projectId);
        if (note is null) return NotFound();
        if (note.AuthorId != User.GetUserId() && !User.IsInRole(DatabaseSeeder.MainCompanyUserRole)) return Forbid();
        var photo = note.Photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null) return NotFound();
        db.ProjectNotePhotos.Remove(photo);
        note.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string? DetectImageType(byte[] data)
    {
        if (data.Length > 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff) return "image/jpeg";
        if (data.Length > 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (data.Length > 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            data.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}
