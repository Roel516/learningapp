using System.ComponentModel.DataAnnotations;

namespace LearningResourcesApp.Client.Models.Authenticatie;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is verplicht")]
    [EmailAddress(ErrorMessage = "Ongeldig emailadres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wachtwoord is verplicht")]
    public string Wachtwoord { get; set; } = string.Empty;
}
