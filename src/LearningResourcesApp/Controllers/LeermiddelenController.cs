using LearningResourcesApp.Authorization;
using LearningResourcesApp.Data;
using LearningResourcesApp.Models;
using LearningResourcesApp.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LearningResourcesApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeermiddelenController : ControllerBase
{
    private readonly ILeermiddelRepository _leermiddelRepository;
    private readonly IReactieRepository _reactieRepository;
    private readonly ILogger<LeermiddelenController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeermiddelenController(
        ILeermiddelRepository leermiddelRepository,
        IReactieRepository reactieRepository,
        ILogger<LeermiddelenController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _leermiddelRepository = leermiddelRepository;
        _reactieRepository = reactieRepository;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: api/leermiddelen
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Leermiddel>>> GetLeermiddelen()
    {
        try
        {
            var leermiddelen = (await _leermiddelRepository.GetAllAsync()).ToList();

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
            _logger.LogError(ex, "Error retrieving leermiddelen");
            return StatusCode(500, "Er is een fout opgetreden bij het ophalen van leermiddelen");
        }
    }

    // GET: api/leermiddelen/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Leermiddel>> GetLeermiddel(Guid id)
    {
        try
        {
            var leermiddel = await _leermiddelRepository.GetByIdAsync(id);

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
            _logger.LogError(ex, "Error retrieving leermiddel {LeermiddelId}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het ophalen van het leermiddel");
        }
    }

    // POST: api/leermiddelen
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<ActionResult<Leermiddel>> CreateLeermiddel(Leermiddel leermiddel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdLeermiddel = await _leermiddelRepository.CreateAsync(leermiddel);

            return CreatedAtAction(nameof(GetLeermiddel), new { id = createdLeermiddel.Id }, createdLeermiddel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leermiddel");
            return StatusCode(500, "Er is een fout opgetreden bij het aanmaken van het leermiddel");
        }
    }

    // PUT: api/leermiddelen/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> UpdateLeermiddel(Guid id, Leermiddel leermiddel)
    {
        if (id != leermiddel.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _leermiddelRepository.UpdateAsync(leermiddel);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leermiddel {LeermiddelId}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het updaten van het leermiddel");
        }
    }

    // DELETE: api/leermiddelen/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> DeleteLeermiddel(Guid id)
    {
        try
        {
            await _leermiddelRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leermiddel {LeermiddelId}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het verwijderen van het leermiddel");
        }
    }

    // GET: api/leermiddelen/reacties/pending
    [HttpGet("reacties/pending")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<ActionResult<IEnumerable<Reactie>>> GetPendingReacties()
    {
        try
        {
            var pendingReacties = await _reactieRepository.GetPendingAsync();
            return Ok(pendingReacties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending reacties");
            return StatusCode(500, "Er is een fout opgetreden bij het ophalen van pending reacties");
        }
    }

    // PUT: api/leermiddelen/reacties/{id}/approve
    [HttpPut("reacties/{id}/approve")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> ApproveReactie(Guid id)
    {
        try
        {
            await _reactieRepository.ApproveAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving reactie {ReactieId}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het goedkeuren van de reactie");
        }
    }

    // DELETE: api/leermiddelen/reacties/{id}
    [HttpDelete("reacties/{id}")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> DeleteReactie(Guid id)
    {
        try
        {
            await _reactieRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reactie {ReactieId}", id);
            return StatusCode(500, "Er is een fout opgetreden bij het verwijderen van de reactie");
        }
    }

    // POST: api/leermiddelen/{id}/reacties
    [HttpPost("{id}/reacties")]
    public async Task<ActionResult<Reactie>> AddReactie(Guid id, Reactie reactie)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var leermiddel = await _leermiddelRepository.GetByIdAsync(id);
            if (leermiddel == null)
            {
                return NotFound();
            }

            reactie.LeermiddelId = id;

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

            var createdReactie = await _reactieRepository.CreateAsync(reactie);

            return CreatedAtAction(nameof(GetLeermiddel), new { id = leermiddel.Id }, createdReactie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reactie to leermiddel {LeermiddelId}", id);
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
}
