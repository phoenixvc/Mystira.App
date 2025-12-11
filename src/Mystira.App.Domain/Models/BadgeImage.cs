namespace Mystira.App.Domain.Models;

public class BadgeImage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ImageId { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/png";
    public byte[]? ImageData { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
