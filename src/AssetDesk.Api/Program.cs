using System.Text.Json.Serialization;
using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Services;
using AssetDesk.Api.Infrastructure.Identity;
using AssetDesk.Api.Infrastructure.Persistence;
using AssetDesk.Api.Infrastructure.Repositories;
using AssetDesk.Api.Infrastructure.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Base de datos
// ---------------------------------------------------------------------------
// La cadena de conexion NO esta en appsettings.json porque lleva credenciales.
// Sale de user-secrets en desarrollo local o de la variable de entorno
// ConnectionStrings__Default en contenedor. Ver el README.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion 'Default'. Configurala con 'dotnet user-secrets set' " +
        "o con la variable de entorno ConnectionStrings__Default. Ver el README.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        // SQL Server en contenedor tarda unos segundos en aceptar conexiones. Sin reintentos
        // la API se cae al arrancar porque llega antes que la base este lista.
        sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), null);
        sql.MigrationsHistoryTable("__EFMigrationsHistory");
    }));

// ---------------------------------------------------------------------------
// Inyeccion de dependencias
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// Reloj inyectable en lugar de DateTime.UtcNow disperso por el codigo: los tests pueden
// fijarlo y las aserciones sobre timestamps dejan de depender del momento en que corren.
builder.Services.AddSingleton(TimeProvider.System);

// Etapa 1: el actor sale de una cabecera HTTP. Etapa 2: sale de los claims del JWT.
// Cambia solo esta linea; los servicios y las entidades no se enteran.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();

// ---------------------------------------------------------------------------
// Web
// ---------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        // Los enums viajan como texto ("InProgress"), no como numero. Un 1 en el JSON no
        // le dice nada a quien consume la API y se rompe si el enum se reordena.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddOpenApi();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Generador de spec first-party de Microsoft (AddOpenApi) + solo la UI de Swashbuckle.
    // Se evita el generador viejo de Swashbuckle, que es la parte que quedo legacy.
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "AssetDesk API v1");
        o.DocumentTitle = "AssetDesk API";
    });

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    // Fuera de desarrollo si, pero no en Development: la API corre sobre HTTP plano dentro
    // de la red de Docker y un redirect a HTTPS rompe el scrapeo de Prometheus.
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
