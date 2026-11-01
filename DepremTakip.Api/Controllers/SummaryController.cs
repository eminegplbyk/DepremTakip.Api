using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using DepremTakip.Api.Data;
using DepremTakip.Api.Dtos;
using DepremTakip.Api.Helpers;
using DepremTakip.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Controllers;

[ApiController]
[Route("api/summary")]
public class SummaryController : ControllerBase
{
    private readonly AppDbContext _db;

    public SummaryController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Bir il için seçilen dönemin deprem özeti (ör. Malatya, son 30 gün). Giriş gerektirmez.</summary>
    /// <param name="province">İl. Listeden seçin.</param>
    /// <param name="period">Dönem. Boş bırakırsanız son 30 gün.</param>
    [HttpGet("province")]
    public async Task<ActionResult<ProvinceSummaryDto>> GetProvinceSummary(
        [FromQuery, Required] string province,
        [FromQuery] TimePeriod period = TimePeriod.Son30Gun,
        CancellationToken ct = default)
    {
        // 1) İl adı geçerli mi? ("istanbul" yazılsa da "İstanbul" bulunur)
        var name = Provinces.Match(province);
        if (name == null)
            return BadRequest($"'{province}' adında bir il bulunamadı. Lütfen listeden bir il seçin.");

        // 2) Dönemin başlangıcını hesapla
        var now = DateTime.UtcNow;
        var from = now - ToTimeSpan(period);

        // 3) Bu ilde bu dönemdeki depremler
        var quakes = _db.Earthquakes.AsNoTracking()
            .Where(e => e.Province == name && e.OccurredAtUtc >= from);

        var summary = new ProvinceSummaryDto
        {
            Province = name,
            Period = period,
            FromUtc = from,
            ToUtc = now,
            TotalCount = await quakes.CountAsync(ct)
        };

        // 4) Hiç deprem yoksa hata değil, "0 deprem" özeti dön (kabul kriteri 4)
        if (summary.TotalCount == 0)
            return Ok(summary);

        // 5) Ortalama, en büyük ve en son deprem
        summary.AverageMagnitude = Math.Round(await quakes.AverageAsync(e => e.Magnitude, ct), 2);

        summary.Strongest = await quakes
            .OrderByDescending(e => e.Magnitude).ThenByDescending(e => e.OccurredAtUtc)
            .Select(ToDto).FirstAsync(ct);

        summary.Latest = await quakes
            .OrderByDescending(e => e.OccurredAtUtc)
            .Select(ToDto).FirstAsync(ct);

        // 6) Büyüklük gruplarına göre say
        summary.Bands = new MagnitudeBands
        {
            Below3 = await quakes.CountAsync(e => e.Magnitude < 3, ct),
            From3To4 = await quakes.CountAsync(e => e.Magnitude >= 3 && e.Magnitude < 4, ct),
            From4To5 = await quakes.CountAsync(e => e.Magnitude >= 4 && e.Magnitude < 5, ct),
            Above5 = await quakes.CountAsync(e => e.Magnitude >= 5, ct)
        };

        return Ok(summary);
    }

    // Earthquake -> EarthquakeDto dönüşümü (veritabanı sorgusunun içinde çalışır)
    private static readonly Expression<Func<Earthquake, EarthquakeDto>> ToDto = e => new EarthquakeDto
    {
        Id = e.Id,
        EventId = e.EventId,
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

    private static TimeSpan ToTimeSpan(TimePeriod period) => period switch
    {
        TimePeriod.Son24Saat => TimeSpan.FromHours(24),
        TimePeriod.Son7Gun => TimeSpan.FromDays(7),
        TimePeriod.Son30Gun => TimeSpan.FromDays(30),
        TimePeriod.Son90Gun => TimeSpan.FromDays(90),
        _ => TimeSpan.FromDays(365)
    };
}