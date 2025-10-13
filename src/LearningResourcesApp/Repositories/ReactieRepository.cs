using LearningResourcesApp.Data;
using LearningResourcesApp.Helpers;
using LearningResourcesApp.Models;
using LearningResourcesApp.Repositories.Interfaces;
using LearningResourcesApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearningResourcesApp.Repositories;

public class ReactieRepository : IReactieRepository
{
    private readonly LeermiddelContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ExceptionHandler _exceptionHandler;
    private readonly ILogger<ReactieRepository> _logger;

    public ReactieRepository(
        LeermiddelContext context,
        IDateTimeProvider dateTimeProvider,
        ExceptionHandler exceptionHandler,
        ILogger<ReactieRepository> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
    }

    public async Task<IEnumerable<Reactie>> GetPendingAsync()
    {
        return await _exceptionHandler.ExecuteAsync(
            async () => await _context.Reacties
                .Where(r => !r.IsGoedgekeurd)
                .OrderBy(r => r.AangemaaktOp)
                .ToListAsync(),
            "Error retrieving pending reacties");
    }

    public async Task<Reactie?> GetByIdAsync(Guid id)
    {
        return await _exceptionHandler.ExecuteAsync(
            async () => await _context.Reacties.FindAsync(id),
            "Error retrieving reactie with ID {ReactieId}",
            id);
    }

    public async Task<Reactie> CreateAsync(Reactie reactie)
    {
        return await _exceptionHandler.ExecuteAsync(
            async () =>
            {
                reactie.Id = Guid.NewGuid();
                reactie.AangemaaktOp = _dateTimeProvider.UtcNow;

                _context.Reacties.Add(reactie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created reactie with ID {ReactieId} for leermiddel {LeermiddelId}",
                    reactie.Id, reactie.LeermiddelId);
                return reactie;
            },
            "Error creating reactie for leermiddel {LeermiddelId}",
            reactie.LeermiddelId);
    }

    public async Task ApproveAsync(Guid id)
    {
        await _exceptionHandler.ExecuteAsync(
            async () =>
            {
                var reactie = await _context.Reacties.FindAsync(id);
                if (reactie == null)
                {
                    throw new KeyNotFoundException($"Reactie with ID {id} not found");
                }

                reactie.IsGoedgekeurd = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Approved reactie with ID {ReactieId}", id);
            },
            "Error approving reactie with ID {ReactieId}",
            id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _exceptionHandler.ExecuteAsync(
            async () =>
            {
                var reactie = await _context.Reacties.FindAsync(id);
                if (reactie == null)
                {
                    throw new KeyNotFoundException($"Reactie with ID {id} not found");
                }

                _context.Reacties.Remove(reactie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted reactie with ID {ReactieId}", id);
            },
            "Error deleting reactie with ID {ReactieId}",
            id);
    }
}
