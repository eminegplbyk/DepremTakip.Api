namespace DepremTakip.Api.Dtos;

// İL ÖZETİ: "Malatya, son 30 gün" butonunun arkasındaki bilgi (FR-08).
public class ProvinceSummaryDto
{
    public string Province { get; set; } = string.Empty;
    public TimePeriod Period { get; set; }
    public DateTime FromUtc { get; set; }            // Özetin başladığı zaman
    public DateTime ToUtc { get; set; }              // Özetin bittiği zaman (şimdi)

    public int TotalCount { get; set; }              // Toplam deprem sayısı
    public double? AverageMagnitude { get; set; }    // Ortalama büyüklük (deprem yoksa boş)
    public EarthquakeDto? Strongest { get; set; }    // En büyük deprem
    public EarthquakeDto? Latest { get; set; }       // En son deprem

    public MagnitudeBands Bands { get; set; } = new();
}

// Büyüklük gruplarına göre deprem sayıları.
public class MagnitudeBands
{
    public int Below3 { get; set; }      // 3'ten küçük: genelde hissedilmez
    public int From3To4 { get; set; }    // 3–4: hissedilir
    public int From4To5 { get; set; }    // 4–5: belirgin, hafif hasar olabilir
    public int Above5 { get; set; }      // 5 ve üzeri: hasar riski
}