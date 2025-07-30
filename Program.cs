// =============================================
// PROGRAM.CS - Configuración Principal
// La Cazuela Chapina API
// =============================================

using Microsoft.EntityFrameworkCore;
using LaCazuelaChapina.API.Data;
using FluentValidation;
using System.Reflection;
using LaCazuelaChapina.API.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// CONFIGURACIÓN DE SERVICIOS
// =============================================

// 1. Configurar Entity Framework con PostgreSQL
builder.Services.AddDbContext<CazuelaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=lacazuelachapina;Username=postgres;Password=postgres";
    
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
    
    // Solo en desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// 2. Configurar AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// 3. Configurar FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// 4. Configurar servicios de aplicación
builder.Services.AddScoped<IOpenRouterService, OpenRouterService>();

// 5. Configurar Controllers con validación
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Personalizar respuestas de validación
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage);

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                message = "Errores de validación",
                errors = errors
            });
        };
    });

// 6. Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "La Cazuela Chapina API",
        Version = "v1",
        Description = "Sistema integral para tamales y bebidas tradicionales guatemaltecas",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "La Cazuela Chapina",
            Email = "info@lacazuelachapina.com"
        }
    });

    // Incluir comentarios XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Configurar ENUMs como strings
    c.SchemaFilter<EnumSchemaFilter>();
});

// 7. Configurar CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("https://lacazuelachapina.com", "https://admin.lacazuelachapina.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 8. Configurar HttpClient para OpenRouter
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>("OpenRouter", client =>
{
    client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
    client.DefaultRequestHeaders.Add("User-Agent", "LaCazuelaChapina/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 9. Configurar HttpClient para integraciones LLM
builder.Services.AddHttpClient("OpenRouter", client =>
{
    client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
    client.DefaultRequestHeaders.Add("User-Agent", "LaCazuelaChapina/1.0");
});

// 10. Configurar logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        config.SetMinimumLevel(LogLevel.Debug);
    }
});

// 11. Configurar compresión de respuestas
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// =============================================
// CONFIGURACIÓN DEL PIPELINE
// =============================================

// 1. Manejo de excepciones
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "La Cazuela Chapina API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 2. Redirección HTTPS
app.UseHttpsRedirection();

// 3. Compresión
app.UseResponseCompression();

// 4. CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors("ProductionPolicy");
}

// 5. Autenticación y autorización (pendiente)
// app.UseAuthentication();
// app.UseAuthorization();

// 6. Mapear controllers
app.MapControllers();

// 7. Endpoint de salud
app.MapGet("/health", async (CazuelaDbContext context) =>
{
    try
    {
        // Verificar conexión a base de datos
        await context.Database.CanConnectAsync();
        
        // Obtener estadísticas básicas
        var sucursales = await context.Sucursales.CountAsync();
        var productos = await context.Productos.CountAsync();
        var ventasHoy = await context.Ventas
            .Where(v => v.FechaVenta.Date == DateTime.UtcNow.Date)
            .CountAsync();

        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = "connected",
            sucursales,
            productos,
            ventasHoy,
            version = "1.0.0"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Health Check Failed",
            detail: ex.Message,
            statusCode: 503
        );
    }
})
.WithName("HealthCheck")
.WithTags("Health");

// 8. Endpoint de prueba
app.MapGet("/", () => new
{
    message = "¡Bienvenido a La Cazuela Chapina API!",
    documentation = "/swagger",
    health = "/health",
    timestamp = DateTime.UtcNow
})
.WithName("Root")
.WithTags("Info");

// =============================================
// INICIALIZACIÓN Y MIGRACIÓN
// =============================================

// Aplicar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CazuelaDbContext>();
    
    try
    {
        // Verificar conexión
        await context.Database.CanConnectAsync();
        app.Logger.LogInformation("✅ Conexión a PostgreSQL exitosa");
        
        // Aplicar migraciones pendientes
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            app.Logger.LogInformation($"Aplicando {pendingMigrations.Count()} migraciones...");
            await context.Database.MigrateAsync();
            app.Logger.LogInformation("✅ Migraciones aplicadas correctamente");
        }
        else
        {
            app.Logger.LogInformation("✅ Base de datos actualizada");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "❌ Error conectando a la base de datos");
        throw;
    }
}

app.Logger.LogInformation("🚀 La Cazuela Chapina API iniciada correctamente");

// Obtener las URLs reales donde está corriendo la aplicación
var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "puerto asignado automáticamente";
app.Logger.LogInformation($"🌐 API disponible en: {urls}");

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation($"📚 Swagger UI disponible en: {urls}/swagger (o en la raíz: {urls})");
}

app.Run();

// =============================================
// CLASES DE UTILIDAD
// =============================================

/// <summary>
/// Filtro para mostrar ENUMs como strings en Swagger
/// </summary>
public class EnumSchemaFilter : Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiSchema schema, Swashbuckle.AspNetCore.SwaggerGen.SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            schema.Type = "string";
            schema.Format = null;
            
            foreach (var enumValue in Enum.GetValues(context.Type))
            {
                schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(enumValue.ToString()));
            }
        }
    }
}