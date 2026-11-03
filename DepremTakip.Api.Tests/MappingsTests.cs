using DepremTakip.Api.Dtos;
using DepremTakip.Api.Helpers;
using DepremTakip.Api.Models;

namespace DepremTakip.Api.Tests;

// Ortak dönüşümler (Helpers/Mappings.cs)
public class MappingsTests
{
    [Theory]
    [InlineData(TimePeriod.Son24Saat, 1)]
    [InlineData(TimePeriod.Son7Gun, 7)]
    [InlineData(TimePeriod.Son30Gun, 30)]
    [InlineData(TimePeriod.Son90Gun, 90)]
    [InlineData(TimePeriod.Son1Yil, 365)]
    public void ToTimeSpan_HazirDonemi_DogruGunSayisinaCevirir(TimePeriod period, int expectedDays)
    {
        Assert.Equal(TimeSpan.FromDays(expectedDays), period.ToTimeSpan());
    }

    [Fact]
    public void ToDto_TurkiyeSaati_UtcArti3Olur()
    {
        // BR-06: zamanlar UTC saklanır, Türkiye saatiyle (UTC+3) gösterilir
        var quake = new Earthquake
        {
            EventId = "1",
            Source = "EMSC",
            OccurredAtUtc = new DateTime(2023, 2, 6, 1, 17, 0, DateTimeKind.Utc),
            Magnitude = 7.8,
            Province = "Gaziantep"
        };

        var dto = Mappings.ToDto.Compile()(quake);

        Assert.Equal(new DateTime(2023, 2, 6, 4, 17, 0), dto.OccurredAtTurkey);
        Assert.Equal("EMSC", dto.Source);
        Assert.Equal(7.8, dto.Magnitude);
        Assert.Equal("Gaziantep", dto.Province);
    }
}
