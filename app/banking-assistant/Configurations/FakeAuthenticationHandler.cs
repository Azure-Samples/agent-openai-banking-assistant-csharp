using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BankingAssistant.Configurations;

/// <summary>
/// A fake authentication handler for development purposes.
/// </summary>
public class FakeAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // In development, create a fake user with a basic claim.
        var claims = new[] { new Claim(ClaimTypes.Name, "LocalTestUser") };
        var identity = new ClaimsIdentity(claims, "Fake");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Fake");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}