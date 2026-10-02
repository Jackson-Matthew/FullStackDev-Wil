using System.ComponentModel.DataAnnotations;

namespace BlastPro.Api.Models.Dtos;

public sealed record ProjectNoteDto(int Id, int ProjectId, string Title, string Body,
    string AuthorName, DateTime CreatedAtUtc, DateTime UpdatedAtUtc,
    List<ProjectNotePhotoDto> Photos, List<ProjectNoteCommentDto> Comments);

public sealed record ProjectNotePhotoDto(int Id, string FileName, string ContentType, DateTime CreatedAtUtc);
public sealed record ProjectNoteCommentDto(int Id, string Body, string AuthorName, DateTime CreatedAtUtc);

public sealed class SaveProjectNoteRequest
{
    [StringLength(150)] public string Title { get; set; } = "";
    [StringLength(10000)] public string Body { get; set; } = "";
}

public sealed class AddProjectNoteCommentRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Body { get; set; } = "";
}
