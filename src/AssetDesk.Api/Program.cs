using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Services;
using AssetDesk.Api.Infrastructure.Identity;
using AssetDesk.Api.Infrastructure.Persistence;
using AssetDesk.Api.Infrastructure.Repositories;
using AssetDesk.Api.Infrastructure.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

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
// Los enums viajan como texto ("InProgress"), no como numero: un 1 en el JSON no le dice
// nada a quien consume la API y se rompe si el enum se reordena.
//
// El convertidor NO se registra aca, sino como atributo en cada enum. La razon: MVC y el
// generador de OpenAPI leen objetos de configuracion JSON distintos, asi que un convertidor
// puesto en AddJsonOptions hacia que la API respondiera "High" mientras el schema publicado
// seguia diciendo "integer". El atributo viaja con el tipo y las dos cosas coinciden.
builder.Services.AddControllers();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddOpenApi(options =>
{
    // Sin esto el documento toma el nombre del assembly como titulo. Swagger es lo primero
    // que abre alguien que evalua la API, asi que la portada se escribe a proposito.
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "AssetDesk API",
            Version = "v1",
            Description =
                "Gestion de activos de TI y tickets de soporte.\n\n" +
                "Tres reglas de negocio se obligan desde el dominio y devuelven **409 Conflict** " +
                "cuando se intentan violar:\n\n" +
                "1. No se cierra un ticket que tiene subtareas abiertas.\n" +
                "2. Un activo dado de baja no acepta tickets nuevos.\n" +
                "3. Todo cambio queda registrado con autor y momento.\n\n" +
                "Los codigos de error separan dos preguntas distintas: **400** es un request mal " +
                "formado, **409** es un request correcto que el estado actual no permite.\n\n" +
                "Mientras no haya autenticacion, el autor de cada operacion se manda en la " +
                "cabecera `X-Acting-User`."
        };
        return Task.CompletedTask;
    });

    // La cabecera X-Acting-User la consume HeaderCurrentUser, no un parametro de accion, asi
    // que el generador no la puede descubrir sola y Swagger no ofrecia donde escribirla.
    // Se declara aca en lugar de ensuciar la firma de cada controller con un [FromHeader]
    // que nadie usa. Solo en las operaciones que mutan: en un GET no hay nada que auditar.
    options.AddOperationTransformer((operation, context, ct) =>
    {
        var method = context.Description.HttpMethod;
        if (method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = HeaderCurrentUser.HeaderName,
                In = ParameterLocation.Header,
                Required = false,
                Description =
                    "Quien ejecuta la operacion. Queda como autor en la bitacora. " +
                    "Provisional: cuando entre la autenticacion, el autor sale del token " +
                    "y esta cabecera desaparece.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                Example = "jarango"
            });
        }
        return Task.CompletedTask;
    });
});

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
