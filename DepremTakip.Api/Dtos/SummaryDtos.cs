namespace DepremTakip.Api.Dtos;

public class ProvinceSummaryDto
{
    public string Province { get; set; } = string.Empty;
    public TimePeriod Period { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    public int TotalCount { get; set; }
    public double? AverageMagnitude { get; set; }
    public EarthquakeDto? Strongest { get; set; }
    public EarthquakeDto? Latest { get; set; }

    public MagnitudeBands Bands { get; set; } = new();
}

public class MagnitudeBands
{
    public int Below3 { get; set; }
    public int From3To4 { get; set; }
    public int From4To5 { get; set; }
    public int Above5 { get; set; }
}