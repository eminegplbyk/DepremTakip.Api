using System.Linq.Expressions;
using DepremTakip.Api.Dtos;
using DepremTakip.Api.Models;

namespace DepremTakip.Api.Helpers;

public static class Mappings
{
    public static readonly Expression<Func<Earthquake, EarthquakeDto>> ToDto = e => new EarthquakeDto
    {
        Id = e.Id,
        EventId = e.EventId,
        Source = e.Source,
        OccurredAtUtc = e.OccurredAtUtc,
        OccurredAtTurkey = e.OccurredAtUtc.AddHours(3),
        Magnitude = e.Magnitude,
        MagnitudeType = e.MagnitudeType,
        DepthKm = e.DepthKm,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        Location = e.Location,
        Province = e.Province,
        District = e.District
    };

    public static TimeSpan ToTimeSpan(this TimePeriod period) => period switch
    {
        TimePeriod.Son24Saat => TimeSpan.FromHours(24),
        TimePeriod.Son7Gun => TimeSpan.FromDays(7),
        TimePeriod.Son30Gun => TimeSpan.FromDays(30),
        TimePeriod.Son90Gun => TimeSpan.FromDays(90),
        _ => TimeSpan.FromDays(365)
    };
}
