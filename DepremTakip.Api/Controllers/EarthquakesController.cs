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
    /// <remarks>
    /// Tüm filtreler isteğe bağlıdır. Tarih için ya hazır dönem (Period) ya da özel aralık (From/To) kullanılır, ikisi birlikte kullanılamaz.
    /// Örnek: /api/earthquakes?Province=Malatya&amp;Period=Son30Gun&amp;MinMagnitude=3
    /// </remarks>
    /// <response code="200">Filtreye uyan depremler ve sayfa bilgisi.</response>
    /// <response code="400">Geçersiz istek (ör. bilinmeyen il, Period ile From/To birlikte, başlangıç bitişten sonra). Mesaj ne yapılması gerektiğini söyler.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EarthquakeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EarthquakeDto>>> GetList(
        [FromQuery] EarthquakeQuery q, CancellationToken ct)
    {
        if (q.Period.HasValue && (q.From.HasValue || q.To.HasValue))
            return BadRequest("Hazır aralık (Period) ile özel tarih (From/To) birlikte kullanılamaz. " +
                              "Ya Period'dan bir seçenek seçin ya da From/To girin; diğerini boş bırakın.");

        DateTime? from = q.From;
        DateTime? to = q.To;
        if (q.Period.HasValue)
            from = DateTime.UtcNow - q.Period.Value.ToTimeSpan();

        if (from.HasValue && to.HasValue && from > to)
            return BadRequest($"Başlangıç tarihi ({from:yyyy-MM-dd}) bitiş tarihinden ({to:yyyy-MM-dd}) sonra olamaz. " +
                              "From, To'dan önceki bir tarih olmalı.");

        if (q.MinMagnitude.HasValue && q.MaxMagnitude.HasValue && q.MinMagnitude > q.MaxMagnitude)
            return BadRequest("En düşük büyüklük en yüksek büyüklükten büyük olamaz.");

        var query = _db.Earthquakes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Province))
        {
            var province = Provinces.Match(q.Province);
            if (province == null)
                return BadRequest($"'{q.Province}' adında bir il bulunamadı. Lütfen listeden bir il seçin.");
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

        query = q.SortBy == SortOption.Buyukluk
            ? query.OrderByDescending(e => e.Magnitude).ThenByDescending(e => e.OccurredAtUtc)
            : query.OrderByDescending(e => e.OccurredAtUtc);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(Mappings.ToDto)
            .ToListAsync(ct);

        return Ok(new PagedResult<EarthquakeDto>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = total
        });
    }
}
