using DepremTakip.Api.Data;
using DepremTakip.Api.Dtos;
using DepremTakip.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Controllers;

[ApiController]
[Route("api/earthquakes")]
public class EarthquakesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EarthquakesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Depremleri filtreleyip sayfa sayfa listeler. Giriş gerektirmez.</summary>
    // GARSON: Sipariş fişine (filtrelere) uyan depremleri sayfa sayfa getirir (FR-07, FR-09).
    [HttpGet]
    public async Task<ActionResult<PagedResult<EarthquakeDto>>> GetList(
        [FromQuery] EarthquakeQuery q, CancellationToken ct)
    {
        // 1) Tarih: ya hazır aralık (Period) ya da özel tarih (From/To). İkisi birden olmaz.
        if (q.Period.HasValue && (q.From.HasValue || q.To.HasValue))
            return BadRequest("Hazır aralık (Period) ile özel tarih (From/To) birlikte kullanılamaz. " +
                              "Ya Period'dan bir seçenek seçin ya da From/To girin; diğerini boş bırakın.");

        DateTime? from = q.From;
        DateTime? to = q.To;
        if (q.Period.HasValue)
            from = DateTime.UtcNow - ToTimeSpan(q.Period.Value);

        // 2) Fiş mantıklı mı? Değilse ne yapılması gerektiğini söyleyen bir hata ver.
        if (from.HasValue && to.HasValue && from > to)
            return BadRequest($"Başlangıç tarihi ({from:yyyy-MM-dd}) bitiş tarihinden ({to:yyyy-MM-dd}) sonra olamaz. " +
                              "From, To'dan önceki bir tarih olmalı.");

        if (q.MinMagnitude.HasValue && q.MaxMagnitude.HasValue && q.MinMagnitude > q.MaxMagnitude)
            return BadRequest("En düşük büyüklük en yüksek büyüklükten büyük olamaz.");

        // 3) Defterin tamamıyla başla (henüz veritabanına gidilmedi, sorgu hazırlanıyor).
        var query = _db.Earthquakes.AsNoTracking().AsQueryable();

        // 4) Fişte hangi filtre doluysa onu uygula.
        if (!string.IsNullOrWhiteSpace(q.Province))
        {
            // "istanbul" / "Istanbul" yazılsa da resmi yazımı "İstanbul" ile ara.
            var province = Provinces.Match(q.Province) ?? q.Province.Trim();
            query = query.Where(e => e.Province == province);
        }
        if (from.HasValue)
        {
            var f = from.Value;
            query = query.Where(e => e.OccurredAtUtc >= f);
        }
        if (to.HasValue)
        {
            var t = to.Value;
            query = query.Where(e => e.OccurredAtUtc <= t);
        }
        if (q.MinMagnitude.HasValue)
        {
            var min = q.MinMagnitude.Value;
            query = query.Where(e => e.Magnitude >= min);
        }
        if (q.MaxMagnitude.HasValue)
        {
            var max = q.MaxMagnitude.Value;
            query = query.Where(e => e.Magnitude <= max);
        }
        if (q.MaxDepthKm.HasValue)
        {
            var depth = q.MaxDepthKm.Value;
            query = query.Where(e => e.DepthKm <= depth);
        }

        // 5) Sırala: varsayılan en yeni en üstte; istenirse en büyük en üstte.
        query = q.SortBy == SortOption.Buyukluk
            ? query.OrderByDescending(e => e.Magnitude).ThenByDescending(e => e.OccurredAtUtc)
            : query.OrderByDescending(e => e.OccurredAtUtc);

        // 6) Filtreye uyan toplam kaç deprem var?
        var total = await query.CountAsync(ct);

        // 7) Sadece istenen sayfayı al ve DTO'ya çevir.
        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(e => new EarthquakeDto
            {
                Id = e.Id,
                EventId = e.EventId,
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
            })
            .ToListAsync(ct);

        // 8) Tepsiye koy ve gönder.
        return Ok(new PagedResult<EarthquakeDto>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = total
        });
    }

    private static TimeSpan ToTimeSpan(TimePeriod period) => period switch
    {
        TimePeriod.Son24Saat => TimeSpan.FromHours(24),
        TimePeriod.Son7Gun => TimeSpan.FromDays(7),
        TimePeriod.Son30Gun => TimeSpan.FromDays(30),
        TimePeriod.Son90Gun => TimeSpan.FromDays(90),
        _ => TimeSpan.FromDays(365)
    };
}
