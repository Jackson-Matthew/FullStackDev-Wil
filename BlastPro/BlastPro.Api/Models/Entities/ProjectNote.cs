namespace BlastPro.Api.Models.Entities;

public sealed class ProjectNote
{
    public int Id { get; set; }
    public int BlastProjectId { get; set; }
    public string AuthorId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public BlastProject BlastProject { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
    public ICollection<ProjectNotePhoto> Photos { get; set; } = new List<ProjectNotePhoto>();
    public ICollection<ProjectNoteComment> Comments { get; set; } = new List<ProjectNoteComment>();
}

public sealed class ProjectNotePhoto
{
    public int Id { get; set; }
    public int ProjectNoteId { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Data { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public ProjectNote Note { get; set; } = null!;
}

public sealed class ProjectNoteComment
{
    public int Id { get; set; }
    public int ProjectNoteId { get; set; }
    public string AuthorId { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public ProjectNote Note { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
