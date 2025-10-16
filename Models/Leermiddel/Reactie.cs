using System.ComponentModel.DataAnnotations;

namespace LearningResourcesApp.Models.Leermiddel;

public class Reactie
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Gebruiker ID is verplicht")]
    public string GebruikerId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gebruikersnaam is verplicht")]
    [StringLength(100, ErrorMessage = "Gebruikersnaam mag maximaal 100 tekens zijn")]
    public string Gebruikersnaam { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tekst is verplicht")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Tekst moet tussen 1 en 1000 tekens zijn")]
    public string Tekst { get; set; } = string.Empty;

    public DateTime AangemaaktOp { get; set; }

    public bool IsGoedgekeurd { get; set; } = false;

    // Foreign key voor relatie met Leermiddel
    [Required]
    public Guid LeermiddelId { get; set; }
}
