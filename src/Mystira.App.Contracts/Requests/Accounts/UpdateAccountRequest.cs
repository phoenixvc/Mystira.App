using Mystira.App.Domain.Models;

namespace Mystira.App.Contracts.Requests.Accounts;

public class UpdateAccountRequest
{
    public string? DisplayName { get; set; }
    public AccountSettings? Settings { get; set; }
}

