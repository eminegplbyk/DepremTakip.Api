using DepremTakip.Api.Afad;
using DepremTakip.Api.Data;
using DepremTakip.Api.Emsc;
using DepremTakip.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Services;

public record ImportResult(int Fetched, int Added, int Updated);

public class EarthquakeImporter
{
    private readonly AppDbContext _db;
    private readonly AfadClient _afad;
    private readonly EmscClient _emsc;

    public EarthquakeImporter(AppDbContext db, AfadClient afad, EmscClient emsc)
    {
        _db = db;
        _afad = afad;
        _emsc = emsc;
    }

    public async Task<ImportResult> ImportAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var events = await _afad.GetEventsAsync(startUtc, endUtc, ct);

        var incoming = events
            .Where(dto => !string.IsNullOrWhiteSpace(dto.EventId))
            .Select(dto => new Earthquake
            {
                EventId = dto.EventId,
                Source = "AFAD",
                OccurredAtUtc = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
                Magnitude = dto.Magnitude,
                MagnitudeType = dto.Type ?? "",
                DepthKm = dto.Depth,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Location = dto.Location ?? "",
                Province = dto.Province,
                District = dto.District
            })
            .ToList();

        var (added, updated) = await UpsertAsync(incoming, ct);
        return new ImportResult(events.Count, added, updated);
    }

    public async Task<ImportResult> ImportHistoricalAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var incoming = await _emsc.GetEventsAsync(startUtc, endUtc, ct);
        var (added, updated) = await UpsertAsync(incoming, ct);
        return new ImportResult(incoming.Count, added, updated);
    }

    private async Task<(int Added, int Updated)> UpsertAsync(List<Earthquake> incoming, CancellationToken ct)
    {
        var ids = incoming.Select(e => e.EventId).Distinct().ToList();
        var existing = await _db.Earthquakes
            .Where(e => ids.Contains(e.EventId))
            .ToDictionaryAsync(e => e.EventId, ct);

        int added = 0, updated = 0;

        foreach (var eq in incoming)
        {
            if (existing.TryGetValue(eq.EventId, out var current))
            {
                if (current.Magnitude != eq.Magnitude || current.DepthKm != eq.DepthKm || current.Location != eq.Location)
                {
                    Copy(eq, current);
                    updated++;
                }
            }
            else
            {
                eq.CreatedAtUtc = DateTime.UtcNow;
                _db.Earthquakes.Add(eq);
                existing[eq.EventId] = eq;
                added++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return (added, updated);
    }

    private static void Copy(Earthquake from, Earthquake to)
    {
        to.OccurredAtUtc = from.OccurredAtUtc;
        to.Magnitude = from.Magnitude;
        to.MagnitudeType = from.MagnitudeType;
        to.DepthKm = from.DepthKm;
        to.Latitude = from.Latitude;
        to.Longitude = from.Longitude;
        to.Location = from.Location;
        to.Province = from.Province;
        to.District = from.District;
    }
}
