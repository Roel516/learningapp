using LearningResourcesApp.Data;
using LearningResourcesApp.Models;
using LearningResourcesApp.Repositories.Interfaces;
using LearningResourcesApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearningResourcesApp.Repositories;

public class ReactieRepository : IReactieRepository
{
    private readonly LeermiddelContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ReactieRepository> _logger;

    public ReactieRepository(
        LeermiddelContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<ReactieRepository> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<Reactie>> GetPendingAsync()
    {
        try
        {
            return await _context.Reacties
                .Where(r => !r.IsGoedgekeurd)
                .OrderBy(r => r.AangemaaktOp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending reacties");
            throw;
        }
    }

    public async Task<Reactie?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Reacties.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reactie with ID {ReactieId}", id);
            throw;
        }
    }

    public async Task<Reactie> CreateAsync(Reactie reactie)
    {
        try
        {
            reactie.Id = Guid.NewGuid();
            reactie.AangemaaktOp = _dateTimeProvider.UtcNow;

            _context.Reacties.Add(reactie);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created reactie with ID {ReactieId} for leermiddel {LeermiddelId}",
                reactie.Id, reactie.LeermiddelId);
            return reactie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reactie for leermiddel {LeermiddelId}", reactie.LeermiddelId);
            throw;
        }
    }

    public async Task ApproveAsync(Guid id)
    {
        try
        {
            var reactie = await _context.Reacties.FindAsync(id);
            if (reactie == null)
            {
                throw new KeyNotFoundException($"Reactie with ID {id} not found");
            }

            reactie.IsGoedgekeurd = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Approved reactie with ID {ReactieId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving reactie with ID {ReactieId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var reactie = await _context.Reacties.FindAsync(id);
            if (reactie == null)
            {
                throw new KeyNotFoundException($"Reactie with ID {id} not found");
            }

            _context.Reacties.Remove(reactie);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted reactie with ID {ReactieId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reactie with ID {ReactieId}", id);
            throw;
        }
    }
}
