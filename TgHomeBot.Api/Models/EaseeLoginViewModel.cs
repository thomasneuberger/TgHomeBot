using System.ComponentModel.DataAnnotations;

namespace TgHomeBot.Api.Models;

/// <summary>
/// View model for Easee login page
/// </summary>
public class EaseeLoginViewModel
{
    [Required(ErrorMessage = "Benutzername ist erforderlich")]
    [Display(Name = "Benutzername")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passwort ist erforderlich")]
    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string Password { get; set; } = string.Empty;

    public bool IsAuthenticated { get; set; }
    public string? Message { get; set; }
}
