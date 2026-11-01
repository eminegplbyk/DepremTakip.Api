using System.Reflection;
using System.Text.Json.Serialization;
using DepremTakip.Api.Afad;
using DepremTakip.Api.Data;
using DepremTakip.Api.Services;
using DepremTakip.Api.Swagger;
using Microsoft.EntityFrameworkCore;

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

// AFAD verisini veritabanına kaydeden sınıf
builder.Services.AddScoped<EarthquakeImporter>();

// Arka planda sürekli çalışan nöbetçi: AFAD'ı düzenli kontrol eder
builder.Services.AddHostedService<EarthquakeSyncService>();

// Seçenekler (ör. sıralama) sayı yerine adıyla görünsün: "Tarih", "Buyukluk"
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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
