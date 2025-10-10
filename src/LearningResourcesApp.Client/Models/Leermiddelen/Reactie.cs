namespace LearningResourcesApp.Client.Models.Leermiddelen;

public class Reactie
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GebruikerId { get; set; } = string.Empty;
    public string Gebruikersnaam { get; set; } = string.Empty;
    public string Tekst { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.Now;
    public bool IsGoedgekeurd { get; set; } = false;

    // Foreign key voor relatie met Leermiddel
    public Guid LeermiddelId { get; set; }
}
