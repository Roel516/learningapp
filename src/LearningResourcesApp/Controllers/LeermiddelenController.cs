using LearningResourcesApp.Data;
using LearningResourcesApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningResourcesApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeermiddelenController : ControllerBase
{
    private readonly LeermiddelContext _context;
    private readonly ILogger<LeermiddelenController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeermiddelenController(
        LeermiddelContext context,
        ILogger<LeermiddelenController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: api/leermiddelen
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Leermiddel>>> GetLeermiddelen()
    {
        try
        {
            var leermiddelen = await _context.Leermiddelen
                .Include(l => l.Reacties)
                .OrderByDescending(l => l.AangemaaktOp)
                .ToListAsync();

            // Controleer of gebruiker interne medewerker is
            var isInterneMedewerker = await IsInterneMedewerker();

            // Filter reacties voor niet-interne gebruikers
            if (!isInterneMedewerker)
            {
                // Haal huidige gebruiker ID op
                string? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    currentUserId = user?.Id;
                }

                foreach (var leermiddel in leermiddelen)
                {
                    // Toon goedgekeurde reacties + eigen niet-goedgekeurde reacties
                    leermiddel.Reacties = leermiddel.Reacties
                        .Where(r => r.IsGoedgekeurd || r.GebruikerId == currentUserId)
                        .ToList();
                }
            }

            return Ok(leermiddelen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen leermiddelen");
            return StatusCode(500, "Er is een fout opgetreden bij het ophalen van leermiddelen");
        }
    }

    // GET: api/leermiddelen/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Leermiddel>> GetLeermiddel(Guid id)
    {
        try
        {
            var leermiddel = await _context.Leermiddelen
                .Include(l => l.Reacties)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leermiddel == null)
            {
                return NotFound();
            }

            // Controleer of gebruiker interne medewerker is
            var isInterneMedewerker = await IsInterneMedewerker();

            // Filter reacties voor niet-interne gebruikers
            if (!isInterneMedewerker)
            {
                // Haal huidige gebruiker ID op
                string? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    currentUserId = user?.Id;
                }

                // Toon goedgekeurde reacties + eigen niet-goedgekeurde reacties
                leermiddel.Reacties = leermiddel.Reacties
                    .Where(r => r.IsGoedgekeurd || r.GebruikerId == currentUserId)
                    .ToList();
            }

            return Ok(leermiddel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen leermiddel {Id}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het ophalen van het leermiddel");
        }
    }

    // POST: api/leermiddelen
    [HttpPost]
    public async Task<ActionResult<Leermiddel>> CreateLeermiddel(Leermiddel leermiddel)
    {
        try
        {
            // Alleen interne medewerkers kunnen leermiddelen aanmaken
            var isInterneMedewerker = await IsInterneMedewerker();
            if (!isInterneMedewerker)
            {
                return Forbid();
            }

            leermiddel.Id = Guid.NewGuid();
            leermiddel.AangemaaktOp = DateTime.Now;
            leermiddel.Reacties = new List<Reactie>();

            _context.Leermiddelen.Add(leermiddel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeermiddel), new { id = leermiddel.Id }, leermiddel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij aanmaken leermiddel");
            return StatusCode(500, "Er is een fout opgetreden bij het aanmaken van het leermiddel");
        }
    }

    // PUT: api/leermiddelen/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLeermiddel(Guid id, Leermiddel leermiddel)
    {
        // Alleen interne medewerkers kunnen leermiddelen updaten
        var isInterneMedewerker = await IsInterneMedewerker();
        if (!isInterneMedewerker)
        {
            return Forbid();
        }

        if (id != leermiddel.Id)
        {
            return BadRequest();
        }

        try
        {
            _context.Entry(leermiddel).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await LeermiddelExists(id))
            {
                return NotFound();
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij updaten leermiddel {Id}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het updaten van het leermiddel");
        }
    }

    // DELETE: api/leermiddelen/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLeermiddel(Guid id)
    {
        // Alleen interne medewerkers kunnen leermiddelen verwijderen
        var isInterneMedewerker = await IsInterneMedewerker();
        if (!isInterneMedewerker)
        {
            return Forbid();
        }

        try
        {
            var leermiddel = await _context.Leermiddelen.FindAsync(id);
            if (leermiddel == null)
            {
                return NotFound();
            }

            _context.Leermiddelen.Remove(leermiddel);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij verwijderen leermiddel {Id}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het verwijderen van het leermiddel");
        }
    }

    // GET: api/leermiddelen/reacties/pending
    [HttpGet("reacties/pending")]
    public async Task<ActionResult<IEnumerable<Reactie>>> GetPendingReacties()
    {
        try
        {
            // Alleen interne medewerkers kunnen pending reacties zien
            var isInterneMedewerker = await IsInterneMedewerker();
            if (!isInterneMedewerker)
            {
                return Forbid();
            }

            var pendingReacties = await _context.Reacties
                .Where(r => !r.IsGoedgekeurd)
                .OrderBy(r => r.AangemaaktOp)
                .ToListAsync();

            return Ok(pendingReacties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen pending reacties");
            return StatusCode(500, "Er is een fout opgetreden bij het ophalen van pending reacties");
        }
    }

    // PUT: api/leermiddelen/reacties/{id}/approve
    [HttpPut("reacties/{id}/approve")]
    public async Task<IActionResult> ApproveReactie(Guid id)
    {
        try
        {
            // Alleen interne medewerkers kunnen reacties goedkeuren
            var isInterneMedewerker = await IsInterneMedewerker();
            if (!isInterneMedewerker)
            {
                return Forbid();
            }

            var reactie = await _context.Reacties.FindAsync(id);
            if (reactie == null)
            {
                return NotFound();
            }

            reactie.IsGoedgekeurd = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij goedkeuren reactie {Id}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het goedkeuren van de reactie");
        }
    }

    // DELETE: api/leermiddelen/reacties/{id}
    [HttpDelete("reacties/{id}")]
    public async Task<IActionResult> DeleteReactie(Guid id)
    {
        try
        {
            // Alleen interne medewerkers kunnen reacties verwijderen
            var isInterneMedewerker = await IsInterneMedewerker();
            if (!isInterneMedewerker)
            {
                return Forbid();
            }

            var reactie = await _context.Reacties.FindAsync(id);
            if (reactie == null)
            {
                return NotFound();
            }

            _context.Reacties.Remove(reactie);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij verwijderen reactie {Id}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het verwijderen van de reactie");
        }
    }

    // POST: api/leermiddelen/{id}/reacties
    [HttpPost("{id}/reacties")]
    public async Task<ActionResult<Reactie>> AddReactie(Guid id, Reactie reactie)
    {
        try
        {
            var leermiddel = await _context.Leermiddelen
                .Include(l => l.Reacties)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leermiddel == null)
            {
                return NotFound();
            }

            reactie.Id = Guid.NewGuid();
            reactie.LeermiddelId = id;
            reactie.AangemaaktOp = DateTime.Now;

            // Controleer of gebruiker interne medewerker is
            var isInterneMedewerker = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var claims = await _userManager.GetClaimsAsync(user);
                    isInterneMedewerker = claims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");
                }
            }

            // Reacties van interne medewerkers worden automatisch goedgekeurd
            reactie.IsGoedgekeurd = isInterneMedewerker;

            _context.Reacties.Add(reactie);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeermiddel), new { id = leermiddel.Id }, reactie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij toevoegen reactie aan leermiddel {Id}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het toevoegen van de reactie");
        }
    }

    private async Task<bool> IsInterneMedewerker()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return false;
        }

        var claims = await _userManager.GetClaimsAsync(user);
        return claims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");
    }

    private async Task<bool> LeermiddelExists(Guid id)
    {
        return await _context.Leermiddelen.AnyAsync(e => e.Id == id);
    }
}
