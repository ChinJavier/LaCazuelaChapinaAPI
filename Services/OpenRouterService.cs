// =============================================
// ARCHIVO: Services/OpenRouterService.cs
// Servicio para integración con LLM via OpenRouter
// =============================================

using System.Text;
using System.Text.Json;
using LaCazuelaChapina.API.DTOs.LLM;

namespace LaCazuelaChapina.API.Services
{
    /// <summary>
    /// Servicio para integración con modelos LLM gratuitos via OpenRouter
    /// </summary>
    public interface IOpenRouterService
    {
        Task<string> GenerarRecomendacionCombo(ComboRecommendationRequest request);
        Task<string> AnalyzeVentasPatterns(VentasAnalysisRequest request);
        Task<string> GenerateInventoryAlert(InventoryAlertRequest request);
        Task<string> CreateMarketingContent(MarketingContentRequest request);
    }

    public class OpenRouterService : IOpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterService> _logger;
        private readonly string? _apiKey;

        public OpenRouterService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouterService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenRouter:ApiKey"];

            // Configurar headers por defecto
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://lacazuelachapina.com");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "La Cazuela Chapina");
        }

        /// <summary>
        /// Genera recomendaciones de combos personalizadas usando LLM
        /// </summary>
        public async Task<string> GenerarRecomendacionCombo(ComboRecommendationRequest request)
        {
            try
            {
                var prompt = $@"
Eres un experto en gastronomía guatemalteca especializado en tamales y bebidas tradicionales. 
Ayuda a crear recomendaciones de combos para 'La Cazuela Chapina'.

CONTEXTO DEL CLIENTE:
- Época del año: {request.Epoca}
- Presupuesto aproximado: Q{request.PresupuestoMaximo}
- Número de personas: {request.NumeroPersonas}
- Preferencias especiales: {request.PreferenciasEspeciales}

PRODUCTOS DISPONIBLES:
Tamales: Masa (maíz amarillo, blanco, arroz), Rellenos (recado rojo de cerdo, negro de pollo, chipilín vegetariano, mezcla chuchito), Envolturas (hoja plátano, tusa maíz), Picante (sin chile, suave, chapín)

Bebidas: Atol de elote, Atole shuco, Pinol, Cacao batido. Endulzantes (panela, miel, sin azúcar), Toppings (malvaviscos, canela, ralladura cacao)

TAREA:
Recomienda un combo personalizado considerando:
1. Tradiciones guatemaltecas de la época
2. Equilibrio nutricional y sabores
3. Optimización del presupuesto
4. Experiencia gastronómica auténtica

Responde en formato JSON con esta estructura:
{{
    ""nombre_combo"": ""Nombre atractivo del combo"",
    ""descripcion"": ""Descripción detallada"",
    ""componentes"": [
        {{
            ""tipo"": ""tamal"" o ""bebida"",
            ""cantidad"": número,
            ""especificaciones"": ""detalles de personalización""
        }}
    ],
    ""precio_estimado"": número,
    ""razon_recomendacion"": ""Por qué es perfecto para este cliente""
}}";

                var response = await CallOpenRouter(prompt, "meta-llama/llama-3.1-8b-instruct:free");

                _logger.LogInformation("Recomendación de combo generada para {NumeroPersonas} personas en {Epoca}",
                    request.NumeroPersonas, request.Epoca);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando recomendación de combo");
                return GenerateDefaultComboRecommendation(request);
            }
        }

        /// <summary>
        /// Analiza patrones de ventas usando LLM para insights de negocio
        /// </summary>
        public async Task<string> AnalyzeVentasPatterns(VentasAnalysisRequest request)
        {
            try
            {
                var prompt = $@"
Eres un analista de datos especializado en negocios gastronómicos guatemaltecos.
Analiza estos datos de ventas de 'La Cazuela Chapina':

DATOS DE VENTAS:
- Ventas totales del mes: Q{request.VentasTotalesMes}
- Tamales más vendidos: {string.Join(", ", request.TamalesMasVendidos)}
- Bebidas por horario: {string.Join(", ", request.BebidasPorHorario)}
- Proporción picante vs no picante: {request.ProporcionPicante}% prefiere picante
- Desperdicios principales: {string.Join(", ", request.DesperdiciosPrincipales)}

CONTEXTO:
- Mes actual: {request.MesActual}
- Sucursal: {request.NombreSucursal}
- Días analizados: {request.DiasAnalizados}

TAREA:
Proporciona insights accionables sobre:
1. Tendencias de consumo identificadas
2. Oportunidades de optimización
3. Recomendaciones para reducir desperdicios
4. Estrategias para aumentar ventas
5. Ajustes estacionales recomendados

Responde en formato estructurado y fácil de entender para el gerente de la sucursal.";

                var response = await CallOpenRouter(prompt, "google/gemma-2-9b-it:free");

                _logger.LogInformation("Análisis de patrones de ventas generado para {Sucursal}", request.NombreSucursal);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analizando patrones de ventas");
                return "Error generando análisis. Revisar datos manualmente.";
            }
        }

        /// <summary>
        /// Genera alertas inteligentes de inventario
        /// </summary>
        public async Task<string> GenerateInventoryAlert(InventoryAlertRequest request)
        {
            try
            {
                var prompt = $@"
Eres el sistema inteligente de gestión de inventario de 'La Cazuela Chapina'.
Genera una alerta prioritizada para el siguiente problema de stock:

PROBLEMA DETECTADO:
- Materia prima: {request.MateriaPrima}
- Stock actual: {request.StockActual} {request.UnidadMedida}
- Stock mínimo: {request.StockMinimo} {request.UnidadMedida}
- Días estimados para agotarse: {request.DiasEstimadosAgotamiento}
- Proveedor principal: {request.ProveedorPrincipal}

CONTEXTO OPERATIVO:
- Sucursal: {request.NombreSucursal}
- Productos afectados: {string.Join(", ", request.ProductosAfectados)}
- Demanda promedio diaria: {request.DemandaPromediaDiaria} {request.UnidadMedida}

TAREA:
Genera una alerta que incluya:
1. Nivel de criticidad (CRÍTICO/ALTO/MEDIO)
2. Impacto en la operación
3. Acción recomendada inmediata
4. Cantidad sugerida para reorden
5. Proveedores alternativos si es necesario

Responde en formato de alerta ejecutiva, clara y accionable.";

                var response = await CallOpenRouter(prompt, "mistralai/mistral-7b-instruct:free");

                _logger.LogInformation("Alerta de inventario generada para {MateriaPrima} en {Sucursal}",
                    request.MateriaPrima, request.NombreSucursal);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando alerta de inventario");
                return $"ALERTA CRÍTICA: Stock bajo de {request.MateriaPrima}. Revisar inmediatamente.";
            }
        }

        /// <summary>
        /// Genera contenido de marketing contextual
        /// </summary>
        public async Task<string> CreateMarketingContent(MarketingContentRequest request)
        {
            try
            {
                var prompt = $@"
Eres un especialista en marketing gastronómico guatemalteco para 'La Cazuela Chapina'.
Crea contenido promocional auténtico y atractivo.

PARÁMETROS:
- Tipo de contenido: {request.TipoContenido}
- Ocasión especial: {request.OcasionEspecial}
- Productos a destacar: {string.Join(", ", request.ProductosDestacados)}
- Público objetivo: {request.PublicoObjetivo}
- Tono deseado: {request.TonoDeseado}

IDENTIDAD DE MARCA:
- Autenticidad guatemalteca
- Tradición familiar
- Calidad artesanal
- Calidez y hospitalidad

TAREA:
Genera contenido que incluya:
1. Texto principal atractivo
2. Call-to-action efectivo
3. Hashtags relevantes
4. Sugerencias de imagen/visual

El contenido debe evocar nostalgia, tradición y el sabor auténtico de Guatemala.";

                var response = await CallOpenRouter(prompt, "meta-llama/llama-3.1-8b-instruct:free");

                _logger.LogInformation("Contenido de marketing generado para {TipoContenido} - {OcasionEspecial}",
                    request.TipoContenido, request.OcasionEspecial);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando contenido de marketing");
                return "Error generando contenido. Usar plantillas predefinidas.";
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS
        // =============================================

        private async Task<string> CallOpenRouter(string prompt, string model)
        {
            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error llamando OpenRouter API: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"OpenRouter API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent);

            return openRouterResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "Error procesando respuesta";
        }

        private string GenerateDefaultComboRecommendation(ComboRecommendationRequest request)
        {
            // Fallback en caso de error con LLM
            return JsonSerializer.Serialize(new
            {
                nombre_combo = $"Combo Familiar para {request.NumeroPersonas}",
                descripcion = "Combinación tradicional de tamales y bebidas guatemaltecas",
                componentes = new[]
                {
                    new { tipo = "tamal", cantidad = request.NumeroPersonas, especificaciones = "Mezcla de rellenos tradicionales" },
                    new { tipo = "bebida", cantidad = 2, especificaciones = "Atol de elote y pinol" }
                },
                precio_estimado = Math.Min(request.PresupuestoMaximo, request.NumeroPersonas * 15),
                razon_recomendacion = "Recomendación estándar basada en tradición guatemalteca"
            });
        }
    }

    // =============================================
    // MODELOS PARA OPENROUTER
    // =============================================

    public class OpenRouterResponse
    {
        public Choice[]? Choices { get; set; }
    }

    public class Choice
    {
        public Message? Message { get; set; }
    }

    public class Message
    {
        public string? Content { get; set; }
    }
}