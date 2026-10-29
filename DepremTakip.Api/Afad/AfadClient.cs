using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DepremTakip.Api.Afad;

// AFAD'a istek atıp cevabı AfadEventDto listesine çeviren sınıf ("kurye").
public class AfadClient
{
    private readonly HttpClient _http;

    // AFAD sayıları tırnak içinde ("3.3") gönderiyor; bu ayar onları da double olarak okumamızı sağlıyor.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // HttpClient'ı biz oluşturmuyoruz; Program.cs'teki AddHttpClient ayarıyla .NET hazırlayıp veriyor.
    public AfadClient(HttpClient http)
    {
        _http = http;
    }

    // Verilen UTC zaman aralığındaki depremleri AFAD'dan getirir.
    public async Task<List<AfadEventDto>> GetEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var url = $"event/filter?start={Format(startUtc)}&end={Format(endUtc)}&orderby=timedesc";

        var events = await _http.GetFromJsonAsync<List<AfadEventDto>>(url, JsonOptions, ct);
        return events ?? new List<AfadEventDto>();
    }

    // Tarihi AFAD'ın beklediği "2026-10-29T16:50:22" biçimine çevirir.
    private static string Format(DateTime utc) =>
        utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
