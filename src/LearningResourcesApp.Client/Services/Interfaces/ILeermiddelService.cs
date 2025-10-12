using LearningResourcesApp.Client.Models.Leermiddelen;

namespace LearningResourcesApp.Client.Services.Interfaces;

public interface ILeermiddelService
{
    Task<List<Leermiddel>> HaalAlleLeermiddelenOp();
    Task<Leermiddel?> HaalLeermiddelOpMetId(Guid id);
    Task<bool> VoegLeermiddelToe(Leermiddel leermiddel);
    Task<bool> WijzigLeermiddel(Leermiddel leermiddel);
    Task<bool> VoegReactieToe(Guid leermiddelId, Reactie reactie);
    Task<bool> VerwijderLeermiddel(Guid id);
}
