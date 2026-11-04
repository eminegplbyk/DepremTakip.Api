using DepremTakip.Api.Services;
using DepremTakip.Api.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Tests;

public class EarthquakeImporterTests
{
    private static readonly DateTime Start = new(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

    private static string AfadEvent(string eventId, string magnitude) =>
        $$"""
        {"eventID":"{{eventId}}","location":"Yeşilyurt (Malatya)","latitude":"38.29","longitude":"38.25",
         "depth":"7.0","type":"ML","magnitude":"{{magnitude}}","province":"Malatya","district":"Yeşilyurt",
         "date":"2026-11-03T10:00:00"}
        """;

    private static string AfadResponse(params string[] events) => "[" + string.Join(",", events) + "]";

    private static readonly FakeHandler Unused = new(_ => throw new InvalidOperationException("Bu testte çağrılmamalı"));

    [Fact]
    public async Task Import_YeniDepremler_Eklenir()
    {
        using var db = TestHelpers.NewDb();
        var body = AfadResponse(AfadEvent("100", "3.9"), AfadEvent("101", "2.1"));
        var importer = new EarthquakeImporter(db, TestHelpers.Afad(FakeHandler.Returns(body)), TestHelpers.Emsc(Unused));

        var result = await importer.ImportAsync(Start, End);

        Assert.Equal(new ImportResult(Fetched: 2, Added: 2, Updated: 0), result);
        var saved = await db.Earthquakes.SingleAsync(e => e.EventId == "100");
        Assert.Equal(3.9, saved.Magnitude);
        Assert.Equal("Malatya", saved.Province);
        Assert.Equal("AFAD", saved.Source);
        Assert.Equal(DateTimeKind.Utc, saved.OccurredAtUtc.Kind);
    }

    [Fact]
    public async Task Import_AyniDepremIkinciKezGelirse_KopyaOlusmaz()
    {
        using var db = TestHelpers.NewDb();
        var body = AfadResponse(AfadEvent("100", "3.9"));
        var importer = new EarthquakeImporter(db, TestHelpers.Afad(FakeHandler.Returns(body)), TestHelpers.Emsc(Unused));

        await importer.ImportAsync(Start, End);
        var second = await importer.ImportAsync(Start, End);

        Assert.Equal(0, second.Added);
        Assert.Equal(0, second.Updated);
        Assert.Equal(1, await db.Earthquakes.CountAsync());
    }

    [Fact]
    public async Task Import_AyniCevaptaIkiKezGelenDeprem_BirKezEklenir()
    {
        using var db = TestHelpers.NewDb();
        var body = AfadResponse(AfadEvent("100", "3.9"), AfadEvent("100", "3.9"));
        var importer = new EarthquakeImporter(db, TestHelpers.Afad(FakeHandler.Returns(body)), TestHelpers.Emsc(Unused));

        var result = await importer.ImportAsync(Start, End);

        Assert.Equal(1, result.Added);
        Assert.Equal(1, await db.Earthquakes.CountAsync());
    }

    [Fact]
    public async Task Import_AfadBuyukluguDuzeltirse_KayitGuncellenir()
    {
        using var db = TestHelpers.NewDb();
        var body = AfadResponse(AfadEvent("100", "3.9"));
        var importer = new EarthquakeImporter(db, TestHelpers.Afad(new FakeHandler(_ => new HttpResponseMessage
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        })), TestHelpers.Emsc(Unused));

        await importer.ImportAsync(Start, End);
        body = AfadResponse(AfadEvent("100", "4.1"));
        var second = await importer.ImportAsync(Start, End);

        Assert.Equal(0, second.Added);
        Assert.Equal(1, second.Updated);
        var saved = await db.Earthquakes.SingleAsync();
        Assert.Equal(4.1, saved.Magnitude);
    }

    [Fact]
    public async Task Import_EventIdBosOlanKayit_Kaydedilmez()
    {
        using var db = TestHelpers.NewDb();
        var body = AfadResponse(AfadEvent("", "3.0"), AfadEvent("100", "3.9"));
        var importer = new EarthquakeImporter(db, TestHelpers.Afad(FakeHandler.Returns(body)), TestHelpers.Emsc(Unused));

        var result = await importer.ImportAsync(Start, End);

        Assert.Equal(2, result.Fetched);
        Assert.Equal(1, result.Added);
        Assert.Equal("100", (await db.Earthquakes.SingleAsync()).EventId);
    }

    [Fact]
    public async Task ImportHistorical_EmscKayitlari_KaynagiEmscOlarakEklenir()
    {
        using var db = TestHelpers.NewDb();
        var body = "#header\n" +
                   "20230206_1|2023-02-06T01:17:35|37.17|37.08|10|EMSC|X|EMSC|1|mw|7.8|EMSC|CENTRAL TURKEY\n" +
                   "20230206_2|2023-02-06T10:24:49|38.02|37.20|10|EMSC|X|EMSC|1|mw|7.5|EMSC|CENTRAL TURKEY\n";
        var importer = new EarthquakeImporter(db, TestHelpers.Afad(Unused),
            TestHelpers.Emsc(FakeHandler.Returns(body, mediaType: "text/plain")));

        var result = await importer.ImportHistoricalAsync(Start, End);

        Assert.Equal(2, result.Added);
        Assert.All(await db.Earthquakes.ToListAsync(), e => Assert.Equal("EMSC", e.Source));
    }
}
