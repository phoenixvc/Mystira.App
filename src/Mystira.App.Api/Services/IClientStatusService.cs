using Mystira.App.Contracts.Requests.Media;
using Mystira.App.Contracts.Responses.Media;

namespace Mystira.App.Api.Services;

public interface IClientStatusService
{
    Task<ClientStatusResponse> GetClientStatusAsync(ClientStatusRequest request);
}
