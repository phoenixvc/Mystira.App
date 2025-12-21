using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification to get all badge configurations
/// </summary>
public class AllBadgeConfigurationsSpecification : BaseSpecification<BadgeConfiguration>
{
    public AllBadgeConfigurationsSpecification() : base()
    {
        ApplyOrderBy(b => b.Axis);
    }
}

/// <summary>
/// Specification to filter badge configurations by compass axis
/// </summary>
public class BadgeConfigurationsByAxisSpecification : BaseSpecification<BadgeConfiguration>
{
    public BadgeConfigurationsByAxisSpecification(string axis)
        : base(b => b.Axis == axis)
    {
        ApplyOrderBy(b => b.Threshold);
    }
}
