using System.ComponentModel.DataAnnotations;

namespace LearningResourcesApp.Models.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Naam is verplicht")]
    public string Naam { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is verplicht")]
    [EmailAddress(ErrorMessage = "Ongeldig emailadres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wachtwoord is verplicht")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Wachtwoord moet minimaal 6 karakters lang zijn")]
    public string Wachtwoord { get; set; } = string.Empty;

    public bool IsSelfRegistration { get; set; } = true;
}
