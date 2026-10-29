using DepremTakip.Api.Afad;
using DepremTakip.Api.Data;
using DepremTakip.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Services;

public record ImportResult(int Fetched, int Added, int Updated);

// AFAD'dan gelen depremleri veritabanına kaydeder:
// yeni deprem -> ekle, zaten varsa ve AFAD bilgisini değiştirdiyse -> güncelle.
public class EarthquakeImporter
{
    private readonly AppDbContext _db;
    private readonly AfadClient _afad;

    public EarthquakeImporter(AppDbContext db, AfadClient afad)
    {
        _db = db;
        _afad = afad;
    }

    public async Task<ImportResult> ImportAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        // 1) AFAD'dan bu aralıktaki depremleri al
        var events = await _afad.GetEventsAsync(startUtc, endUtc, ct);

        // 2) Bunlardan hangileri bizde zaten var? Tek sorguda bul.
        var ids = events.Select(e => e.EventId).Distinct().ToList();
        var existing = await _db.Earthquakes
            .Where(e => ids.Contains(e.EventId))
            .ToDictionaryAsync(e => e.EventId, ct);

        int added = 0, updated = 0;

        foreach (var dto in events)
        {
            if (string.IsNullOrWhiteSpace(dto.EventId)) continue; // bozuk kayıt, atla

            if (existing.TryGetValue(dto.EventId, out var eq))
            {
                // Zaten var: AFAD büyüklüğü/yeri sonradan düzelttiyse biz de güncelleyelim
                if (eq.Magnitude != dto.Magnitude || eq.DepthKm != dto.Depth || eq.Location != (dto.Location ?? ""))
                {
                    Copy(dto, eq);
                    updated++;
                }
            }
            else
            {
                // Yeni deprem: ekle
                var newEq = new Earthquake { EventId = dto.EventId, CreatedAtUtc = DateTime.UtcNow };
                Copy(dto, newEq);
                _db.Earthquakes.Add(newEq);
                existing[dto.EventId] = newEq; // AFAD aynı depremi iki kez gönderirse ikinciyi ekleme
                added++;
            }
        }

        // 3) Hepsini tek seferde veritabanına yaz
        await _db.SaveChangesAsync(ct);

        return new ImportResult(events.Count, added, updated);
    }

    // DTO'daki bilgileri Earthquake nesnesine aktarır
    private static void Copy(AfadEventDto dto, Earthquake eq)
    {
        eq.OccurredAtUtc = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc); // AFAD saatleri UTC
        eq.Magnitude = dto.Magnitude;
        eq.MagnitudeType = dto.Type ?? "";
        eq.DepthKm = dto.Depth;
        eq.Latitude = dto.Latitude;
        eq.Longitude = dto.Longitude;
        eq.Location = dto.Location ?? "";
        eq.Province = dto.Province;
        eq.District = dto.District;
    }
}
