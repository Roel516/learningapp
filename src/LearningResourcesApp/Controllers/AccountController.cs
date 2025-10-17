using LearningResourcesApp.Authorization;
using LearningResourcesApp.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearningResourcesApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
			CreateBadRequest("Ongeldige invoer");			
        }

        // Check of email al bestaat
        var bestaandeGebruiker = await _userManager.FindByEmailAsync(request.Email);
        if (bestaandeGebruiker != null)
        {
            return CreateBadRequest("Een gebruiker met dit e-mailadres bestaat al");			
        }

        var user = await MaakNieuweGebruiker(request.Naam, request.Email, request.Wachtwoord);
        if (user == null)
        {
            CreateBadRequest("Kon gebruiker niet aanmaken");			
        }

        if (request.IsSelfRegistration) {
			await _signInManager.SignInAsync(user, isPersistent: true);
		}        

        return Ok(MaakSuccesAuthResponse(user, isInterneMedewerker: false));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return CreateBadRequest("Ongeldige invoer");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return CreateBadRequest("Ongeldige email of wachtwoord");			
        }

        var signInSucceeded = await ValideerWachtwoord(user, request.Wachtwoord);
        if (!signInSucceeded)
        {
            return UnauthorizedRequest("Ongeldige email of wachtwoord");
           
        }

        var isInterneMedewerker = await CheckIsInterneMedewerker(user);
        return Ok(MaakSuccesAuthResponse(user, isInterneMedewerker));
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
        return Ok(MaakSuccesAuthResponse(user, isInterneMedewerker));
    }

    [HttpPost("external-login")]
    public async Task<ActionResult<AuthResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ProviderId))
        {
			return CreateBadRequest("Ongeldige externe login gegevens");			
        }

        var user = await ZoekOfMaakGebruiker(request);
        if (user == null)
        {
			return CreateBadRequest("Kon gebruiker niet aanmaken");			
        }

        var loginGekoppeld = await KoppelExterneLogin(user, request);
        if (!loginGekoppeld)
        {
            return CreateBadRequest("Kon externe login niet koppelen");            
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        var isInterneMedewerker = await CheckIsInterneMedewerker(user);
        return Ok(MaakSuccesAuthResponse(user, isInterneMedewerker));
    }    	

    // GET: api/account/users - Lijst van alle gebruikers (alleen voor interne medewerkers)
    [HttpGet("users")]
    [Authorize(Policy = AuthorizationPolicies.InterneMedewerker)]
    public async Task<ActionResult<IEnumerable<Gebruiker>>> GetAllUsers()
    {
        // Haal alle gebruikers op
        var users = _userManager.Users.ToList();
        var userInfoList = new List<Gebruiker>();

        foreach (var user in users)
        {
            var isUserInterneMedewerker = await CheckIsInterneMedewerker(user);

            userInfoList.Add(new Gebruiker
            {
                Id = user.Id,
                Naam = user.UserName ?? string.Empty,
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
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        
        var userClaims = await _userManager.GetClaimsAsync(user);
        var existingClaim = userClaims.FirstOrDefault(c => c.Type == AppClaims.InterneMedewerker);

        if (existingClaim != null)
        {           
            await _userManager.RemoveClaimAsync(user, existingClaim);
        }
        else
        {           
            await _userManager.AddClaimAsync(user, new Claim(AppClaims.InterneMedewerker, "true"));
        }

        return NoContent();
    }

	private BadRequestObjectResult CreateBadRequest(string message)
	{
		return BadRequest(new AuthResponse
		{
			Succes = false,
			Foutmelding = message
		});
	}

	private UnauthorizedObjectResult UnauthorizedRequest(string message)
	{
		return Unauthorized(new AuthResponse
		{
			Succes = false,
			Foutmelding = message
		});
	}

	private AuthResponse MaakSuccesAuthResponse(IdentityUser user, bool isInterneMedewerker)
	{
		return new AuthResponse
		{
			Succes = true,
			Gebruiker = new Gebruiker
			{
				Id = user.Id,
				Naam = user.UserName ?? string.Empty,
				Email = user.Email ?? string.Empty,
				IsInterneMedewerker = isInterneMedewerker
			}
		};
	}

	private async Task<bool> KoppelExterneLogin(IdentityUser user, ExternalLoginRequest request)
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

	private async Task<bool> ValideerWachtwoord(IdentityUser user, string wachtwoord)
	{
		var result = await _signInManager.PasswordSignInAsync(
			user.UserName ?? string.Empty,
			wachtwoord,
			isPersistent: true,
			lockoutOnFailure: false);

		return result.Succeeded;
	}

	private async Task<bool> CheckIsInterneMedewerker(IdentityUser user)
	{
		var claims = await _userManager.GetClaimsAsync(user);
		return claims.Any(c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");
	}

	private async Task<IdentityUser?> ZoekOfMaakGebruiker(ExternalLoginRequest request)
	{
		var user = await _userManager.FindByEmailAsync(request.Email);
		if (user != null)
		{
			return user;
		}

		return await MaakNieuweGebruiker(request.Naam, request.Email);
	}

	private async Task<IdentityUser?> MaakNieuweGebruiker(string userName, string email, string wachtwoord = "")
	{
		var user = new IdentityUser
		{
			UserName = userName,
			Email = email,
			EmailConfirmed = true
		};

		var result = null as IdentityResult;

		if (wachtwoord == "")
		{
			result = await _userManager.CreateAsync(user);
		}
		else
		{
			result = await _userManager.CreateAsync(user, wachtwoord);
		}

		if (!result.Succeeded)
		{
			Console.WriteLine($"Failed to create user {userName}: {string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"))}");
		}

		return result.Succeeded ? user : null;
	}
}
