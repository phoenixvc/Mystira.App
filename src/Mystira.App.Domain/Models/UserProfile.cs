namespace Mystira.App.Domain.Models;

public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<FantasyTheme> PreferredFantasyThemes { get; set; } = new();
    
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
    
    // Store as string for database compatibility, but provide AgeGroup access
    private string _ageGroup = "school";
    public string AgeGroupName 
    { 
        get => _ageGroup; 
        set => _ageGroup = value; 
    }
    
    // Convenience property to get AgeGroup object
    public AgeGroup AgeGroup 
    { 
        get => AgeGroup.Parse(_ageGroup) ?? new AgeGroup("school", 6, 9);
        set => _ageGroup = value?.Value ?? "school";
    }
    
    /// <summary>
    /// Calculate current age from date of birth, or return null if not available
    /// </summary>
    public int? CurrentAge
    {
        get
        {
            if (!DateOfBirth.HasValue)
                return null;

            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }
    }
    
    /// <summary>
    /// Update age group based on current age (if date of birth is available)
    /// </summary>
    public void UpdateAgeGroupFromBirthDate()
    {
        var ageGroup = GetAgeGroupFromBirthDate();
        if (ageGroup != null)
        {
            AgeGroup = ageGroup;
        }
    }

    public AgeGroup? GetAgeGroupFromBirthDate()
    {
        if (!CurrentAge.HasValue)
        {
            return null;
        }

        var currentAge = CurrentAge.Value;
        var ageGroups = AgeGroup.ValueMap.Values;
        var appropriateAgeGroup = ageGroups.FirstOrDefault(ag =>
            currentAge >= ag.MinimumAge && currentAge <= ag.MaximumAge);

        if (appropriateAgeGroup == null)
        {
            var maxAgeGroup = ageGroups.OrderByDescending(ag => ag.MaximumAge).FirstOrDefault();
            if (maxAgeGroup != null && currentAge > maxAgeGroup.MaximumAge)
            {
                appropriateAgeGroup = maxAgeGroup;
            }
        }

        return appropriateAgeGroup;
    }
    
    /// <summary>
    /// Badges earned by this user profile
    /// </summary>
    public virtual List<UserBadge> EarnedBadges { get; private set; } = new();
    
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

public class AgeGroup : StringEnum<AgeGroup>
{
    public int MinimumAge { get; }
    public int MaximumAge { get; }
    public string AgeRange => $"{MinimumAge}-{MaximumAge}";

    public AgeGroup(string name, int minimumAge, int maximumAge) : base(name)
    {
        MinimumAge = minimumAge;
        MaximumAge = maximumAge;
    }

    public bool IsAppropriateFor(int requiredMinimumAge)
    {
        return MinimumAge >= requiredMinimumAge;
    }

    public bool IsAppropriateFor(AgeGroup targetAgeGroup)
    {
        return MinimumAge >= targetAgeGroup.MinimumAge;
    }

    public static bool IsContentAppropriate(string contentMinimumAgeGroup, string targetAgeGroup)
    {
        var contentAge = Parse(contentMinimumAgeGroup);
        var targetAge = Parse(targetAgeGroup);

        if (contentAge == null || targetAge == null)
            return true;

        return targetAge.MinimumAge >= contentAge.MinimumAge;
    }
}
