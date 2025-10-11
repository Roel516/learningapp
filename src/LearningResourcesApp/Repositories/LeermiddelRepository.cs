using LearningResourcesApp.Data;
using LearningResourcesApp.Models;
using LearningResourcesApp.Repositories.Interfaces;
using LearningResourcesApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearningResourcesApp.Repositories;

public class LeermiddelRepository : ILeermiddelRepository
{
    private readonly LeermiddelContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LeermiddelRepository> _logger;

    public LeermiddelRepository(
        LeermiddelContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<LeermiddelRepository> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<Leermiddel>> GetAllAsync()
    {
        try
        {
            return await _context.Leermiddelen
                .Include(l => l.Reacties)
                .OrderByDescending(l => l.AangemaaktOp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all leermiddelen");
            throw;
        }
    }

    public async Task<Leermiddel?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Leermiddelen
                .Include(l => l.Reacties)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leermiddel with ID {LeermiddelId}", id);
            throw;
        }
    }

    public async Task<Leermiddel> CreateAsync(Leermiddel leermiddel)
    {
        try
        {
            leermiddel.Id = Guid.NewGuid();
            leermiddel.AangemaaktOp = _dateTimeProvider.UtcNow;
            leermiddel.Reacties = new List<Reactie>();

            _context.Leermiddelen.Add(leermiddel);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created leermiddel with ID {LeermiddelId}", leermiddel.Id);
            return leermiddel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leermiddel");
            throw;
        }
    }

    public async Task UpdateAsync(Leermiddel leermiddel)
    {
        try
        {
            _context.Entry(leermiddel).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated leermiddel with ID {LeermiddelId}", leermiddel.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating leermiddel with ID {LeermiddelId}", leermiddel.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leermiddel with ID {LeermiddelId}", leermiddel.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var leermiddel = await _context.Leermiddelen.FindAsync(id);
            if (leermiddel == null)
            {
                throw new KeyNotFoundException($"Leermiddel with ID {id} not found");
            }

            _context.Leermiddelen.Remove(leermiddel);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted leermiddel with ID {LeermiddelId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leermiddel with ID {LeermiddelId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _context.Leermiddelen.AnyAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if leermiddel exists with ID {LeermiddelId}", id);
            throw;
        }
    }
}
