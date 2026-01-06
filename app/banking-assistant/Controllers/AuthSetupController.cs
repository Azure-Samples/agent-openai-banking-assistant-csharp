
using Microsoft.AspNetCore.Authorization;

[Route("/api/auth_setup")]
[ApiController]
public class AuthSetupController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return await Task.FromResult(Ok(new { UseLogin = false }));
    }
}
