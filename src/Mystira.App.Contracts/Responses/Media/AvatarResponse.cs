namespace Mystira.App.Contracts.Responses.Media;

public class AvatarResponse
{
    public Dictionary<string, List<string>> AgeGroupAvatars { get; set; } = new();
}

public class AvatarConfigurationResponse
{
    public string AgeGroup { get; set; } = string.Empty;
    public List<string> AvatarMediaIds { get; set; } = new();
}

