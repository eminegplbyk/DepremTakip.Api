using System.ComponentModel.DataAnnotations;

namespace DepremTakip.Api.Dtos;

/// <summary>Liste sıralama seçenekleri.</summary>
public enum SortOption
{
    /// <summary>En yeni deprem en üstte.</summary>
    Tarih,
    /// <summary>En büyük deprem en üstte.</summary>
    Buyukluk
}

/// <summary>Hazır zaman aralıkları.</summary>
public enum TimePeriod
{
    Son24Saat,
    Son7Gun,
    Son30Gun,
    Son90Gun,
    Son1Yil
}

public class EarthquakeQuery
{
    /// <summary>İl. Listeden seçin; boş bırakırsanız tüm iller gelir.</summary>
    public string? Province { get; set; }

    /// <summary>Hazır zaman aralığı (ör. Son30Gun). Bunu seçerseniz From/To boş kalmalı.</summary>
    public TimePeriod? Period { get; set; }

    /// <summary>Özel aralık başlangıcı (yıl-ay-gün, ör. 2026-10-25). Sadece Period boşsa kullanın.</summary>
    public DateTime? From { get; set; }

    /// <summary>Özel aralık bitişi (yıl-ay-gün, ör. 2026-10-31). Sadece Period boşsa kullanın.</summary>
    public DateTime? To { get; set; }

    /// <summary>En düşük büyüklük, ör. 4 yazarsanız 4.0 ve üzeri gelir.</summary>
    public double? MinMagnitude { get; set; }

    /// <summary>En yüksek büyüklük, ör. 5</summary>
    public double? MaxMagnitude { get; set; }

    /// <summary>En fazla derinlik (km), ör. 10 yazarsanız 10 km'den sığ depremler gelir.</summary>
    public double? MaxDepthKm { get; set; }

    /// <summary>Sıralama. Boş bırakırsanız en yeni deprem en üstte.</summary>
    public SortOption? SortBy { get; set; }

    /// <summary>Kaçıncı sayfa. Boş bırakırsanız 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Sayfada kaç deprem olsun (1-100). Boş bırakırsanız 20.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public class EarthquakeDto
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    /// <summary>Verinin kaynağı: AFAD (canlı) veya EMSC (geçmiş veri, il bilgisi yaklaşık).</summary>
    public string Source { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    /// <summary>Deprem zamanı, Türkiye saatiyle (UTC+3).</summary>
    public DateTime OccurredAtTurkey { get; set; }
    public double Magnitude { get; set; }
    public string MagnitudeType { get; set; } = string.Empty;
    public double DepthKm { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string? District { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
