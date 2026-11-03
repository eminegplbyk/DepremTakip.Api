using DepremTakip.Api.Helpers;

namespace DepremTakip.Api.Tests;

// İl eşleştirme ve en yakın il hesabı (Helpers/Provinces.cs)
public class ProvincesTests
{
    [Fact]
    public void All_TurkiyeninTum81Ilini_TekrarsizIcerir()
    {
        Assert.Equal(81, Provinces.All.Length);
        Assert.Equal(81, Provinces.All.Distinct().Count());
    }

    // NFR-09: il adları yazım farklarına toleranslı olmalı
    [Theory]
    [InlineData("İstanbul", "İstanbul")]
    [InlineData("istanbul", "İstanbul")]
    [InlineData("Istanbul", "İstanbul")]
    [InlineData("ISTANBUL", "İstanbul")]
    [InlineData("kutahya", "Kütahya")]
    [InlineData("  Şırnak  ", "Şırnak")]
    [InlineData("sanliurfa", "Şanlıurfa")]
    public void Match_FarkliYazimlari_ResmiIlAdinaCevirir(string input, string expected)
    {
        Assert.Equal(expected, Provinces.Match(input));
    }

    [Theory]
    [InlineData("Atlantis")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Match_BilinmeyenVeyaBosIl_NullDoner(string? input)
    {
        Assert.Null(Provinces.Match(input));
    }

    [Fact]
    public void Nearest_IlMerkezindekiNokta_OIliDoner()
    {
        Assert.Equal("İzmir", Provinces.Nearest(38.42, 27.14, 100));
    }

    [Fact]
    public void Nearest_Simav_KutahyaDegilUsakaAtanir()
    {
        // İş analizi Bulgu 3: Simav (Kütahya) Uşak il merkezine daha yakın.
        // Bu test, il atamasının YAKLAŞIK olduğunu belgeler.
        Assert.Equal("Uşak", Provinces.Nearest(39.09, 28.98, 100));
    }

    [Fact]
    public void Nearest_6Subat2023EmscMerkezUssu_GaziantepeAtanir()
    {
        // İş analizi Bulgu 4: 7.8'lik depremin EMSC koordinatı Gaziantep merkezine daha yakın.
        Assert.Equal("Gaziantep", Provinces.Nearest(37.17, 37.08, 100));
    }

    [Fact]
    public void Nearest_YurtDisiNokta_MaxKmdenUzaksaNullDoner()
    {
        // Atina: en yakın il merkezi (İzmir) ~300 km uzakta
        Assert.Null(Provinces.Nearest(37.98, 23.73, 100));
    }
}
