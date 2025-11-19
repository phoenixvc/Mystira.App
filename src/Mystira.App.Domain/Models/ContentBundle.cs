namespace Mystira.App.Domain.Models;

public class ContentBundle
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ScenarioIds { get; set; } = new();
    public string ImageId { get; set; } = string.Empty; // Used by CachedMystiraImage
    public List<BundlePrice> Prices { get; set; } = new();
    public bool IsFree { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
}

public class BundlePrice
{
    public decimal Value { get; set; }
    public string Currency { get; set; } = "USD";
}
