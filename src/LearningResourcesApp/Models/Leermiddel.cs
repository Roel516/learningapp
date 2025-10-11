using System.ComponentModel.DataAnnotations;

namespace LearningResourcesApp.Models;

public class Leermiddel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Titel is verplicht")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Titel moet tussen 3 en 200 tekens zijn")]
    public string Titel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Beschrijving is verplicht")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Beschrijving moet tussen 10 en 2000 tekens zijn")]
    public string Beschrijving { get; set; } = string.Empty;

    [Url(ErrorMessage = "Link moet een geldige URL zijn")]
    [StringLength(500, ErrorMessage = "Link mag maximaal 500 tekens zijn")]
    public string Link { get; set; } = string.Empty;

    public List<Reactie> Reacties { get; set; } = new List<Reactie>();

    public DateTime AangemaaktOp { get; set; }
}
