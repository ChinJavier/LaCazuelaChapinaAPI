// =============================================
// ARCHIVO: Services/OpenRouterService.cs
// Servicio de integración con OpenRouter API
// =============================================

using System.Text;
using System.Text.Json;

namespace LaCazuelaChapina.API.Services
{
    /// <summary>
    /// Interfaz para el servicio de OpenRouter
    /// </summary>
    public interface IOpenRouterService
    {
        Task<string> GenerarRespuestaAsync(string prompt, string modelo = "meta-llama/llama-3.2-3b-instruct:free");
        Task<string> GenerarRespuestaSimpleAsync(string prompt);
        Task<List<ModeloDisponible>> ObtenerModelosDisponiblesAsync();
        Task<bool> VerificarConexionAsync();
    }

    /// <summary>
    /// Servicio para integración con OpenRouter API para funcionalidades de IA
    /// </summary>
    public class OpenRouterService : IOpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenRouterService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public OpenRouterService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenRouterService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            // Obtener configuración
            _apiKey = _configuration["OpenRouter:ApiKey"] ?? 
                     Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? 
                     throw new InvalidOperationException("OpenRouter API Key no configurada");
            
            _baseUrl = _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1/";

            // Configurar HttpClient
            ConfigurarHttpClient();
        }

        private void ConfigurarHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://lacazuelachapina.com");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "La Cazuela Chapina API");
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // 60 segundos timeout
        }

        /// <summary>
        /// Genera una respuesta usando el modelo especificado de OpenRouter
        /// </summary>
        public async Task<string> GenerarRespuestaAsync(string prompt, string modelo = "meta-llama/llama-3.2-3b-instruct:free")
        {
            try
            {
                var request = new OpenRouterRequest
                {
                    Model = modelo,
                    Messages = new List<Message>
                    {
                        new Message
                        {
                            Role = "system",
                            Content = "Eres un asistente experto en La Cazuela Chapina, especialista en tamales guatemaltecos y bebidas tradicionales. Responde de manera útil, precisa y amigable."
                        },
                        new Message
                        {
                            Role = "user",
                            Content = prompt
                        }
                    },
                    MaxTokens = 1500,
                    Temperature = 0.7f,
                    TopP = 0.9f,
                    FrequencyPenalty = 0.1f,
                    PresencePenalty = 0.1f
                };

                var jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _logger.LogDebug("Enviando solicitud a OpenRouter con modelo: {Modelo}", modelo);

                var response = await _httpClient.PostAsync("chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error en OpenRouter API: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    
                    throw new HttpRequestException($"Error en OpenRouter API: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                
                var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent, 
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                if (openRouterResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
                {
                    _logger.LogWarning("Respuesta vacía o inválida de OpenRouter");
                    throw new InvalidOperationException("Respuesta vacía o inválida de OpenRouter");
                }

                var resultado = openRouterResponse.Choices.First().Message.Content.Trim();
                
                _logger.LogInformation("Respuesta generada exitosamente con {Tokens} tokens", 
                    openRouterResponse.Usage?.TotalTokens ?? 0);

                return resultado;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout al comunicarse con OpenRouter");
                throw new TimeoutException("Timeout al comunicarse con OpenRouter", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al comunicarse con OpenRouter");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error al procesar respuesta JSON de OpenRouter");
                throw new InvalidOperationException("Error al procesar respuesta de OpenRouter", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en OpenRouterService");
                throw;
            }
        }

        /// <summary>
        /// Obtiene la lista de modelos disponibles en OpenRouter
        /// </summary>
        public async Task<List<ModeloDisponible>> ObtenerModelosDisponiblesAsync()
        {
            try
            {
                _logger.LogDebug("Obteniendo modelos disponibles de OpenRouter");

                var response = await _httpClient.GetAsync("models");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error obteniendo modelos: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Error obteniendo modelos: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                
                var modelosResponse = JsonSerializer.Deserialize<ModelosResponse>(responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                if (modelosResponse?.Data == null)
                {
                    return new List<ModeloDisponible>();
                }

                var modelosDisponibles = modelosResponse.Data
                    .Where(m => m.Id?.Contains("free") == true || m.Pricing?.Prompt == "0") // Solo modelos gratuitos
                    .Select(m => new ModeloDisponible
                    {
                        Id = m.Id ?? "unknown",
                        Nombre = m.Name ?? m.Id ?? "Unknown",
                        Descripcion = m.Description ?? "Sin descripción",
                        EsGratuito = m.Id?.Contains("free") == true || m.Pricing?.Prompt == "0",
                        ContextWindow = m.ContextLength ?? 4096,
                        MaxTokens = m.TopProvider?.MaxCompletionTokens ?? 1000
                    })
                    .OrderBy(m => m.Nombre)
                    .ToList();

                _logger.LogInformation("Obtenidos {Count} modelos gratuitos disponibles", modelosDisponibles.Count);

                return modelosDisponibles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo modelos disponibles");
                return new List<ModeloDisponible>
                {
                    new ModeloDisponible
                    {
                        Id = "meta-llama/llama-3.2-3b-instruct:free",
                        Nombre = "Llama 3.2 3B Instruct (Free)",
                        Descripcion = "Modelo gratuito por defecto",
                        EsGratuito = true,
                        ContextWindow = 4096,
                        MaxTokens = 1000
                    }
                };
            }
        }

        /// <summary>
        /// Verifica la conectividad con OpenRouter
        /// </summary>
        public async Task<bool> VerificarConexionAsync()
        {
            try
            {
                _logger.LogDebug("Verificando conexión con OpenRouter");

                // Hacer una solicitud simple para verificar la conectividad
                var response = await _httpClient.GetAsync("models", new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                
                var isConnected = response.IsSuccessStatusCode;
                
                _logger.LogInformation("Verificación de conexión OpenRouter: {Status}", 
                    isConnected ? "Exitosa" : "Fallida");

                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo verificar conexión con OpenRouter");
                return false;
            }
        }

        /// <summary>
        /// Genera una respuesta simple y rápida para casos básicos
        /// </summary>
        public async Task<string> GenerarRespuestaSimpleAsync(string prompt)
        {
            const string modeloRapido = "meta-llama/llama-3.2-3b-instruct:free";
            
            try
            {
                var request = new OpenRouterRequest
                {
                    Model = modeloRapido,
                    Messages = new List<Message>
                    {
                        new Message { Role = "user", Content = prompt }
                    },
                    MaxTokens = 500,
                    Temperature = 0.5f
                };

                var jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    return openRouterResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? 
                           "Lo siento, no pude generar una respuesta en este momento.";
                }

                return "Servicio temporalmente no disponible.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en respuesta simple");
                return "Error al procesar la solicitud.";
            }
        }
    }

    // =============================================
    // CLASES DE MODELO PARA OPENROUTER API
    // =============================================

    public class OpenRouterRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<Message> Messages { get; set; } = new();
        public int MaxTokens { get; set; } = 1000;
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 0.9f;
        public float FrequencyPenalty { get; set; } = 0.0f;
        public float PresencePenalty { get; set; } = 0.0f;
        public bool Stream { get; set; } = false;
    }

    public class Message
    {
        public string Role { get; set; } = string.Empty; // "system", "user", "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class OpenRouterResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public List<Choice> Choices { get; set; } = new();
        public Usage? Usage { get; set; }
    }

    public class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; } = new();
        public string FinishReason { get; set; } = string.Empty;
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class ModelosResponse
    {
        public List<ModelInfo> Data { get; set; } = new();
    }

    public class ModelInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? ContextLength { get; set; }
        public Pricing? Pricing { get; set; }
        public TopProvider? TopProvider { get; set; }
    }

    public class Pricing
    {
        public string? Prompt { get; set; }
        public string? Completion { get; set; }
    }

    public class TopProvider
    {
        public int? MaxCompletionTokens { get; set; }
    }

    public class ModeloDisponible
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool EsGratuito { get; set; }
        public int ContextWindow { get; set; }
        public int MaxTokens { get; set; }
    }
}