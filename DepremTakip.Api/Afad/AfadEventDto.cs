using System.Text.Json.Serialization;

namespace DepremTakip.Api.Afad;

// AFAD'ın /apiv2/event/filter cevabındaki tek bir deprem
public class AfadEventDto
{
    [JsonPropertyName("eventID")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("depth")]
    public double Depth { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("magnitude")]
    public double Magnitude { get; set; }

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}