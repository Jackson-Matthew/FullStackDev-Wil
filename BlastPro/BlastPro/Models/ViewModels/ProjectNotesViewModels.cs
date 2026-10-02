using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels;

public sealed class ProjectNotesPageViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public List<ProjectNoteViewModel> Notes { get; set; } = [];
}

public sealed class ProjectNoteViewModel
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<ProjectNotePhotoViewModel> Photos { get; set; } = [];
    public List<ProjectNoteCommentViewModel> Comments { get; set; } = [];
}

public sealed class ProjectNotePhotoViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
}

public sealed class ProjectNoteCommentViewModel
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class SaveNoteForm
{
    public int ProjectId { get; set; }
    public int NoteId { get; set; }
    [StringLength(150)] public string Title { get; set; } = "";
    [StringLength(10000)] public string Body { get; set; } = "";
}
