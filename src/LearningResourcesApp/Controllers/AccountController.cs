using LearningResourcesApp.Authorization;
using LearningResourcesApp.Helpers;
using LearningResourcesApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearningResourcesApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Ongeldige invoer"
            });
        }

        // Check of email al bestaat
        var bestaandeGebruiker = await _userManager.FindByEmailAsync(request.Email);
        if (bestaandeGebruiker != null)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Een gebruiker met dit e-mailadres bestaat al"
            });
        }

        var user = await MaakNieuweGebruiker(request);
        if (user == null)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Kon gebruiker niet aanmaken"
            });
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(MaakAuthResponse(user, isInterneMedewerker: false));
    }

    private async Task<ApplicationUser?> MaakNieuweGebruiker(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Naam = request.Naam,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Wachtwoord);
        return result.Succeeded ? user : null;
    }

    private AuthResponse MaakAuthResponse(ApplicationUser user, bool isInterneMedewerker)
    {
        return new AuthResponse
        {
            Succes = true,
            Gebruiker = new UserInfo
            {
                Id = user.Id,
                Naam = user.Naam ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsInterneMedewerker = isInterneMedewerker
            }
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Ongeldige invoer"
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Ongeldige email of wachtwoord"
            });
        }

        var signInSucceeded = await ValideerWachtwoord(user, request.Wachtwoord);
        if (!signInSucceeded)
        {
            return Unauthorized(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Ongeldige email of wachtwoord"
            });
        }

        var isInterneMedewerker = await CheckIsInterneMedewerker(user);
        return Ok(MaakAuthResponse(user, isInterneMedewerker));
    }

    private async Task<bool> ValideerWachtwoord(ApplicationUser user, string wachtwoord)
    {
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? string.Empty,
            wachtwoord,
            isPersistent: true,
            lockoutOnFailure: false);

        return result.Succeeded;
    }

    private async Task<bool> CheckIsInterneMedewerker(ApplicationUser user)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        return claims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new AuthResponse { Succes = true });
    }

    [HttpGet("current-user")]
    public async Task<ActionResult<AuthResponse>> GetCurrentUser()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Ok(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Niet ingelogd"
            });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Ok(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Gebruiker niet gevonden"
            });
        }


        var isInterneMedewerker = await CheckIsInterneMedewerker(user);
        return Ok(MaakAuthResponse(user, isInterneMedewerker));
    }

    [HttpPost("external-login")]
    public async Task<ActionResult<AuthResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ProviderId))
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Ongeldige externe login gegevens"
            });
        }

        var user = await ZoekOfMaakGebruiker(request);
        if (user == null)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Kon gebruiker niet aanmaken"
            });
        }

        var loginGekoppeld = await KoppelExterneLogin(user, request);
        if (!loginGekoppeld)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Kon externe login niet koppelen"
            });
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        var isInterneMedewerker = await CheckIsInterneMedewerker(user);
        return Ok(MaakAuthResponse(user, isInterneMedewerker));
    }

    private async Task<ApplicationUser?> ZoekOfMaakGebruiker(ExternalLoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            return user;
        }

        // Maak nieuwe gebruiker aan voor externe login
        var nieuweGebruiker = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Naam = request.Naam,
            EmailConfirmed = true // Google heeft email al geverifieerd
        };

        var createResult = await _userManager.CreateAsync(nieuweGebruiker);
        return createResult.Succeeded ? nieuweGebruiker : null;
    }

    private async Task<bool> KoppelExterneLogin(ApplicationUser user, ExternalLoginRequest request)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        var bestaandeLogin = logins.FirstOrDefault(l =>
            l.LoginProvider == request.Provider &&
            l.ProviderKey == request.ProviderId);

        if (bestaandeLogin != null)
        {
            return true; // Login is al gekoppeld
        }

        var loginInfo = new UserLoginInfo(request.Provider, request.ProviderId, request.Provider);
        var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
        return addLoginResult.Succeeded;
    }

    // GET: api/account/users - Lijst van alle gebruikers (alleen voor interne medewerkers)
    [HttpGet("users")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<ActionResult<IEnumerable<UserInfo>>> GetAllUsers()
    {
        // Haal alle gebruikers op
        var users = _userManager.Users.ToList();
        var userInfoList = new List<UserInfo>();

        foreach (var user in users)
        {
            var isUserInterneMedewerker = await CheckIsInterneMedewerker(user);

            userInfoList.Add(new UserInfo
            {
                Id = user.Id,
                Naam = user.Naam ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsInterneMedewerker = isUserInterneMedewerker
            });
        }

        return Ok(userInfoList);
    }

    // PUT: api/account/users/{userId}/toggle-internal-employee
    [HttpPut("users/{userId}/toggle-internal-employee")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<IActionResult> ToggleInternalEmployee(string userId)
    {
        // Haal de gebruiker op die gewijzigd moet worden
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Check of gebruiker al InterneMedewerker claim heeft
        var userClaims = await _userManager.GetClaimsAsync(user);
        var existingClaim = userClaims.FirstOrDefault(c => c.Type == AppClaims.InterneMedewerker);

        if (existingClaim != null)
        {
            // Verwijder de claim (uitschakelen)
            await _userManager.RemoveClaimAsync(user, existingClaim);
        }
        else
        {
            // Voeg de claim toe (inschakelen)
            await _userManager.AddClaimAsync(user, new Claim(AppClaims.InterneMedewerker, "true"));
        }

        return NoContent();
    }
}
