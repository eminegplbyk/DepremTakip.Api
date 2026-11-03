using System.Linq.Expressions;
using DepremTakip.Api.Dtos;
using DepremTakip.Api.Models;

namespace DepremTakip.Api.Helpers;

// Birden fazla controller'ın kullandığı ortak dönüşümler (önceden her controller'da ayrı ayrı yazılıydı).
public static class Mappings
{
    // Earthquake -> EarthquakeDto (veritabanı sorgusunun içinde, SQL'e çevrilerek çalışır)
    public static readonly Expression<Func<Earthquake, EarthquakeDto>> ToDto = e => new EarthquakeDto
    {
        Id = e.Id,
        EventId = e.EventId,
        Source = e.Source,
        OccurredAtUtc = e.OccurredAtUtc,
        OccurredAtTurkey = e.OccurredAtUtc.AddHours(3), // BR-06: Türkiye saati
        Magnitude = e.Magnitude,
        MagnitudeType = e.MagnitudeType,
        DepthKm = e.DepthKm,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        Location = e.Location,
        Province = e.Province,
        District = e.District
    };

    // Hazır dönemi süreye çevirir: Son7Gun -> 7 gün
    public static TimeSpan ToTimeSpan(this TimePeriod period) => period switch
    {
        TimePeriod.Son24Saat => TimeSpan.FromHours(24),
        TimePeriod.Son7Gun => TimeSpan.FromDays(7),
        TimePeriod.Son30Gun => TimeSpan.FromDays(30),
        TimePeriod.Son90Gun => TimeSpan.FromDays(90),
        _ => TimeSpan.FromDays(365)
    };
}
