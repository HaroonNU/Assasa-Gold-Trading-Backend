using System.Text.Json.Serialization;
using GoldTrading.Api.DependencyInjection;
using GoldTrading.Api.Middleware;
using GoldTrading.Application.Configuration;
using GoldTrading.Application.Constants;
using GoldTrading.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration — all business values from appsettings.json ────────────────
builder.Services.AddOptions<PricingOptions>()
    .BindConfiguration(PricingOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<QuoteOptions>()
    .BindConfiguration(QuoteOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<TradingOptions>()
    .BindConfiguration(TradingOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SeedDataOptions>()
    .BindConfiguration(SeedDataOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Infrastructure (repositories, price cache, settlement lock) ───────────────
builder.Services.AddInfrastructure();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddApplicationServices();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(o => o.AddPolicy(ApiMessages.DefaultCorsPolicy, p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ── API ───────────────────────────────────────────────────────────────────────
// Enums serialize as strings (e.g. "Buy", "Pkr") rather than raw integers,
// so the wire contract never leaks magic numbers to API consumers.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc(ApiMessages.ApiVersion, new() { Title = ApiMessages.ApiTitle, Version = ApiMessages.ApiVersion }));

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(ApiMessages.DefaultCorsPolicy);
app.UseAuthorization();
app.MapControllers();
app.Run();

// Exposes Program to GoldTrading.IntegrationTests via WebApplicationFactory<Program>.
public partial class Program { }
