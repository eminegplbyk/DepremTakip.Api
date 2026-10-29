using DepremTakip.Api.Afad;
using DepremTakip.Api.Data;
using DepremTakip.Api.Services;
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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
