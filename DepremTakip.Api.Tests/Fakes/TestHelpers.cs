using System.Net;
using System.Text;
using DepremTakip.Api.Afad;
using DepremTakip.Api.Data;
using DepremTakip.Api.Emsc;
using DepremTakip.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Tests.Fakes;

public class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_respond(request));

    public static FakeHandler Returns(string body, HttpStatusCode status = HttpStatusCode.OK, string mediaType = "application/json")
        => new(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) });
}

public static class TestHelpers
{
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static AfadClient Afad(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://afad.test/apiv2/") });

    public static EmscClient Emsc(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://emsc.test/fdsnws/event/1/") });

    public static Earthquake Quake(string eventId, string? province, double magnitude, DateTime occurredAtUtc, double depthKm = 10) => new()
    {
        EventId = eventId,
        Source = "AFAD",
        Province = province,
        Magnitude = magnitude,
        OccurredAtUtc = occurredAtUtc,
        DepthKm = depthKm,
        Location = province ?? "",
        MagnitudeType = "ML"
    };
}
