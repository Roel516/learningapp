using LearningResourcesApp.Data;
using LearningResourcesApp.Helpers;
using LearningResourcesApp.Models;
using LearningResourcesApp.Repositories.Interfaces;
using LearningResourcesApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearningResourcesApp.Repositories;

public class LeermiddelRepository : ILeermiddelRepository
{
    private readonly LeermiddelContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ExceptionHandler _exceptionHandler;
    private readonly ILogger<LeermiddelRepository> _logger;

    public LeermiddelRepository(
        LeermiddelContext context,
        IDateTimeProvider dateTimeProvider,
        ExceptionHandler exceptionHandler,
        ILogger<LeermiddelRepository> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
    }

    public async Task<IEnumerable<Leermiddel>> GetAllAsync()
    {
        return await _exceptionHandler.ExecuteAsync(
            async () => await _context.Leermiddelen
                .Include(l => l.Reacties)
                .OrderByDescending(l => l.AangemaaktOp)
                .ToListAsync(),
            "Error retrieving all leermiddelen");
    }

    public async Task<Leermiddel?> GetByIdAsync(Guid id)
    {
        return await _exceptionHandler.ExecuteAsync(
            async () => await _context.Leermiddelen
                .Include(l => l.Reacties)
                .FirstOrDefaultAsync(l => l.Id == id),
            "Error retrieving leermiddel with ID {LeermiddelId}",
            id);
    }

    public async Task<Leermiddel> CreateAsync(Leermiddel leermiddel)
    {
        return await _exceptionHandler.ExecuteAsync(
            async () =>
            {
                leermiddel.Id = Guid.NewGuid();
                leermiddel.AangemaaktOp = _dateTimeProvider.UtcNow;
                leermiddel.Reacties = new List<Reactie>();

                _context.Leermiddelen.Add(leermiddel);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created leermiddel with ID {LeermiddelId}", leermiddel.Id);
                return leermiddel;
            },
            "Error creating leermiddel");
    }

    public async Task UpdateAsync(Leermiddel leermiddel)
    {
        await _exceptionHandler.ExecuteAsync(
            async () =>
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
            },
            "Error updating leermiddel with ID {LeermiddelId}",
            leermiddel.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _exceptionHandler.ExecuteAsync(
            async () =>
            {
                var leermiddel = await _context.Leermiddelen.FindAsync(id);
                if (leermiddel == null)
                {
                    throw new KeyNotFoundException($"Leermiddel with ID {id} not found");
                }

                _context.Leermiddelen.Remove(leermiddel);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted leermiddel with ID {LeermiddelId}", id);
            },
            "Error deleting leermiddel with ID {LeermiddelId}",
            id);
    }
}

