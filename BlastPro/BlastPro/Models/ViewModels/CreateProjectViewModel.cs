using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels.Projects
{
    public class CreateProjectViewModel
    {
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(100)]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Site location is required.")]
        [StringLength(150)]
        [Display(Name = "Site Location")]
        public string SiteLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a blast type.")]
        [Display(Name = "Blast Type")]
        public string BlastType { get; set; } = string.Empty;
    }
}
