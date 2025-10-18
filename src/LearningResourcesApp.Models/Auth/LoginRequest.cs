using System.ComponentModel.DataAnnotations;

namespace LearningResourcesApp.Models.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is verplicht")]
    [EmailAddress(ErrorMessage = "Ongeldig emailadres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wachtwoord is verplicht")]
    public string Wachtwoord { get; set; } = string.Empty;

    /// <summary>
    /// Set to true to create a cookie session (for browser-based clients like Blazor).
    /// Set to false for JWT-only authentication (for external API consumers).
    /// Default is true for backward compatibility.
    /// </summary>
    public bool UseCookieAuth { get; set; } = true;
}
