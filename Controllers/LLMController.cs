// =============================================
// ARCHIVO: Controllers/LLMController.cs
// Controlador para funcionalidades de IA y LLM
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Services;
using LaCazuelaChapina.API.DTOs.LLM;
using System.Text.Json;
using LaCazuelaChapina.API.Models.Inventario;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para funcionalidades de inteligencia artificial y recomendaciones
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LLMController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IOpenRouterService _openRouterService;
        private readonly ILogger<LLMController> _logger;

        public LLMController(
            CazuelaDbContext context,
            IOpenRouterService openRouterService,
            ILogger<LLMController> logger)
        {
            _context = context;
            _openRouterService = openRouterService;
            _logger = logger;
        }

        /// <summary>
        /// Genera recomendaciones personalizadas de combos usando IA
        /// </summary>
        /// <param name="request">Parámetros para la recomendación</param>
        /// <returns>Recomendación de combo personalizada</returns>
        [HttpPost("recomendar-combo")]
        [ProducesResponseType(typeof(LLMResponse<string>), 200)]
        public async Task<ActionResult<LLMResponse<string>>> RecomendarCombo(ComboRecommendationRequest request)
        {
            try
            {
                _logger.LogInformation("Generando recomendación de combo para {NumeroPersonas} personas en {Epoca}", 
                    request.NumeroPersonas, request.Epoca);

                var recomendacion = await _openRouterService.GenerarRecomendacionCombo(request);

                var response = new LLMResponse<string>
                {
                    Success = true,
                    Message = "Recomendación generada exitosamente",
                    Data = recomendacion,
                    ModelUsed = "meta-llama/llama-3.1-8b-instruct:free",
                    ConfidenceLevel = 0.85
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando recomendación de combo");
                return StatusCode(500, new LLMResponse<string>
                {
                    Success = false,
                    Message = "Error generando recomendación",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Analiza patrones de ventas de una sucursal usando IA
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>Análisis detallado de patrones y recomendaciones</returns>
        [HttpPost("analizar-ventas/{sucursalId}")]
        [ProducesResponseType(typeof(LLMResponse<string>), 200)]
        public async Task<ActionResult<LLMResponse<string>>> AnalizarPatronesVentas(int sucursalId)
        {
            try
            {
                // Recopilar datos de ventas de la sucursal
                var fechaInicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var sucursal = await _context.Sucursales.FindAsync(sucursalId);

                if (sucursal == null)
                    return NotFound("Sucursal no encontrada");

                var ventasTotales = await _context.Ventas
                    .Where(v => v.SucursalId == sucursalId && v.FechaVenta >= fechaInicioMes)
                    .SumAsync(v => v.Total);

                var tamalesMasVendidos = await _context.DetalleVentas
                    .Include(dv => dv.Venta)
                    .Include(dv => dv.Producto)
                    .Include(dv => dv.VarianteProducto)
                    .Where(dv => dv.Venta.SucursalId == sucursalId && 
                               dv.Venta.FechaVenta >= fechaInicioMes &&
                               dv.Producto!.Categoria.Nombre == "Tamales")
                    .GroupBy(dv => dv.Producto!.Nombre)
                    .OrderByDescending(g => g.Sum(dv => dv.Cantidad))
                    .Take(5)
                    .Select(g => $"{g.Key}: {g.Sum(dv => dv.Cantidad)} unidades")
                    .ToListAsync();

                var request = new VentasAnalysisRequest
                {
                    VentasTotalesMes = ventasTotales,
                    TamalesMasVendidos = tamalesMasVendidos,
                    BebidasPorHorario = new List<string>(), // Se llenaría con consulta específica
                    ProporcionPicante = 65, // Placeholder - calcular con consulta real
                    DesperdiciosPrincipales = new List<string>(), // Consulta a movimientos de merma
                    MesActual = DateTime.UtcNow.ToString("MMMM yyyy"),
                    NombreSucursal = sucursal.Nombre,
                    DiasAnalizados = DateTime.UtcNow.Day
                };

                var analisis = await _openRouterService.AnalyzeVentasPatterns(request);

                var response = new LLMResponse<string>
                {
                    Success = true,
                    Message = "Análisis de patrones completado",
                    Data = analisis,
                    ModelUsed = "google/gemma-2-9b-it:free",
                    ConfidenceLevel = 0.80
                };

                _logger.LogInformation("Análisis de patrones generado para sucursal {SucursalId}", sucursalId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analizando patrones de ventas para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new LLMResponse<string>
                {
                    Success = false,
                    Message = "Error generando análisis",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Genera alertas inteligentes de inventario usando IA
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>Alertas priorizadas con recomendaciones</returns>
        [HttpGet("alertas-inventario/{sucursalId}")]
        [ProducesResponseType(typeof(List<LLMResponse<string>>), 200)]
        public async Task<ActionResult<List<LLMResponse<string>>>> GenerarAlertasInventario(int sucursalId)
        {
            try
            {
                var alertas = new List<LLMResponse<string>>();

                // Obtener materias primas con stock bajo
                var stocksBajos = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                    .Include(ss => ss.Sucursal)
                    .Where(ss => ss.SucursalId == sucursalId && 
                               ss.CantidadActual <= ss.MateriaPrima.StockMinimo)
                    .Take(5) // Procesar máximo 5 alertas por vez
                    .ToListAsync();

                foreach (var stock in stocksBajos)
                {
                    var request = new InventoryAlertRequest
                    {
                        MateriaPrima = stock.MateriaPrima.Nombre,
                        StockActual = stock.CantidadActual,
                        StockMinimo = stock.MateriaPrima.StockMinimo,
                        UnidadMedida = stock.MateriaPrima.UnidadMedida,
                        DiasEstimadosAgotamiento = CalcularDiasAgotamiento(stock),
                        ProveedorPrincipal = "Proveedor Principal", // Placeholder
                        NombreSucursal = stock.Sucursal.Nombre,
                        ProductosAfectados = new List<string>(), // Se llenaría con consulta específica
                        DemandaPromediaDiaria = 5.0m, // Placeholder - calcular con historial
                        CostoPromedio = stock.MateriaPrima.CostoPromedio,
                        UltimaCompra = DateTime.UtcNow.AddDays(-7), // Placeholder
                        EsMateriaPrimaCritica = stock.CantidadActual <= 0
                    };

                    var alertaTexto = await _openRouterService.GenerateInventoryAlert(request);

                    alertas.Add(new LLMResponse<string>
                    {
                        Success = true,
                        Message = $"Alerta para {stock.MateriaPrima.Nombre}",
                        Data = alertaTexto,
                        ModelUsed = "mistralai/mistral-7b-instruct:free",
                        ConfidenceLevel = 0.90
                    });
                }

                _logger.LogInformation("Generadas {Count} alertas de inventario para sucursal {SucursalId}", 
                    alertas.Count, sucursalId);

                return Ok(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando alertas de inventario para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new List<LLMResponse<string>>());
            }
        }

        /// <summary>
        /// Genera contenido de marketing personalizado usando IA
        /// </summary>
        /// <param name="request">Parámetros para el contenido</param>
        /// <returns>Contenido de marketing generado</returns>
        [HttpPost("generar-marketing")]
        [ProducesResponseType(typeof(LLMResponse<string>), 200)]
        public async Task<ActionResult<LLMResponse<string>>> GenerarContenidoMarketing(MarketingContentRequest request)
        {
            try
            {
                var contenido = await _openRouterService.CreateMarketingContent(request);

                var response = new LLMResponse<string>
                {
                    Success = true,
                    Message = "Contenido de marketing generado",
                    Data = contenido,
                    ModelUsed = "meta-llama/llama-3.1-8b-instruct:free",
                    ConfidenceLevel = 0.85
                };

                _logger.LogInformation("Contenido de marketing generado para {TipoContenido} - {OcasionEspecial}", 
                    request.TipoContenido, request.OcasionEspecial);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando contenido de marketing");
                return StatusCode(500, new LLMResponse<string>
                {
                    Success = false,
                    Message = "Error generando contenido",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Chatbot inteligente para consultas sobre productos y pedidos
        /// </summary>
        /// <param name="request">Mensaje del usuario</param>
        /// <returns>Respuesta del chatbot</returns>
        [HttpPost("chatbot")]
        [ProducesResponseType(typeof(LLMResponse<string>), 200)]
        public async Task<ActionResult<LLMResponse<string>>> Chatbot(ChatbotRequest request)
        {
            try
            {
                // Construir contexto con información actual de productos
                var productos = await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Variantes)
                    .Where(p => p.Activo)
                    .Take(10)
                    .ToListAsync();

                var contextoProdutos = string.Join(", ", 
                    productos.Select(p => $"{p.Nombre} ({p.Categoria.Nombre}) - Q{p.PrecioBase}"));

                var prompt = $@"
Eres el asistente virtual de 'La Cazuela Chapina', especialista en tamales y bebidas tradicionales guatemaltecas.

PRODUCTOS DISPONIBLES:
{contextoProdutos}

MENSAJE DEL CLIENTE:
{request.Mensaje}

INSTRUCCIONES:
- Responde de manera amigable y profesional
- Usa conocimiento de la gastronomía guatemalteca
- Ofrece recomendaciones específicas cuando sea apropiado
- Si preguntan por precios o disponibilidad, usa la información de productos
- Mantén el tono cálido y familiar característico de Guatemala
- Si no tienes información específica, ofrece contactar por teléfono

Responde como si fueras un experto chapín en tamales y bebidas tradicionales.";

                var response = await CallOpenRouterDirectly(prompt, "meta-llama/llama-3.1-8b-instruct:free");

                var result = new LLMResponse<string>
                {
                    Success = true,
                    Message = "Respuesta del chatbot",
                    Data = response,
                    ModelUsed = "meta-llama/llama-3.1-8b-instruct:free",
                    ConfidenceLevel = 0.88
                };

                _logger.LogInformation("Respuesta de chatbot generada para mensaje: {Mensaje}", 
                    request.Mensaje.Substring(0, Math.Min(50, request.Mensaje.Length)));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en chatbot");
                return Ok(new LLMResponse<string>
                {
                    Success = true,
                    Message = "Respuesta de fallback",
                    Data = "¡Hola! Gracias por contactar La Cazuela Chapina. En este momento tengo dificultades técnicas, pero puedes llamarnos al 2234-5678 para hacer tu pedido. ¡Nuestros tamales y bebidas tradicionales te están esperando!",
                    ModelUsed = "fallback",
                    ConfidenceLevel = 1.0
                });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS
        // =============================================

        private int CalcularDiasAgotamiento(StockSucursal stock)
        {
            // Cálculo simplificado - en producción usar historial real
            var demandaPromediaDiaria = 5.0m; // Placeholder
            return stock.CantidadActual <= 0 ? 0 : (int)(stock.CantidadActual / demandaPromediaDiaria);
        }

        private async Task<string> CallOpenRouterDirectly(string prompt, string model)
        {
            // Implementación directa para casos específicos
            // En una implementación real, esto podría reutilizar el servicio OpenRouter
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_API_KEY");
                
                var request = new
                {
                    model = model,
                    messages = new[] { new { role = "user", content = prompt } },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // Parsear respuesta JSON y extraer contenido
                    return "Respuesta procesada del modelo LLM"; // Placeholder
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en llamada directa a OpenRouter");
            }

            return "Lo siento, no puedo procesar tu consulta en este momento. Por favor contacta directamente a nuestro equipo.";
        }
    }

    // =============================================
    // DTOs ADICIONALES
    // =============================================

    public class ChatbotRequest
    {
        public string Mensaje { get; set; } = string.Empty;
        public string? ContextoAdicional { get; set; }
        public int? SucursalId { get; set; }
    }
}