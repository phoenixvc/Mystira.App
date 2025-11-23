using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IClientStatusService
{
    Task<ClientStatusResponse> GetClientStatusAsync(ClientStatusRequest request);
}
