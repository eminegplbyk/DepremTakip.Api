namespace DepremTakip.Api.Models;

public class Earthquake
{
    public int Id { get; set; }

    public string EventId { get; set; } = string.Empty;

    public string Source { get; set; } = "AFAD";

    public DateTime OccurredAtUtc { get; set; }

    public double Magnitude { get; set; }
    public string MagnitudeType { get; set; } = string.Empty;
    public double DepthKm { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string Location { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string? District { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
