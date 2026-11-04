namespace DepremTakip.Api.Helpers;

public static class Provinces
{
    private static readonly (string Name, double Lat, double Lon)[] Centers =
    {
        ("Adana", 37.00, 35.32), ("Adıyaman", 37.76, 38.28), ("Afyonkarahisar", 38.76, 30.54), ("Ağrı", 39.72, 43.05),
        ("Aksaray", 38.37, 34.03), ("Amasya", 40.65, 35.83), ("Ankara", 39.93, 32.86), ("Antalya", 36.89, 30.71),
        ("Ardahan", 41.11, 42.70), ("Artvin", 41.18, 41.82), ("Aydın", 37.85, 27.84), ("Balıkesir", 39.65, 27.88),
        ("Bartın", 41.63, 32.34), ("Batman", 37.88, 41.13), ("Bayburt", 40.26, 40.23), ("Bilecik", 40.14, 29.98),
        ("Bingöl", 38.88, 40.50), ("Bitlis", 38.40, 42.11), ("Bolu", 40.74, 31.61), ("Burdur", 37.72, 30.29),
        ("Bursa", 40.19, 29.06), ("Çanakkale", 40.15, 26.41), ("Çankırı", 40.60, 33.62), ("Çorum", 40.55, 34.95),
        ("Denizli", 37.78, 29.09), ("Diyarbakır", 37.91, 40.24), ("Düzce", 40.84, 31.16), ("Edirne", 41.68, 26.56),
        ("Elazığ", 38.68, 39.22), ("Erzincan", 39.75, 39.49), ("Erzurum", 39.90, 41.27), ("Eskişehir", 39.78, 30.52),
        ("Gaziantep", 37.07, 37.38), ("Giresun", 40.91, 38.39), ("Gümüşhane", 40.46, 39.48), ("Hakkari", 37.58, 43.74),
        ("Hatay", 36.20, 36.16), ("Iğdır", 39.92, 44.04), ("Isparta", 37.76, 30.55), ("İstanbul", 41.01, 28.98),
        ("İzmir", 38.42, 27.14), ("Kahramanmaraş", 37.58, 36.94), ("Karabük", 41.20, 32.62), ("Karaman", 37.18, 33.22),
        ("Kars", 40.60, 43.10), ("Kastamonu", 41.39, 33.78), ("Kayseri", 38.73, 35.48), ("Kilis", 36.72, 37.12),
        ("Kırıkkale", 39.85, 33.51), ("Kırklareli", 41.73, 27.22), ("Kırşehir", 39.15, 34.16), ("Kocaeli", 40.77, 29.92),
        ("Konya", 37.87, 32.48), ("Kütahya", 39.42, 29.98), ("Malatya", 38.35, 38.31), ("Manisa", 38.61, 27.43),
        ("Mardin", 37.31, 40.74), ("Mersin", 36.81, 34.64), ("Muğla", 37.22, 28.36), ("Muş", 38.74, 41.49),
        ("Nevşehir", 38.62, 34.71), ("Niğde", 37.97, 34.68), ("Ordu", 40.98, 37.88), ("Osmaniye", 37.07, 36.25),
        ("Rize", 41.02, 40.52), ("Sakarya", 40.78, 30.40), ("Samsun", 41.29, 36.33), ("Şanlıurfa", 37.16, 38.79),
        ("Siirt", 37.93, 41.94), ("Sinop", 42.03, 35.15), ("Şırnak", 37.52, 42.46), ("Sivas", 39.75, 37.02),
        ("Tekirdağ", 40.98, 27.51), ("Tokat", 40.31, 36.55), ("Trabzon", 41.00, 39.72), ("Tunceli", 39.11, 39.55),
        ("Uşak", 38.68, 29.41), ("Van", 38.49, 43.38), ("Yalova", 40.65, 29.27), ("Yozgat", 39.82, 34.81),
        ("Zonguldak", 41.45, 31.79)
    };

    public static readonly string[] All = Centers.Select(c => c.Name).ToArray();

    public static string? Match(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var key = Normalize(input);
        return All.FirstOrDefault(p => Normalize(p) == key);
    }

    public static string? Nearest(double lat, double lon, double maxKm)
    {
        var best = Centers
            .Select(c => (c.Name, Km: DistanceKm(lat, lon, c.Lat, c.Lon)))
            .OrderBy(x => x.Km)
            .First();
        return best.Km <= maxKm ? best.Name : null;
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = ToRad(lat2 - lat1), dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    private static string Normalize(string s)
    {
        var chars = s.Trim().Select(c => c switch
        {
            'İ' or 'I' or 'ı' => 'i',
            'Ş' or 'ş' => 's',
            'Ğ' or 'ğ' => 'g',
            'Ü' or 'ü' => 'u',
            'Ö' or 'ö' => 'o',
            'Ç' or 'ç' => 'c',
            _ => char.ToLowerInvariant(c)
        });
        return new string(chars.ToArray());
    }
}
