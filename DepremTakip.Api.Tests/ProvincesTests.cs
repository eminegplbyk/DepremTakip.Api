using DepremTakip.Api.Helpers;

namespace DepremTakip.Api.Tests;

public class ProvincesTests
{
    [Fact]
    public void All_TurkiyeninTum81Ilini_TekrarsizIcerir()
    {
        Assert.Equal(81, Provinces.All.Length);
        Assert.Equal(81, Provinces.All.Distinct().Count());
    }

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
        Assert.Equal("Uşak", Provinces.Nearest(39.09, 28.98, 100));
    }

    [Fact]
    public void Nearest_6Subat2023EmscMerkezUssu_GaziantepeAtanir()
    {
        Assert.Equal("Gaziantep", Provinces.Nearest(37.17, 37.08, 100));
    }

    [Fact]
    public void Nearest_YurtDisiNokta_MaxKmdenUzaksaNullDoner()
    {
        Assert.Null(Provinces.Nearest(37.98, 23.73, 100));
    }
}
