using System.Reflection;
using System.Text.Json.Serialization;
using DepremTakip.Api.Afad;
using DepremTakip.Api.Data;
using DepremTakip.Api.Emsc;
using DepremTakip.Api.Services;
using DepremTakip.Api.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// AFAD'a istek atan sınıf: adres burada bir kez tanımlanıyor
builder.Services.AddHttpClient<AfadClient>(client =>
{
    client.BaseAddress = new Uri("https://deprem.afad.gov.tr/apiv2/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Geçmiş deprem verisi için EMSC (Avrupa-Akdeniz Sismoloji Merkezi) istemcisi
builder.Services.AddHttpClient<EmscClient>(client =>
{
    client.BaseAddress = new Uri("https://www.seismicportal.eu/fdsnws/event/1/");
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DepremTakip/1.0");
});

// Deprem verisini veritabanına kaydeden sınıf
builder.Services.AddScoped<EarthquakeImporter>();

// Arka planda sürekli çalışan nöbetçi: AFAD'ı düzenli kontrol eder
builder.Services.AddHostedService<EarthquakeSyncService>();

// Seçenekler (ör. sıralama) sayı yerine adıyla görünsün: "Tarih", "Buyukluk"
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Swagger sayfasının en üstünde görünen API tanıtımı
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DepremTakip API",
        Version = "v1",
        Description =
            "Türkiye'deki depremleri toplayan, saklayan ve filtrelenebilir şekilde sunan REST API.\n\n" +
            "**Veri kaynakları**\n" +
            "- **AFAD:** Canlı veri. Her dakika kontrol edilir, yeni depremler en geç 1 dakikada sisteme düşer.\n" +
            "- **EMSC** (Avrupa-Akdeniz Sismoloji Merkezi): 2023 başından itibaren geçmiş veri. " +
            "EMSC kayıtlarında il bilgisi, depremin en yakın il merkezine göre **yaklaşık** olarak atanır (`source` alanında görünür).\n\n" +
            "**Notlar**\n" +
            "- Tüm uç noktalar giriş gerektirmez.\n" +
            "- Saatler hem UTC (`occurredAtUtc`) hem Türkiye saatiyle (`occurredAtTurkey`) döner.\n" +
            "- Bu sistem bir erken uyarı sistemi **değildir**; deprem sonrası bilgi ve analiz amaçlıdır."
    });

    // Başlıklar Türkçe olsun: controller adı yerine anlaşılır grup adı
    c.TagActionsBy(api => new[]
    {
        api.ActionDescriptor.RouteValues["controller"] switch
        {
            "Earthquakes" => "Depremler",
            "Summary" => "İl Özeti",
            var other => other ?? "Diğer"
        }
    });

    // Kodda "///" ile yazdığımız açıklamalar Swagger'da kutuların altında görünsün
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    // İl kutusu yazı yerine 81 ilden oluşan açılır liste olsun
    c.ParameterFilter<ProvinceDropdownFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
