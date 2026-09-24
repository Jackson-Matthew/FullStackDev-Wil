using BlastPro.Mvc.Models.Dtos;

namespace BlastPro.Mvc.Models.ViewModels.Projects;

public sealed class ProjectDetailsViewModel
{
    public ProjectDetailDto Project { get; set; } = new();
    public List<BlastHoleViewModel> Holes { get; set; } = new();
}
