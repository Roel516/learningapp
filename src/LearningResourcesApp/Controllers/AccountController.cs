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
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Een gebruiker met dit e-mailadres bestaat al"
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Naam = request.Naam,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Wachtwoord);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Foutmelding = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        // Nieuwe gebruikers krijgen GEEN InterneMedewerker claim
        // Deze kan later worden toegevoegd via gebruikersbeheer

        // Automatisch inloggen na registratie
        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(new AuthResponse
        {
            Succes = true,
            Gebruiker = new UserInfo
            {
                Id = user.Id,
                Naam = user.Naam ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsInterneMedewerker = false
            }
        });
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

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? string.Empty,
            request.Wachtwoord,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized(new AuthResponse
            {
                Succes = false,
                Foutmelding = "Ongeldige email of wachtwoord"
            });
        }

        // Check of gebruiker InterneMedewerker claim heeft
        var claims = await _userManager.GetClaimsAsync(user);
        var isInterneMedewerker = claims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");

        return Ok(new AuthResponse
        {
            Succes = true,
            Gebruiker = new UserInfo
            {
                Id = user.Id,
                Naam = user.Naam ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsInterneMedewerker = isInterneMedewerker
            }
        });
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

        // Check of gebruiker InterneMedewerker claim heeft
        var userClaims = await _userManager.GetClaimsAsync(user);
        var isInterneMedewerker = userClaims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");

        return Ok(new AuthResponse
        {
            Succes = true,
            Gebruiker = new UserInfo
            {
                Id = user.Id,
                Naam = user.Naam ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsInterneMedewerker = isInterneMedewerker
            }
        });
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

        // Zoek gebruiker op email
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            // Maak nieuwe gebruiker aan
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                Naam = request.Naam,
                EmailConfirmed = true // Google heeft email al geverifieerd
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return BadRequest(new AuthResponse
                {
                    Succes = false,
                    Foutmelding = "Kon gebruiker niet aanmaken"
                });
            }

            // Koppel externe login
            var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(
                request.Provider,
                request.ProviderId,
                request.Provider));

            if (!addLoginResult.Succeeded)
            {
                return BadRequest(new AuthResponse
                {
                    Succes = false,
                    Foutmelding = "Kon externe login niet koppelen"
                });
            }
        }
        else
        {
            // Controleer of externe login al gekoppeld is
            var logins = await _userManager.GetLoginsAsync(user);
            var externalLogin = logins.FirstOrDefault(l => l.LoginProvider == request.Provider && l.ProviderKey == request.ProviderId);

            if (externalLogin == null)
            {
                // Koppel nieuwe externe login aan bestaande gebruiker
                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(
                    request.Provider,
                    request.ProviderId,
                    request.Provider));

                if (!addLoginResult.Succeeded)
                {
                    return BadRequest(new AuthResponse
                    {
                        Succes = false,
                        Foutmelding = "Kon externe login niet koppelen"
                    });
                }
            }
        }

        // Log gebruiker in
        await _signInManager.SignInAsync(user, isPersistent: true);

        // Check of gebruiker InterneMedewerker claim heeft (Google gebruikers hebben deze niet)
        var externalUserClaims = await _userManager.GetClaimsAsync(user);
        var isInterneMedewerker = externalUserClaims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");

        return Ok(new AuthResponse
        {
            Succes = true,
            Gebruiker = new UserInfo
            {
                Id = user.Id,
                Naam = user.Naam ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsInterneMedewerker = isInterneMedewerker
            }
        });
    }

    // GET: api/account/users - Lijst van alle gebruikers (alleen voor interne medewerkers)
    [HttpGet("users")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserInfo>>> GetAllUsers()
    {
        // Controleer of huidige gebruiker interne medewerker is
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var currentUserClaims = await _userManager.GetClaimsAsync(currentUser);
        var isInterneMedewerker = currentUserClaims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");

        if (!isInterneMedewerker)
        {
            return Forbid();
        }

        // Haal alle gebruikers op
        var users = _userManager.Users.ToList();
        var userInfoList = new List<UserInfo>();

        foreach (var user in users)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var isUserInterneMedewerker = claims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");

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
    [Authorize]
    public async Task<IActionResult> ToggleInternalEmployee(string userId)
    {
        // Controleer of huidige gebruiker interne medewerker is
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var currentUserClaims = await _userManager.GetClaimsAsync(currentUser);
        var isInterneMedewerker = currentUserClaims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");

        if (!isInterneMedewerker)
        {
            return Forbid();
        }

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
