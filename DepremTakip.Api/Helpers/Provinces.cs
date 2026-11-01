namespace DepremTakip.Api.Helpers;

// Türkiye'nin 81 ili ve esnek il eşleştirme.
public static class Provinces
{
    public static readonly string[] All =
    {
        "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya",
        "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik",
        "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum",
        "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir",
        "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul",
        "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kilis",
        "Kırıkkale", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa",
        "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye",
        "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt", "Sinop", "Şırnak", "Sivas",
        "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"
    };

    // Kullanıcının yazdığı il adını resmi yazımına çevirir.
    // "istanbul", "Istanbul", "ISTANBUL" -> "İstanbul"; "kutahya" -> "Kütahya".
    // Eşleşme yoksa null döner.
    public static string? Match(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var key = Normalize(input);
        return All.FirstOrDefault(p => Normalize(p) == key);
    }

    // Türkçe karakterleri sadeleştirip küçük harfe çevirir: "İzmir" -> "izmir", "Şırnak" -> "sirnak".
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
