namespace DepremTakip.Api.Models;

public class Earthquake
{
    public int Id { get; set; }

    // AFAD'ın her depreme verdiği benzersiz numara. Aynı depremi iki kez kaydetmemek için kullanacağız.
    public string EventId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }   // Deprem zamanı (UTC)

    public double Magnitude { get; set; }          // Büyüklük, ör. 4.2
    public string MagnitudeType { get; set; } = string.Empty; // ML, Mw...
    public double DepthKm { get; set; }            // Derinlik (km)

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string Location { get; set; } = string.Empty; // ör. "Sındırgı (Balıkesir)"
    public string? Province { get; set; }   // İl (boş gelebilir, o yüzden ?)
    public string? District { get; set; }   // İlçe

    public DateTime CreatedAtUtc { get; set; } // Bizim veritabanına eklendiği zaman
}
