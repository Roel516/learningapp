using LearningResourcesApp.Authorization;
using LearningResourcesApp.Data;
using LearningResourcesApp.Helpers;
using LearningResourcesApp.Models.Auth;
using LearningResourcesApp.Models.Leermiddel;
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
    private readonly ControllerExceptionHandler _exceptionHandler;
    private readonly UserManager<IdentityUser> _userManager;

    public LeermiddelenController(
        ILeermiddelRepository leermiddelRepository,
        IReactieRepository reactieRepository,
        ControllerExceptionHandler exceptionHandler,
        UserManager<IdentityUser> userManager)
    {
        _leermiddelRepository = leermiddelRepository;
        _reactieRepository = reactieRepository;
        _exceptionHandler = exceptionHandler;
        _userManager = userManager;
    }

    // GET: api/leermiddelen
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Leermiddel>>> GetLeermiddelen()
    {
        return await _exceptionHandler.ExecuteAsync<IEnumerable<Leermiddel>>(async () =>
        {
            var leermiddelen = (await _leermiddelRepository.GetAllAsync()).ToList();

            // Controleer of gebruiker interne medewerker is
            var isInterneMedewerker = await IsInterneMedewerker();

            // Filter reacties voor niet-interne gebruikers
            if (!isInterneMedewerker)
            {
                var currentUserId = await GetCurrentUserid();

                foreach (var leermiddel in leermiddelen)
                {
                    // Toon goedgekeurde reacties + eigen niet-goedgekeurde reacties
                    leermiddel.Reacties = leermiddel.Reacties
                        .Where(r => r.IsGoedgekeurd || r.GebruikerId == currentUserId)
                        .ToList();
                }
            }

            return Ok(leermiddelen);
        },
        "Error retrieving leermiddelen",
        "Er is een fout opgetreden bij het ophalen van leermiddelen");
    }

    // GET: api/leermiddelen/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Leermiddel>> GetLeermiddel(Guid id)
    {
        return await _exceptionHandler.ExecuteAsync<Leermiddel>(async () =>
        {
            var leermiddel = await _leermiddelRepository.GetByIdAsync(id);

            if (leermiddel == null)
            {
                return NotFound();
            }

            leermiddel = await FilterReactiesVoorInterneGebruiker(leermiddel);

            return Ok(leermiddel);
        },
        "Error retrieving leermiddel {LeermiddelId}",
        "Er is een fout opgetreden bij het ophalen van het leermiddel",
        id);
    }    

    // POST: api/leermiddelen
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<ActionResult<Leermiddel>> CreateLeermiddel(Leermiddel leermiddel)
    {
        return await _exceptionHandler.ExecuteAsync<Leermiddel>(async () =>
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdLeermiddel = await _leermiddelRepository.CreateAsync(leermiddel);

            return CreatedAtAction(nameof(GetLeermiddel), new { id = createdLeermiddel.Id }, createdLeermiddel);
        },
        "Error creating leermiddel",
        "Er is een fout opgetreden bij het aanmaken van het leermiddel");
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

        return await _exceptionHandler.ExecuteAsync(async () =>
        {
            await _leermiddelRepository.UpdateAsync(leermiddel);
            return NoContent();
        },
        "Error updating leermiddel {LeermiddelId}",
        "Er is een fout opgetreden bij het updaten van het leermiddel",
        id);
    }

    // DELETE: api/leermiddelen/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> DeleteLeermiddel(Guid id)
    {
        return await _exceptionHandler.ExecuteAsync(async () =>
        {
            await _leermiddelRepository.DeleteAsync(id);
            return NoContent();
        },
        "Error deleting leermiddel {LeermiddelId}",
        "Er is een fout opgetreden bij het verwijderen van het leermiddel",
        id);
    }

    // GET: api/leermiddelen/reacties/pending
    [HttpGet("reacties/pending")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<ActionResult<IEnumerable<Reactie>>> GetPendingReacties()
    {
        return await _exceptionHandler.ExecuteAsync<IEnumerable<Reactie>>(async () =>
        {
            var pendingReacties = await _reactieRepository.GetPendingAsync();
            return Ok(pendingReacties);
        },
        "Error retrieving pending reacties",
        "Er is een fout opgetreden bij het ophalen van pending reacties");
    }

    // PUT: api/leermiddelen/reacties/{id}/approve
    [HttpPut("reacties/{id}/approve")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> ApproveReactie(Guid id)
    {
        return await _exceptionHandler.ExecuteAsync(async () =>
        {
            await _reactieRepository.ApproveAsync(id);
            return NoContent();
        },
        "Error approving reactie {ReactieId}",
        "Er is een fout opgetreden bij het goedkeuren van de reactie",
        id);
    }

    // DELETE: api/leermiddelen/reacties/{id}
    [HttpDelete("reacties/{id}")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> DeleteReactie(Guid id)
    {
        return await _exceptionHandler.ExecuteAsync(async () =>
        {
            await _reactieRepository.DeleteAsync(id);
            return NoContent();
        },
        "Error deleting reactie {ReactieId}",
        "Er is een fout opgetreden bij het verwijderen van de reactie",
        id);
    }

    // POST: api/leermiddelen/{id}/reacties
    [HttpPost("{id}/reacties")]
    public async Task<ActionResult<Reactie>> AddReactie(Guid id, Reactie reactie)
    {
        return await _exceptionHandler.ExecuteAsync<Reactie>(async () =>
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

            // Reacties van interne medewerkers worden automatisch goedgekeurd
            reactie.IsGoedgekeurd = await IsInterneMedewerker();

            var createdReactie = await _reactieRepository.CreateAsync(reactie);

            return CreatedAtAction(nameof(GetLeermiddel), new { id = leermiddel.Id }, createdReactie);
        },
        "Error adding reactie to leermiddel {LeermiddelId}",
        "Er is een fout opgetreden bij het toevoegen van de reactie",
        id);
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
	private async Task<Leermiddel> FilterReactiesVoorInterneGebruiker(Leermiddel leermiddel)
	{
		// Controleer of gebruiker interne medewerker is
		var isInterneMedewerker = await IsInterneMedewerker();

		// Filter reacties voor niet-interne gebruikers
		if (!isInterneMedewerker)
		{
			var currentUserId = await GetCurrentUserid();

			// Toon goedgekeurde reacties + eigen niet-goedgekeurde reacties
			leermiddel.Reacties = leermiddel.Reacties
				.Where(r => r.IsGoedgekeurd || r.GebruikerId == currentUserId)
				.ToList();
		}

		return leermiddel;
	}

	private async Task<string?> GetCurrentUserid()
	{
		// Haal huidige gebruiker ID op
		string? currentUserId = null;
		if (User.Identity?.IsAuthenticated == true)
		{
			var user = await _userManager.GetUserAsync(User);
			currentUserId = user?.Id;
		}

		return currentUserId;
	}
}
