using LearningResourcesApp.Models;

namespace LearningResourcesApp.Repositories.Interfaces;

public interface ILeermiddelRepository
{
    Task<IEnumerable<Leermiddel>> GetAllAsync();
    Task<Leermiddel?> GetByIdAsync(Guid id);
    Task<Leermiddel> CreateAsync(Leermiddel leermiddel);
    Task UpdateAsync(Leermiddel leermiddel);
    Task DeleteAsync(Guid id);
}
