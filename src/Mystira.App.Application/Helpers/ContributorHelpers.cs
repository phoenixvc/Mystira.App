using Mystira.Domain.Models;

namespace Mystira.App.Application.Helpers;

/// <summary>
/// Shared helper methods for working with contributors
/// </summary>
public static class ContributorHelpers
{
    /// <summary>
    /// Gets a human-readable display name for a contributor role
    /// </summary>
    /// <param name="role">The contributor role</param>
    /// <returns>Human-readable role name</returns>
    public static string GetRoleDisplayName(ContributorRole role)
    {
        return role switch
        {
            ContributorRole.Writer => "Writer",
            ContributorRole.Artist => "Artist",
            ContributorRole.VoiceActor => "Voice Actor",
            ContributorRole.MusicComposer => "Music Composer",
            ContributorRole.SoundDesigner => "Sound Designer",
            ContributorRole.Editor => "Editor",
            ContributorRole.GameDesigner => "Game Designer",
            ContributorRole.QualityAssurance => "Quality Assurance",
            ContributorRole.Other => "Contributor",
            _ => "Contributor"
        };
    }
}
