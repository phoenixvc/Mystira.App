namespace Mystira.App.PWA.Models;

public class Account
{
    public string Auth0UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public bool EmailVerified { get; set; }
    public string? Name { get; set; }
    public string? NickName { get; set; }
    public string? UpdatedAt { get; set; }
}
