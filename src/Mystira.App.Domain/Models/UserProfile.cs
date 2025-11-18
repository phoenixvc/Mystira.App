namespace Mystira.App.Domain.Models;

public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<string> PreferredFantasyThemes { get; set; } = new();
    
    /// <summary>
    /// Date of birth for dynamic age calculation (COPPA compliance: stored securely)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>
    /// Indicates if this is a guest profile (temporary, not persisted long-term)
    /// </summary>
    public bool IsGuest { get; set; } = false;
    
    /// <summary>
    /// Indicates if this profile represents an NPC
    /// </summary>
    public bool IsNpc { get; set; } = false;
    
    // Convenience property to get AgeGroup object
    public string AgeGroup { get; set; } = string.Empty;
    
    /// <summary>
    /// Media ID for the profile avatar image
    /// </summary>
    public string? AvatarMediaId { get; set; }

    /// <summary>
    /// Media ID for the user's selected avatar
    /// </summary>
    public string? SelectedAvatarMediaId { get; set; }

    /// <summary>
    /// Calculate current age from date of birth, or return null if not available
    /// </summary>
    public int? CurrentAge
    {
        get
        {
            if (!DateOfBirth.HasValue)
                return null;
                
            var age = DateTime.Today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
                age--;
                
            return age;
        }
    }
    
    /// <summary>
    /// Update age group based on current age (if date of birth is available)
    /// </summary>
    public void UpdateAgeGroupFromBirthDate()
    {
        if (!CurrentAge.HasValue)
            return;
            
        AgeGroup = AgeGroupConstants.GetAgeGroupForAge(CurrentAge.Value);
    }
    
    /// <summary>
    /// Badges earned by this user profile
    /// </summary>
    public virtual List<UserBadge> EarnedBadges { get; set; } = new();
    
    /// <summary>
    /// Get badges earned for a specific axis
    /// </summary>
    /// <param name="axis">The compass axis</param>
    /// <returns>List of badges for the axis</returns>
    public List<UserBadge> GetBadgesForAxis(string axis)
    {
        return EarnedBadges.Where(b => b.Axis.Equals(axis, StringComparison.OrdinalIgnoreCase))
                          .OrderByDescending(b => b.EarnedAt)
                          .ToList();
    }
    
    /// <summary>
    /// Check if a badge has already been earned
    /// </summary>
    /// <param name="badgeConfigurationId">The badge configuration ID</param>
    /// <returns>True if badge has been earned</returns>
    public bool HasEarnedBadge(string badgeConfigurationId)
    {
        return EarnedBadges.Any(b => b.BadgeConfigurationId == badgeConfigurationId);
    }
    
    /// <summary>
    /// Add a new earned badge
    /// </summary>
    /// <param name="badge">The badge to add</param>
    public void AddEarnedBadge(UserBadge badge)
    {
        if (!HasEarnedBadge(badge.BadgeConfigurationId))
        {
            badge.UserProfileId = this.Id;
            EarnedBadges.Add(badge);
        }
    }
    
    public bool HasCompletedOnboarding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? AccountId { get; set; } // Link to Account
}

public class AgeGroup
{
    private static readonly Dictionary<string, AgeGroup> AgeGroupLookup = new();
    
    public static AgeGroup Toddlers = new("toddlers", 1, 3);     // 1-3
    public static AgeGroup Preschoolers = new("preschoolers", 4, 5); // 4-5  
    public static AgeGroup School = new("school", 6, 9);         // 6-9
    public static AgeGroup Preteens = new("preteens", 10, 12);     // 10-12
    public static AgeGroup Teens = new("teens", 13, 18);           // 13-18
    public static AgeGroup Adults = new("adults", 19, 120);        // 19+

    public static readonly AgeGroup[] All = [Toddlers, Preschoolers, School, Preteens, Teens, Adults];
    
    public string Name { get; set; }
    public int MinimumAge { get; set; }
    public int MaximumAge { get; set; }
    public string AgeRange => $"{MinimumAge}-{MaximumAge}";
    
    private AgeGroup(string name, int minimumAge, int maximumAge)
    {
        Name = name;
        MinimumAge = minimumAge;
        MaximumAge = maximumAge;
        AgeGroupLookup[name] = this;
    }

    public AgeGroup()
    {
        Name = string.Empty;
    }
    
    /// <summary>
    /// Check if this age group is appropriate for a given minimum age requirement
    /// </summary>
    /// <param name="requiredMinimumAge">The minimum age requirement</param>
    /// <returns>True if this age group meets the requirement</returns>
    public bool IsAppropriateFor(int requiredMinimumAge)
    {
        return MinimumAge >= requiredMinimumAge;
    }
    
    /// <summary>
    /// Check if this age group is appropriate for another age group
    /// </summary>
    /// <param name="targetAgeGroup">The target age group to check against</param>
    /// <returns>True if this age group meets the target's minimum age</returns>
    public bool IsAppropriateFor(AgeGroup targetAgeGroup)
    {
        return MinimumAge >= targetAgeGroup.MinimumAge;
    }
    
    /// <summary>
    /// Get an age group by name
    /// </summary>
    /// <param name="name">The age group name</param>
    /// <returns>The age group or null if not found</returns>
    public static AgeGroup? GetByName(string name)
    {
        return AgeGroupLookup.TryGetValue(name?.ToLower() ?? "", out var ageGroup) ? ageGroup : null;
    }
    
    /// <summary>
    /// Check if an age group name is valid
    /// </summary>
    /// <param name="name">The age group name to validate</param>
    /// <returns>True if valid</returns>
    public static bool IsValid(string name)
    {
        return !string.IsNullOrEmpty(name) && AgeGroupLookup.ContainsKey(name.ToLower());
    }
    
    /// <summary>
    /// Get the age range string for an age group name
    /// </summary>
    /// <param name="name">The age group name</param>
    /// <returns>The age range string or "Unknown" if not found</returns>
    public static string GetAgeRange(string name)
    {
        var ageGroup = GetByName(name);
        return ageGroup?.AgeRange ?? "Unknown";
    }
    
    /// <summary>
    /// Check if content with a minimum age group is appropriate for a target age group
    /// </summary>
    /// <param name="contentMinimumAgeGroup">The minimum age group for the content</param>
    /// <param name="targetAgeGroup">The target age group</param>
    /// <returns>True if appropriate</returns>
    public static bool IsContentAppropriate(string contentMinimumAgeGroup, string targetAgeGroup)
    {
        var contentAge = GetByName(contentMinimumAgeGroup);
        var targetAge = GetByName(targetAgeGroup);
        
        if (contentAge == null || targetAge == null)
            return true; // Default to allowing if unknown
            
        return targetAge.MinimumAge >= contentAge.MinimumAge;
    }
    
    public override string ToString() => Name;
    
    public override bool Equals(object? obj)
    {
        return obj is AgeGroup other && Name == other.Name;
    }
    
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
    
    public static bool operator ==(AgeGroup? left, AgeGroup? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(AgeGroup? left, AgeGroup? right)
    {
        return !Equals(left, right);
    }
}

public static class FantasyThemes
{
    public static readonly string[] Available =
        [
            "Classic Fantasy",
            "Medieval Adventure",
            "Magic & Wizards",
            "Dragons & Knights",
            "Forest Adventures",
            "Mystery & Puzzles",
            "Fairy Tales",
            "Animal Companions",
            "Underwater Worlds",
            "Sky Adventures"
        ];
}