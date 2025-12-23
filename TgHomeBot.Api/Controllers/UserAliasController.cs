using Microsoft.AspNetCore.Mvc;
using TgHomeBot.Charging.Contract;
using TgHomeBot.Charging.Contract.Models;

namespace TgHomeBot.Api.Controllers;

/// <summary>
/// Controller for managing Easee user aliases
/// </summary>
public class UserAliasController : Controller
{
    private readonly ILogger<UserAliasController> _logger;
    private readonly IUserAliasService _userAliasService;

    public UserAliasController(
        ILogger<UserAliasController> logger,
        IUserAliasService userAliasService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userAliasService = userAliasService ?? throw new ArgumentNullException(nameof(userAliasService));
    }

    [HttpGet]
    public IActionResult Index()
    {
        _logger.LogInformation("User alias management page accessed");

        var trackedUserIds = _userAliasService.GetTrackedUserIds();
        var aliases = _userAliasService.GetAllAliases();

        var model = trackedUserIds.Select(userId =>
        {
            var alias = aliases.FirstOrDefault(a => a.UserId == userId);
            return alias ?? new UserAlias { UserId = userId };
        }).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Edit(string userId)
    {
        _logger.LogInformation("Editing user alias for user {UserId}", userId);

        var alias = _userAliasService.GetAliasByUserId(userId) 
            ?? new UserAlias { UserId = userId };

        return View(alias);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(UserAlias model, string tokenIdsInput)
    {
        _logger.LogInformation("Saving user alias for user {UserId}", model.UserId);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("User alias validation failed for user {UserId}", model.UserId);
            return View(model);
        }

        // Parse token IDs from comma-separated input
        if (!string.IsNullOrWhiteSpace(tokenIdsInput))
        {
            model.TokenIds = tokenIdsInput
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }
        else
        {
            model.TokenIds = [];
        }

        _userAliasService.SaveAlias(model);

        TempData["Message"] = "Alias erfolgreich gespeichert!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string userId)
    {
        _logger.LogInformation("Deleting user alias for user {UserId}", userId);

        _userAliasService.DeleteAlias(userId);

        TempData["Message"] = "Alias erfolgreich gelöscht!";
        return RedirectToAction(nameof(Index));
    }
}
