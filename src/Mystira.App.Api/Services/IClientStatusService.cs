using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

public interface IClientStatusService
{
    Task<ClientStatusResponse> GetClientStatusAsync(ClientStatusRequest request);
}
