using System.Globalization;
using System.Net;
using DepremTakip.Api.Helpers;
using DepremTakip.Api.Models;

namespace DepremTakip.Api.Emsc;

// EMSC (Avrupa-Akdeniz Sismoloji Merkezi) FDSN servisinden GEÇMİŞ depremleri getirir.
// AFAD'ın servisi sadece son ~2 haftayı verdiği için daha eski veriyi buradan alıyoruz.
// Doküman: https://www.seismicportal.eu/fdsn-wsevent.html
public class EmscClient
{
    // Türkiye ve çevresini kapsayan dikdörtgen (enlem/boylam)
    private const double MinLat = 35.8, MaxLat = 42.2, MinLon = 25.6, MaxLon = 44.9;

    // En yakın il merkezi bu mesafeden uzaksa il atanmaz (ör. Yunanistan, Suriye içleri)
    private const double MaxProvinceDistanceKm = 100;

    private readonly HttpClient _http;

    public EmscClient(HttpClient http)
    {
        _http = http;
    }

    // Verilen UTC aralığındaki depremleri Earthquake nesneleri olarak döner (henüz kaydetmez).
    public async Task<List<Earthquake>> GetEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var url = "query?format=text&limit=5000&orderby=time" +
                  $"&starttime={T(startUtc)}&endtime={T(endUtc)}" +
                  $"&minlat={F(MinLat)}&maxlat={F(MaxLat)}&minlon={F(MinLon)}&maxlon={F(MaxLon)}";

        using var response = await _http.GetAsync(url, ct);

        // FDSN standardında "bu aralıkta deprem yok" cevabı 204 No Content'tir
        if (response.StatusCode == HttpStatusCode.NoContent)
            return new List<Earthquake>();

        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync(ct);

        var result = new List<Earthquake>();
        foreach (var line in text.Split('\n'))
        {
            if (TryParseLine(line, out var eq))
                result.Add(eq);
        }
        return result;
    }

    // Satır biçimi (| ile ayrılmış):
    // EventID|Time|Latitude|Longitude|Depth/km|Author|Catalog|Contributor|ContributorID|MagType|Magnitude|MagAuthor|EventLocationName
    private static bool TryParseLine(string line, out Earthquake eq)
    {
        eq = null!;
        line = line.Trim();
        if (line.Length == 0 || line.StartsWith('#')) return false; // boş satır veya başlık

        var p = line.Split('|');
        if (p.Length < 13) return false;

        if (string.IsNullOrWhiteSpace(p[0])) return false;
        if (!DateTime.TryParse(p[1], CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var time)) return false;
        if (!TryD(p[2], out var lat) || !TryD(p[3], out var lon)) return false;
        if (!TryD(p[10], out var mag)) return false;      // büyüklüğü olmayan kaydı alma
        TryD(p[4], out var depth);

        var region = p[12].Trim();
        var province = Provinces.Nearest(lat, lon, MaxProvinceDistanceKm);

        eq = new Earthquake
        {
            EventId = p[0].Trim(),
            Source = "EMSC",
            OccurredAtUtc = DateTime.SpecifyKind(time, DateTimeKind.Utc),
            Latitude = lat,
            Longitude = lon,
            DepthKm = depth,
            Magnitude = mag,
            MagnitudeType = p[9].Trim().ToUpperInvariant(),
            // AFAD'ın "Yer (İl)" biçimine benzesin; il yaklaşık olduğu için belirtiyoruz
            Location = province != null ? $"{province} yakını (EMSC: {region})" : $"EMSC: {region}",
            Province = province
        };
        return true;
    }

    private static bool TryD(string s, out double d) =>
        double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out d);

    private static string F(double d) => d.ToString(CultureInfo.InvariantCulture);

    private static string T(DateTime d) => d.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
}
