using DepremTakip.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Services;

// NÖBETÇİ: Uygulama açık olduğu sürece arka planda çalışır,
// belirli aralıklarla AFAD'ı kontrol edip yeni depremleri kaydeder.
// Ayrıca ilk açılışlarda geçmiş veriyi (varsayılan 1 yıl) bir kereliğine doldurur.
public class EarthquakeSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EarthquakeSyncService> _logger;
    private readonly IConfiguration _config;

    public EarthquakeSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<EarthquakeSyncService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    // Uygulama açılınca .NET bu metodu otomatik başlatır.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kaç dakikada bir kontrol edilecek? Ayar dosyasında yoksa 1 dakika.
        var minutes = _config.GetValue("Sync:IntervalMinutes", 1);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));

        _logger.LogInformation("Nöbetçi göreve başladı: AFAD her {Minutes} dakikada bir kontrol edilecek.", minutes);

        try
        {
            // 1) Önce güncel depremleri al.
            await SyncOnceAsync(stoppingToken);

            // 2) Geçmiş veri eksikse bir kereliğine tamamla.
            await BackfillHistoryAsync(stoppingToken);

            // 3) Sonra her turda güncel kontrolü tekrarla.
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SyncOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapatılıyor; nöbetçi görevini bırakıyor. Sorun yok.
        }
    }

    // Tek bir tur: AFAD'ı kontrol et, yenileri kaydet.
    private async Task SyncOnceAsync(CancellationToken ct)
    {
        try
        {
            // Nöbetçi sürekli çalışıyor ama veritabanı bağlantısı kısa ömürlü olmalı.
            // Bu yüzden her turda yeni bir "çalışma alanı" (scope) açıp tur bitince kapatıyoruz.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var importer = scope.ServiceProvider.GetRequiredService<EarthquakeImporter>();

            var now = DateTime.UtcNow;

            // Defterdeki en son deprem ne zaman?
            var lastQuake = await db.Earthquakes.MaxAsync(e => (DateTime?)e.OccurredAtUtc, ct);

            // Defter boşsa son 7 günü al (geçmişin geri kalanını BackfillHistoryAsync tamamlar).
            // Doluysa en son depremden 6 saat geriye git (AFAD'ın düzelttiği büyüklükleri yakalamak için).
            var start = lastQuake.HasValue
                ? lastQuake.Value.AddHours(-6)
                : now.AddDays(-7);

            int fetched = 0, added = 0, updated = 0;

            // Uzun aralıkları gün gün böl, AFAD'ı yormayalım.
            for (var from = start; from < now; from = from.AddDays(1))
            {
                var to = from.AddDays(1) < now ? from.AddDays(1) : now;
                var result = await importer.ImportAsync(from, to, ct);
                fetched += result.Fetched;
                added += result.Added;
                updated += result.Updated;
            }

            _logger.LogInformation(
                "[{Time}] AFAD kontrolü tamam: {Fetched} deprem geldi, {Added} yeni, {Updated} güncellendi.",
                DateTime.Now.ToString("HH:mm:ss"), fetched, added, updated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // AFAD'a ulaşılamadı vb. Uygulama kapanmasın, bir sonraki turda tekrar denensin.
            _logger.LogError(ex, "AFAD kontrolü başarısız oldu, bir sonraki turda tekrar denenecek.");
        }
    }

    // GEÇMİŞİ DOLDURMA: Defterdeki en eski depremden geriye doğru, gün gün,
    // hedef tarihe (varsayılan 365 gün önce) kadar eksik veriyi tamamlar.
    // Geriye doğru gittiği için yarıda kesilirse bir sonraki açılışta kaldığı yerden devam eder.
    private async Task BackfillHistoryAsync(CancellationToken ct)
    {
        var historyDays = _config.GetValue("Sync:HistoryDays", 365);
        var target = DateTime.UtcNow.Date.AddDays(-historyDays);

        DateTime? earliest;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            earliest = await db.Earthquakes.MinAsync(e => (DateTime?)e.OccurredAtUtc, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Veritabanına ulaşılamadı: geçmişi bu açılışta atla, uygulama çalışmaya devam etsin.
            _logger.LogError(ex, "Geçmiş veri kontrolü yapılamadı, bir sonraki açılışta tekrar denenecek.");
            return;
        }

        if (!earliest.HasValue || earliest.Value <= target.AddDays(1))
        {
            _logger.LogInformation("Geçmiş veri tamam (son {Days} gün veritabanında).", historyDays);
            return;
        }

        var totalDays = (int)Math.Ceiling((earliest.Value - target).TotalDays);
        _logger.LogInformation(
            "Geçmiş veri dolduruluyor: {Days} gün ({Target:yyyy-MM-dd} → {Earliest:yyyy-MM-dd}). Bu işlem birkaç dakika sürebilir.",
            totalDays, target, earliest.Value);

        int dayCount = 0, totalAdded = 0;
        for (var to = earliest.Value; to > target; to = to.AddDays(-1))
        {
            var from = to.AddDays(-1) > target ? to.AddDays(-1) : target;
            try
            {
                // Her gün için yeni scope: bellekte binlerce kayıt birikmesin.
                using var scope = _scopeFactory.CreateScope();
                var importer = scope.ServiceProvider.GetRequiredService<EarthquakeImporter>();
                var result = await importer.ImportAsync(from, to, ct);
                totalAdded += result.Added;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "{From:yyyy-MM-dd} günü alınamadı, atlanıyor.", from);
            }

            dayCount++;
            if (dayCount % 30 == 0)
                _logger.LogInformation("Geçmiş veri: {Done}/{Total} gün tamamlandı, {Added} deprem eklendi.",
                    dayCount, totalDays, totalAdded);

            // AFAD'ı yormamak için her gün arasında kısa bir mola (NFR-06).
            await Task.Delay(250, ct);
        }

        _logger.LogInformation("Geçmiş veri tamamlandı: {Added} deprem eklendi.", totalAdded);
    }
}
