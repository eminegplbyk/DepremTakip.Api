using System.Globalization;
using System.Net;
using DepremTakip.Api.Helpers;
using DepremTakip.Api.Models;

namespace DepremTakip.Api.Emsc;

public class EmscClient
{
    private const double MinLat = 35.8, MaxLat = 42.2, MinLon = 25.6, MaxLon = 44.9;

    private const double MaxProvinceDistanceKm = 100;

    private readonly HttpClient _http;

    public EmscClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Earthquake>> GetEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var url = "query?format=text&limit=5000&orderby=time" +
                  $"&starttime={T(startUtc)}&endtime={T(endUtc)}" +
                  $"&minlat={F(MinLat)}&maxlat={F(MaxLat)}&minlon={F(MinLon)}&maxlon={F(MaxLon)}";

        using var response = await _http.GetAsync(url, ct);

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

    private static bool TryParseLine(string line, out Earthquake eq)
    {
        eq = null!;
        line = line.Trim();
        if (line.Length == 0 || line.StartsWith('#')) return false;

        var p = line.Split('|');
        if (p.Length < 13) return false;

        if (string.IsNullOrWhiteSpace(p[0])) return false;
        if (!DateTime.TryParse(p[1], CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var time)) return false;
        if (!TryD(p[2], out var lat) || !TryD(p[3], out var lon)) return false;
        if (!TryD(p[10], out var mag)) return false;
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
