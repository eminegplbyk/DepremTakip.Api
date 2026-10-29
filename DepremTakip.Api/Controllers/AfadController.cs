using DepremTakip.Api.Afad;
using DepremTakip.Api.Data;
using DepremTakip.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Controllers;

[ApiController]
[Route("api/afad")]
public class AfadController : ControllerBase
{
    private readonly AfadClient _afad;
    private readonly EarthquakeImporter _importer;
    private readonly AppDbContext _db;

    public AfadController(AfadClient afad, EarthquakeImporter importer, AppDbContext db)
    {
        _afad = afad;
        _importer = importer;
        _db = db;
    }

    // AFAD'dan son X saatin depremlerini getirir, veritabanına KAYDETMEZ (sadece bakmak için).
    // GET /api/afad/preview?hours=3
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] int hours = 3, CancellationToken ct = default)
    {
        if (hours < 1 || hours > 48) return BadRequest("hours 1 ile 48 arasında olmalı.");

        var now = DateTime.UtcNow;
        var events = await _afad.GetEventsAsync(now.AddHours(-hours), now, ct);
        return Ok(events);
    }

    // AFAD'dan son X günün depremlerini çekip veritabanına KAYDEDER.
    // POST /api/afad/import?days=7
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromQuery] int days = 1, CancellationToken ct = default)
    {
        if (days < 1 || days > 30) return BadRequest("days 1 ile 30 arasında olmalı.");

        var now = DateTime.UtcNow;
        int fetched = 0, added = 0, updated = 0;

        // AFAD'ı yormamak için gün gün çekiyoruz
        for (var start = now.AddDays(-days); start < now; start = start.AddDays(1))
        {
            var end = start.AddDays(1) < now ? start.AddDays(1) : now;
            var r = await _importer.ImportAsync(start, end, ct);
            fetched += r.Fetched;
            added += r.Added;
            updated += r.Updated;
        }

        var total = await _db.Earthquakes.CountAsync(ct);
        return Ok(new { fetched, added, updated, totalInDatabase = total });
    }
}
