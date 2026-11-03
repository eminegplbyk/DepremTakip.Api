using DepremTakip.Api.Controllers;
using DepremTakip.Api.Data;
using DepremTakip.Api.Dtos;
using DepremTakip.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace DepremTakip.Api.Tests;

// Filtreli liste (GET /api/earthquakes) ve il özeti (GET /api/summary/province)
public class EarthquakesControllerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    // Örnek veri: 3 Malatya, 1 İzmir, 1 il bilgisi olmayan deprem
    private static AppDbContext SeededDb()
    {
        var db = TestHelpers.NewDb();
        db.Earthquakes.AddRange(
            TestHelpers.Quake("1", "Malatya", 2.5, Now.AddDays(-1)),
            TestHelpers.Quake("2", "Malatya", 4.2, Now.AddDays(-3), depthKm: 25),
            TestHelpers.Quake("3", "Malatya", 3.1, Now.AddDays(-60)),
            TestHelpers.Quake("4", "İzmir", 5.0, Now.AddHours(-2)),
            TestHelpers.Quake("5", null, 3.0, Now.AddDays(-2)));
        db.SaveChanges();
        return db;
    }

    private static PagedResult<EarthquakeDto> Ok(ActionResult<PagedResult<EarthquakeDto>> result) =>
        Assert.IsType<PagedResult<EarthquakeDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);

    // ---------- Hatalı istekler: anlaşılır 400 dönmeli ----------

    [Fact]
    public async Task GetList_PeriodVeFromBirlikte_BadRequest()
    {
        // BR-11: hazır dönem ile özel tarih birlikte kullanılamaz
        using var db = SeededDb();
        var result = await new EarthquakesController(db).GetList(
            new EarthquakeQuery { Period = TimePeriod.Son7Gun, From = Now.AddDays(-10) }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetList_BaslangicBitistenSonra_BadRequest()
    {
        using var db = SeededDb();
        var result = await new EarthquakesController(db).GetList(
            new EarthquakeQuery { From = Now, To = Now.AddDays(-5) }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetList_MinBuyuklukMaxtanBuyuk_BadRequest()
    {
        using var db = SeededDb();
        var result = await new EarthquakesController(db).GetList(
            new EarthquakeQuery { MinMagnitude = 5, MaxMagnitude = 3 }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetList_BilinmeyenIl_BadRequest()
    {
        using var db = SeededDb();
        var result = await new EarthquakesController(db).GetList(
            new EarthquakeQuery { Province = "Atlantis" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ---------- Filtreleme, sıralama, sayfalama ----------

    [Fact]
    public async Task GetList_FiltreYok_TumDepremlerEnYeniEnUstte()
    {
        using var db = SeededDb();
        var page = Ok(await new EarthquakesController(db).GetList(new EarthquakeQuery(), default));

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(new[] { "4", "1", "5", "2", "3" }, page.Items.Select(e => e.EventId));
    }

    [Fact]
    public async Task GetList_IlKucukHarfleYazilsaDa_DogruIliFiltreler()
    {
        using var db = SeededDb();
        var page = Ok(await new EarthquakesController(db).GetList(new EarthquakeQuery { Province = "malatya" }, default));

        Assert.Equal(3, page.TotalCount);
        Assert.All(page.Items, e => Assert.Equal("Malatya", e.Province));
    }

    [Fact]
    public async Task GetList_HazirDonem_SadeceOAraliktakiDepremler()
    {
        using var db = SeededDb();
        var page = Ok(await new EarthquakesController(db).GetList(
            new EarthquakeQuery { Province = "Malatya", Period = TimePeriod.Son30Gun }, default));

        Assert.Equal(new[] { "1", "2" }, page.Items.Select(e => e.EventId)); // 60 gün önceki dışarıda
    }

    [Fact]
    public async Task GetList_BuyuklukVeDerinlikFiltresi_Uygulanir()
    {
        using var db = SeededDb();
        var page = Ok(await new EarthquakesController(db).GetList(
            new EarthquakeQuery { MinMagnitude = 3, MaxMagnitude = 4.5, MaxDepthKm = 15 }, default));

        // 4.2'lik deprem 25 km derinlikte olduğu için elenir
        Assert.Equal(new[] { "5", "3" }, page.Items.Select(e => e.EventId));
    }

    [Fact]
    public async Task GetList_BuyukluguGoreSiralama_EnBuyukEnUstte()
    {
        using var db = SeededDb();
        var page = Ok(await new EarthquakesController(db).GetList(
            new EarthquakeQuery { SortBy = SortOption.Buyukluk }, default));

        Assert.Equal(new[] { 5.0, 4.2, 3.1, 3.0, 2.5 }, page.Items.Select(e => e.Magnitude));
    }

    [Fact]
    public async Task GetList_Sayfalama_IstenenSayfaVeToplamSayiDoner()
    {
        using var db = SeededDb();
        var page = Ok(await new EarthquakesController(db).GetList(
            new EarthquakeQuery { Page = 2, PageSize = 2 }, default));

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(new[] { "5", "2" }, page.Items.Select(e => e.EventId));
    }

    // ---------- İl özeti ----------

    [Fact]
    public async Task ProvinceSummary_DepremYoksa_HataDegilSifirOzetDoner()
    {
        using var db = SeededDb();
        var result = await new SummaryController(db).GetProvinceSummary("Van", TimePeriod.Son30Gun);

        var summary = Assert.IsType<ProvinceSummaryDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("Van", summary.Province);
        Assert.Equal(0, summary.TotalCount);
        Assert.Null(summary.Strongest);
    }

    [Fact]
    public async Task ProvinceSummary_SayiEnBuyukVeBuyuklukGruplariniHesaplar()
    {
        using var db = SeededDb();
        var result = await new SummaryController(db).GetProvinceSummary("malatya", TimePeriod.Son30Gun);

        var summary = Assert.IsType<ProvinceSummaryDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(3.35, summary.AverageMagnitude!.Value, 2);
        Assert.Equal(4.2, summary.Strongest!.Magnitude);
        Assert.Equal("1", summary.Latest!.EventId);
        Assert.Equal(1, summary.Bands.Below3);
        Assert.Equal(0, summary.Bands.From3To4);
        Assert.Equal(1, summary.Bands.From4To5);
        Assert.Equal(0, summary.Bands.Above5);
    }

    [Fact]
    public async Task ProvinceSummary_BilinmeyenIl_BadRequest()
    {
        using var db = SeededDb();
        var result = await new SummaryController(db).GetProvinceSummary("Atlantis");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
