using DepremTakip.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Services;

// NÖBETÇİ: Uygulama açık olduğu sürece arka planda çalışır,
// belirli aralıklarla AFAD'ı kontrol edip yeni depremleri kaydeder.
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
            // Önce hemen bir kez çalış, sonra her turda tekrar et.
            do
            {
                await SyncOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
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

            // Defter boşsa son 7 günü doldur.
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
}