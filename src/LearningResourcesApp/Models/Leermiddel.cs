namespace LearningResourcesApp.Models;

public class Leermiddel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titel { get; set; } = string.Empty;
    public string Beschrijving { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public List<Reactie> Reacties { get; set; } = new List<Reactie>();
    public DateTime AangemaaktOp { get; set; } = DateTime.Now;
}
