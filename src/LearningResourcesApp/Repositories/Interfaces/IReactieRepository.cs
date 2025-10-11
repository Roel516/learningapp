using LearningResourcesApp.Models;

namespace LearningResourcesApp.Repositories.Interfaces;

public interface IReactieRepository
{
    Task<IEnumerable<Reactie>> GetPendingAsync();
    Task<Reactie?> GetByIdAsync(Guid id);
    Task<Reactie> CreateAsync(Reactie reactie);
    Task ApproveAsync(Guid id);
    Task DeleteAsync(Guid id);
}
