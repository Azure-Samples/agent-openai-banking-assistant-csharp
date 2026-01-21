
namespace BankingAssistant.Controllers;

/// <summary>
/// Controller for authentication setup configuration.
/// </summary>
[Route("/api/auth_setup")]
[ApiController]
public class AuthSetupController : ControllerBase
{
    /// <summary>
    /// Gets the authentication configuration.
    /// </summary>
    /// <returns>An object indicating whether login is required.</returns>
    [HttpGet]
    public async Task<IActionResult> IndexAsync()
    {
        return await Task.FromResult(Ok(new { UseLogin = false }));
    }
}
