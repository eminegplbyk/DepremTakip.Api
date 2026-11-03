using System.Net;
using DepremTakip.Api.Tests.Fakes;

namespace DepremTakip.Api.Tests;

// EMSC'nin metin cevabını okuma (Emsc/EmscClient.cs)
public class EmscClientTests
{
    private const string Header =
        "#EventID|Time|Latitude|Longitude|Depth/km|Author|Catalog|Contributor|ContributorID|MagType|Magnitude|MagAuthor|EventLocationName";

    private static readonly DateTime Start = new(2023, 2, 6, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2023, 2, 7, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetEvents_GecerliSatir_DepremeCevrilirVeIlAtanir()
    {
        var body = Header + "\n" +
                   "20230206_0000008|2023-02-06T01:17:35.00|37.17|37.08|10.0|EMSC|EMSC-RTS|EMSC|1|mw|7.8|EMSC|CENTRAL TURKEY\n";
        var emsc = TestHelpers.Emsc(FakeHandler.Returns(body, mediaType: "text/plain"));

        var result = await emsc.GetEventsAsync(Start, End);

        var eq = Assert.Single(result);
        Assert.Equal("20230206_0000008", eq.EventId);
        Assert.Equal("EMSC", eq.Source);
        Assert.Equal(7.8, eq.Magnitude);
        Assert.Equal("MW", eq.MagnitudeType);
        Assert.Equal(new DateTime(2023, 2, 6, 1, 17, 35), eq.OccurredAtUtc);
        Assert.Equal(DateTimeKind.Utc, eq.OccurredAtUtc.Kind);
        Assert.Equal("Gaziantep", eq.Province);
        Assert.Equal("Gaziantep yakını (EMSC: CENTRAL TURKEY)", eq.Location);
    }

    [Fact]
    public async Task GetEvents_BaslikBosVeHataliSatirlar_Atlanir()
    {
        var body = Header + "\n" +
                   "\n" +
                   "eksik|satir|var\n" +                                                                         // alan sayısı az
                   "20230206_1|2023-02-06T02:00:00|37.5|37.0|5|EMSC|X|EMSC|1|ml||EMSC|CENTRAL TURKEY\n" +         // büyüklük yok
                   "|2023-02-06T03:00:00|37.5|37.0|5|EMSC|X|EMSC|1|ml|3.1|EMSC|CENTRAL TURKEY\n" +                // EventId yok
                   "20230206_2|2023-02-06T04:00:00|37.5|37.0|5|EMSC|X|EMSC|1|ml|3.4|EMSC|CENTRAL TURKEY\n";       // geçerli
        var emsc = TestHelpers.Emsc(FakeHandler.Returns(body, mediaType: "text/plain"));

        var result = await emsc.GetEventsAsync(Start, End);

        var eq = Assert.Single(result);
        Assert.Equal("20230206_2", eq.EventId);
    }

    [Fact]
    public async Task GetEvents_YurtDisiDeprem_IlAtanmaz()
    {
        var body = Header + "\n" +
                   "20230206_3|2023-02-06T05:00:00|37.98|23.73|10|EMSC|X|EMSC|1|ml|3.0|EMSC|GREECE\n";
        var emsc = TestHelpers.Emsc(FakeHandler.Returns(body, mediaType: "text/plain"));

        var eq = Assert.Single(await emsc.GetEventsAsync(Start, End));

        Assert.Null(eq.Province);
        Assert.Equal("EMSC: GREECE", eq.Location);
    }

    [Fact]
    public async Task GetEvents_204NoContent_BosListeDoner()
    {
        // FDSN standardı: aralıkta deprem yoksa 204 döner, bu hata değildir
        var emsc = TestHelpers.Emsc(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent)));

        var result = await emsc.GetEventsAsync(Start, End);

        Assert.Empty(result);
    }
}
