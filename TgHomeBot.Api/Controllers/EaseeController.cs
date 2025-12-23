using Microsoft.AspNetCore.Mvc;
using TgHomeBot.Api.Models;
using TgHomeBot.Charging.Contract;

namespace TgHomeBot.Api.Controllers;

/// <summary>
/// Controller for Easee authentication
/// </summary>
public class EaseeController : Controller
{
    private readonly ILogger<EaseeController> _logger;
    private readonly IChargingConnector _chargingConnector;

    public EaseeController(
        ILogger<EaseeController> logger,
        IChargingConnector chargingConnector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chargingConnector = chargingConnector ?? throw new ArgumentNullException(nameof(chargingConnector));
    }

    [HttpGet]
    public IActionResult Login()
    {
        _logger.LogInformation("Easee login page accessed");
        
        var model = new EaseeLoginViewModel
        {
            IsAuthenticated = _chargingConnector.IsAuthenticated
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(EaseeLoginViewModel model)
    {
        _logger.LogInformation("Easee login attempt for user {UserName}", model.UserName);
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Easee login validation failed for user {UserName}", model.UserName);
            model.IsAuthenticated = _chargingConnector.IsAuthenticated;
            return View(model);
        }

        var success = await _chargingConnector.AuthenticateAsync(model.UserName, model.Password);

        if (success)
        {
            _logger.LogInformation("Easee authentication successful for user {UserName}", model.UserName);
            model.IsAuthenticated = true;
            model.Message = "Erfolgreich authentifiziert!";
            ModelState.Clear();
            model.UserName = string.Empty;
            model.Password = string.Empty;
        }
        else
        {
            _logger.LogWarning("Easee authentication failed for user {UserName}", model.UserName);
            model.IsAuthenticated = false;
            model.Message = "Authentifizierung fehlgeschlagen. Bitte überprüfen Sie Ihre Zugangsdaten.";
        }

        return View(model);
    }
}
