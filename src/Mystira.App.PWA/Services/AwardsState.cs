using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAwardsState
{
    FinalizeSessionResponse? Result { get; }
    void Set(FinalizeSessionResponse result);
    void Clear();
}

public class AwardsState : IAwardsState
{
    public FinalizeSessionResponse? Result { get; private set; }

    public void Set(FinalizeSessionResponse result)
    {
        Result = result;
    }

    public void Clear()
    {
        Result = null;
    }
}
