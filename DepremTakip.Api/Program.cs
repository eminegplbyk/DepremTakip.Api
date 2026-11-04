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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient<AfadClient>(client =>
{
    client.BaseAddress = new Uri("https://deprem.afad.gov.tr/apiv2/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<EmscClient>(client =>
{
    client.BaseAddress = new Uri("https://www.seismicportal.eu/fdsnws/event/1/");
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DepremTakip/1.0");
});

builder.Services.AddScoped<EarthquakeImporter>();

builder.Services.AddHostedService<EarthquakeSyncService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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

    c.TagActionsBy(api => new[]
    {
        api.ActionDescriptor.RouteValues["controller"] switch
        {
            "Earthquakes" => "Depremler",
            "Summary" => "İl Özeti",
            var other => other ?? "Diğer"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    c.ParameterFilter<ProvinceDropdownFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
