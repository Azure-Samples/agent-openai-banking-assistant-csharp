namespace BankingAssistant.Models;

/// <summary>
/// Represents a logged-in user with their profile information.
/// </summary>
/// <param name="username">The user's username.</param>
/// <param name="mail">The user's email address.</param>
/// <param name="role">The user's role or permission level.</param>
/// <param name="displayName">The user's display name.</param>
public record LoggedUser(string username, string mail, string role, string displayName);
