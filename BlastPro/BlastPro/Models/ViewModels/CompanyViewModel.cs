using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels;

public sealed class CompanyViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? RegistrationNumber { get; set; }
    [Required, EmailAddress, StringLength(254), Display(Name = "Contact email")] public string ContactEmail { get; set; } = "";
    [Required, Phone, StringLength(30), Display(Name = "Contact phone")] public string ContactPhone { get; set; } = "";
    [StringLength(300)] public string? Address { get; set; }
    public int ActiveBlasterCount { get; set; }
    public int BlasterLimit { get; set; } = 5;
    public List<BlasterViewModel> Blasters { get; set; } = [];
}
