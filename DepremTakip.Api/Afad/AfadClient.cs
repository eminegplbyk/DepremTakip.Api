using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DepremTakip.Api.Afad;

public class AfadClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public AfadClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<AfadEventDto>> GetEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var url = $"event/filter?start={Format(startUtc)}&end={Format(endUtc)}&orderby=timedesc";

        var events = await _http.GetFromJsonAsync<List<AfadEventDto>>(url, JsonOptions, ct);
        return events ?? new List<AfadEventDto>();
    }

    private static string Format(DateTime utc) =>
        utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
