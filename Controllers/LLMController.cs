// =============================================
// ARCHIVO: Controllers/LLMController.cs
// Integración con LLM - La Cazuela Chapina
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Services;
using LaCazuelaChapina.API.DTOs.LLM;
using LaCazuelaChapina.API.Models.Enums;
using System.Text.Json;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para funcionalidades inteligentes con LLM/IA
    /// Integración creativa con OpenRouter para mejorar la experiencia del usuario
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LLMController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOpenRouterService _openRouterService;
        private readonly ILogger<LLMController> _logger;
        private readonly IConfiguration _configuration;

        public LLMController(
            CazuelaDbContext context,
            IMapper mapper,
            IOpenRouterService openRouterService,
            ILogger<LLMController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _openRouterService = openRouterService;
            _logger = logger;
            _configuration = configuration;
        }

        // =============================================
        // ASISTENTE INTELIGENTE PARA PEDIDOS
        // =============================================

        /// <summary>
        /// Asistente IA que ayuda a crear pedidos personalizados basado en preferencias del cliente
        /// </summary>
        [HttpPost("asistente-pedido")]
        public async Task<ActionResult<AsistentePedidoResponseDto>> AsistentePedido(AsistentePedidoRequestDto request)
        {
            try
            {
                // Obtener productos y combos disponibles
                var productos = await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Variantes.Where(v => v.Activa))
                    .Where(p => p.Activo)
                    .ToListAsync();

                var combos = await _context.Combos
                    .Include(c => c.Componentes)
                    .Where(c => c.Activo && 
                        (c.TipoCombo == TipoCombo.Fijo || 
                         (c.FechaInicioVigencia <= DateTime.UtcNow.Date && 
                          c.FechaFinVigencia >= DateTime.UtcNow.Date)))
                    .ToListAsync();

                // Construir contexto para el LLM
                var contextoMenu = ConstruirContextoMenu(productos, combos);
                
                var prompt = $@"
Eres un asistente experto en La Cazuela Chapina, especialista en tamales guatemaltecos y bebidas tradicionales.

MENÚ DISPONIBLE:
{contextoMenu}

SOLICITUD DEL CLIENTE: {request.SolicitudCliente}

PREFERENCIAS ADICIONALES:
- Presupuesto: {(request.PresupuestoMaximo.HasValue ? $"Q{request.PresupuestoMaximo:F2}" : "Sin límite")}
- Número de personas: {request.NumeroPersonas ?? 1}
- Ocasión especial: {request.OcasionEspecial ?? "Consumo regular"}
- Restricciones: {request.Restricciones ?? "Ninguna"}
- Nivel de picante preferido: {request.NivelPicantePreferido ?? "No especificado"}

INSTRUCCIONES:
1. Analiza la solicitud del cliente y sus preferencias
2. Recomienda productos específicos del menú disponible
3. Sugiere personalizaciones apropiadas (masa, relleno, envoltura, picante para tamales)
4. Para bebidas, recomienda tipo, endulzante y toppings
5. Si aplica, sugiere combos que se ajusten al presupuesto
6. Calcula el total aproximado
7. Explica brevemente por qué cada recomendación es ideal

FORMATO DE RESPUESTA (JSON):
{{
  ""recomendaciones"": [
    {{
      ""tipo"": ""producto"" | ""combo"",
      ""nombre"": ""nombre del producto/combo"",
      ""cantidad"": numero,
      ""personalizaciones"": [""lista de personalizaciones sugeridas""],
      ""precio_aproximado"": numero,
      ""razon"": ""explicación breve""
    }}
  ],
  ""total_aproximado"": numero,
  ""mensaje_personalizado"": ""mensaje amigable explicando la recomendación"",
  ""consejos_adicionales"": [""lista de consejos útiles""]
}}

Responde SOLO con el JSON válido, sin texto adicional.";

                var respuestaLLM = await _openRouterService.GenerarRespuestaAsync(prompt, "meta-llama/llama-3.2-3b-instruct:free");
                
                // Parsear respuesta JSON
                var recomendacion = JsonSerializer.Deserialize<AsistentePedidoResponseDto>(respuestaLLM, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (recomendacion == null)
                {
                    // Crear respuesta de fallback si falla el parsing
                    recomendacion = new AsistentePedidoResponseDto
                    {
                        MensajePersonalizado = "Lo siento, tuve dificultades procesando tu solicitud. Por favor, intenta con una solicitud más específica.",
                        ConsejosAdicionales = new List<string> { "Especifica el tipo de tamal que prefieres", "Menciona si tienes alguna restricción alimentaria" }
                    };
                }

                // Enriquecer con datos reales del sistema
                await EnriquecerRecomendacionConDatosReales(recomendacion);

                _logger.LogInformation("Asistente de pedido procesado para: {Solicitud}", request.SolicitudCliente);
                
                return Ok(recomendacion);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error al parsear respuesta del LLM");
                
                // Respuesta de fallback
                var fallbackResponse = new AsistentePedidoResponseDto
                {
                    MensajePersonalizado = "Disculpa, tuve problemas procesando tu solicitud. Te recomiendo nuestro Combo Familiar 'Fiesta Patronal' que incluye tamales variados y bebidas tradicionales.",
                    Recomendaciones = new List<RecomendacionProductoDto>
                    {
                        new RecomendacionProductoDto
                        {
                            Tipo = "combo",
                            Nombre = "Combo Familiar Fiesta Patronal",
                            Cantidad = 1,
                            PrecioAproximado = 145.00,
                            Razon = "Opción popular que satisface a toda la familia"
                        }
                    },
                    TotalAproximado = 145.00
                };
                
                return Ok(fallbackResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en asistente de pedido");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // ANÁLISIS INTELIGENTE DE VENTAS
        // =============================================

        /// <summary>
        /// Genera insights inteligentes sobre patrones de venta y tendencias usando IA
        /// </summary>
        [HttpPost("analisis-ventas")]
        public async Task<ActionResult<AnalisisVentasLLMDto>> AnalisisInteligenteVentas(AnalisisVentasRequestDto request)
        {
            try
            {
                var fechaInicio = request.FechaInicio ?? DateTime.UtcNow.AddDays(-30);
                var fechaFin = request.FechaFin ?? DateTime.UtcNow;

                // Obtener datos de ventas
                var ventasData = await ObtenerDatosVentasParaAnalisis(fechaInicio, fechaFin, request.SucursalId);

                var prompt = $@"
Eres un analista de datos experto especializado en negocios de comida guatemalteca tradicional.

DATOS DE VENTAS (últimos {(fechaFin - fechaInicio).Days} días):
{ventasData}

ANÁLISIS SOLICITADO:
- Tipo: {request.TipoAnalisis}
- Enfoque especial: {request.EnfoqueEspecial ?? "General"}

INSTRUCCIONES:
1. Analiza los patrones de venta identificando tendencias
2. Detecta productos estrella y de bajo rendimiento
3. Identifica oportunidades de mejora
4. Sugiere estrategias específicas para aumentar ventas
5. Recomienda ajustes de inventario o precios si es necesario
6. Identifica patrones estacionales o por horarios
7. Sugiere nuevos combos o promociones basados en los datos

FORMATO DE RESPUESTA (JSON):
{{
  ""resumen_ejecutivo"": ""resumen de 2-3 líneas"",
  ""tendencias_principales"": [""lista de tendencias detectadas""],
  ""productos_estrella"": [
    {{
      ""nombre"": ""nombre del producto"",
      ""rendimiento"": ""descripción del rendimiento"",
      ""recomendacion"": ""qué hacer con este producto""
    }}
  ],
  ""oportunidades_mejora"": [""lista de oportunidades específicas""],
  ""estrategias_sugeridas"": [
    {{
      ""estrategia"": ""nombre de la estrategia"",
      ""descripcion"": ""descripción detallada"",
      ""impacto_esperado"": ""alto"" | ""medio"" | ""bajo"",
      ""dificultad_implementacion"": ""baja"" | ""media"" | ""alta""
    }}
  ],
  ""predicciones"": [""predicciones para los próximos 30 días""],
  ""alertas"": [""alertas importantes que requieren atención""]
}}

Responde SOLO con el JSON válido.";

                var respuestaLLM = await _openRouterService.GenerarRespuestaAsync(prompt, "meta-llama/llama-3.2-3b-instruct:free");
                
                var analisis = JsonSerializer.Deserialize<AnalisisVentasLLMDto>(respuestaLLM, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (analisis == null)
                {
                    analisis = new AnalisisVentasLLMDto
                    {
                        ResumenEjecutivo = "Análisis basado en datos disponibles del período seleccionado",
                        TendenciasPrincipales = new List<string> { "Los datos muestran patrones regulares de consumo" },
                        ProductosEstrella = new List<ProductoEstrella>(),
                        OportunidadesMejora = new List<string> { "Continuar monitoreando tendencias de ventas" }
                    };
                }

                analisis.FechaAnalisis = DateTime.UtcNow;
                analisis.PeriodoAnalizado = $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}";

                _logger.LogInformation("Análisis inteligente de ventas generado para período {Inicio} - {Fin}", fechaInicio, fechaFin);
                
                return Ok(analisis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en análisis inteligente de ventas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // GENERADOR DE DESCRIPCIONES CREATIVAS
        // =============================================

        /// <summary>
        /// Genera descripciones creativas y atractivas para productos y combos
        /// </summary>
        [HttpPost("generar-descripcion")]
        public async Task<ActionResult<DescripcionCreativaDto>> GenerarDescripcionCreativa(GenerarDescripcionRequestDto request)
        {
            try
            {
                var tipoProducto = request.TipoProducto.ToLower();
                var contextoPrompt = "";

                if (tipoProducto == "tamal")
                {
                    contextoPrompt = "tamales guatemaltecos tradicionales, masa de maíz, envueltos en hoja de plátano o tusa";
                }
                else if (tipoProducto == "bebida")
                {
                    contextoPrompt = "bebidas tradicionales guatemaltecas de maíz y cacao, servidas calientes";
                }
                else if (tipoProducto == "combo")
                {
                    contextoPrompt = "combinaciones especiales de tamales y bebidas para ocasiones familiares";
                }

                var prompt = $@"
Eres un copywriter experto en gastronomía guatemalteca y marketing de alimentos tradicionales.

PRODUCTO: {request.NombreProducto}
TIPO: {request.TipoProducto}
INGREDIENTES/COMPONENTES: {string.Join(", ", request.Ingredientes ?? new List<string>())}
OCASIÓN: {request.OcasionEspecial ?? "Consumo general"}
TONO: {request.TonoDescripcion ?? "Tradicional y familiar"}

CONTEXTO: {contextoPrompt}

INSTRUCCIONES:
1. Crea una descripción atractiva que despierte el apetito
2. Incluye elementos de la tradición guatemalteca
3. Resalta los ingredientes principales
4. Menciona la experiencia sensorial (sabor, aroma, textura)
5. Conecta con emociones y recuerdos familiares
6. Incluye beneficios o momentos ideales de consumo

FORMATO DE RESPUESTA (JSON):
{{
  ""descripcion_principal"": ""descripción principal de 2-3 líneas"",
  ""descripcion_extendida"": ""descripción más detallada para marketing"",
  ""slogan_sugerido"": ""frase pegajosa de máximo 8 palabras"",
  ""hashtags_sugeridos"": [""lista de hashtags relevantes""],
  ""momentos_ideales"": [""cuándo es perfecto consumir este producto""],
  ""maridajes_sugeridos"": [""con qué otros productos combina bien""]
}}

Usa lenguaje cálido, familiar y que evoque la tradición guatemalteca. Responde SOLO con el JSON válido.";

                var respuestaLLM = await _openRouterService.GenerarRespuestaAsync(prompt, "meta-llama/llama-3.2-3b-instruct:free");
                
                var descripcion = JsonSerializer.Deserialize<DescripcionCreativaDto>(respuestaLLM, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (descripcion == null)
                {
                    descripcion = new DescripcionCreativaDto
                    {
                        DescripcionPrincipal = $"{request.NombreProducto} - Una deliciosa tradición guatemalteca que conecta con nuestras raíces.",
                        DescripcionExtendida = $"Nuestro {request.NombreProducto} está elaborado con ingredientes frescos y recetas tradicionales que han pasado de generación en generación.",
                        SloganSugerido = "Tradición que alimenta el alma",
                        HashtagsSugeridos = new List<string> { "#TradicionGuatemalteca", "#LaCazuelaChapina", "#ComidaTradicional" }
                    };
                }

                _logger.LogInformation("Descripción creativa generada para: {Producto}", request.NombreProducto);
                
                return Ok(descripcion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando descripción creativa");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // CHATBOT INTELIGENTE DE SOPORTE
        // =============================================

        /// <summary>
        /// Chatbot inteligente que responde preguntas sobre productos, pedidos y La Cazuela Chapina
        /// </summary>
        [HttpPost("chatbot")]
        public async Task<ActionResult<ChatbotResponseDto>> ChatbotSoporte(ChatbotRequestDto request)
        {
            try
            {
                // Obtener contexto relevante basado en la pregunta
                var contextoRelevante = await ObtenerContextoRelevante(request.Pregunta);

                var prompt = $@"
Eres el asistente virtual oficial de ""La Cazuela Chapina"", especialista en tamales guatemaltecos y bebidas tradicionales.

PERSONALIDAD:
- Amigable, conocedor y orgulloso de la tradición guatemalteca
- Entusiasta por los tamales y la cultura gastronómica local
- Servicial y paciente con los clientes
- Usa expresiones guatemaltecas cuando sea apropiado (de forma moderada)

CONOCIMIENTO BASE:
{contextoRelevante}

PREGUNTA DEL CLIENTE: {request.Pregunta}
HISTORIAL PREVIO: {string.Join("\n", request.HistorialConversacion ?? new List<string>())}

INSTRUCCIONES:
1. Responde de manera amigable y profesional
2. Si es sobre productos, sé específico con precios y opciones
3. Si es sobre pedidos, guía paso a paso
4. Si es sobre la empresa, comparte la pasión por la tradición
5. Si no sabes algo, sé honesto y ofrece conectar con un humano
6. Sugiere productos relevantes cuando sea apropiado
7. Incluye emojis de forma moderada para calidez

FORMATO DE RESPUESTA (JSON):
{{
  ""respuesta"": ""respuesta principal al cliente"",
  ""productos_sugeridos"": [""lista de productos relevantes si aplica""],
  ""acciones_sugeridas"": [""qué puede hacer el cliente a continuación""],
  ""necesita_humano"": boolean,
  ""categoria_consulta"": ""productos"" | ""pedidos"" | ""informacion"" | ""soporte"" | ""otro"",
  ""confianza_respuesta"": ""alta"" | ""media"" | ""baja""
}}

Responde SOLO con el JSON válido.";

                var respuestaLLM = await _openRouterService.GenerarRespuestaAsync(prompt, "meta-llama/llama-3.2-3b-instruct:free");
                
                var respuestaChatbot = JsonSerializer.Deserialize<ChatbotResponseDto>(respuestaLLM, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (respuestaChatbot == null)
                {
                    respuestaChatbot = new ChatbotResponseDto
                    {
                        Respuesta = "¡Hola! Soy el asistente de La Cazuela Chapina 😊 Estoy aquí para ayudarte con información sobre nuestros deliciosos tamales y bebidas tradicionales. ¿En qué puedo asistirte?",
                        CategoriaConsulta = "soporte",
                        ConfianzaRespuesta = "alta",
                        SesionId = request.SesionId
                    };
                }
                else
                {
                    respuestaChatbot.SesionId = request.SesionId;
                }

                // Guardar la interacción para aprendizaje futuro
                await GuardarInteraccionChatbot(request.Pregunta, respuestaChatbot.Respuesta, request.SesionId);

                _logger.LogInformation("Consulta de chatbot procesada: {Categoria}", respuestaChatbot.CategoriaConsulta);
                
                return Ok(respuestaChatbot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en chatbot de soporte");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // OPTIMIZADOR DE INVENTARIO CON IA
        // =============================================

        /// <summary>
        /// Analiza patrones de consumo y sugiere optimizaciones de inventario usando IA
        /// </summary>
        [HttpPost("optimizar-inventario")]
        public async Task<ActionResult<OptimizacionInventarioDto>> OptimizarInventario(OptimizarInventarioRequestDto request)
        {
            try
            {
                // Obtener datos de inventario y movimientos
                var datosInventario = await ObtenerDatosInventarioParaAnalisis(request.SucursalId);

                var prompt = $@"
Eres un experto en gestión de inventarios para restaurantes especializados en comida guatemalteca tradicional.

DATOS ACTUALES DE INVENTARIO:
{datosInventario}

PARÁMETROS DE ANÁLISIS:
- Días a proyectar: {request.DiasProyeccion ?? 30}
- Nivel de servicio deseado: {request.NivelServicioDeseado ?? 95}%
- Considerar estacionalidad: {request.ConsiderarEstacionalidad}

INSTRUCCIONES:
1. Analiza patrones de consumo de cada materia prima
2. Identifica productos con alta rotación vs baja rotación
3. Detecta riesgos de desabastecimiento o sobrestock
4. Sugiere puntos de reorden óptimos
5. Recomienda cantidades de compra
6. Identifica oportunidades de reducir desperdicios
7. Considera la estacionalidad de productos guatemaltecos

FORMATO DE RESPUESTA (JSON):
{{
  ""resumen_general"": ""análisis general del estado del inventario"",
  ""productos_criticos"": [
    {{
      ""nombre"": ""nombre del producto"",
      ""stock_actual"": numero,
      ""proyeccion_agotamiento"": ""fecha estimada"",
      ""cantidad_sugerida_compra"": numero,
      ""prioridad"": ""alta"" | ""media"" | ""baja""
    }}
  ],
  ""oportunidades_ahorro"": [""formas de reducir costos de inventario""],
  ""alertas_estacionales"": [""productos que necesitan atención por temporada""],
  ""recomendaciones_compra"": [
    {{
      ""producto"": ""nombre"",
      ""cantidad"": numero,
      ""justificacion"": ""por qué esta cantidad""
    }}
  ],
  ""ahorro_estimado"": numero,
  ""riesgo_desabasto"": ""bajo"" | ""medio"" | ""alto""
}}

Responde SOLO con el JSON válido.";

                var respuestaLLM = await _openRouterService.GenerarRespuestaAsync(prompt, "meta-llama/llama-3.2-3b-instruct:free");
                
                var optimizacion = JsonSerializer.Deserialize<OptimizacionInventarioDto>(respuestaLLM, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (optimizacion == null)
                {
                    optimizacion = new OptimizacionInventarioDto
                    {
                        ResumenGeneral = "Análisis de inventario basado en datos actuales disponibles",
                        ProductosCriticos = new List<ProductoCriticoInventario>(),
                        OportunidadesAhorro = new List<string> { "Continuar monitoreando niveles de stock" },
                        RiesgoDesabasto = "medio"
                    };
                }

                optimizacion.FechaAnalisis = DateTime.UtcNow;
                optimizacion.SucursalAnalizada = request.SucursalId;

                _logger.LogInformation("Optimización de inventario generada para sucursal {SucursalId}", request.SucursalId);
                
                return Ok(optimizacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en optimización de inventario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // PROCESAMIENTO DE VOZ A TEXTO (BONUS)
        // =============================================

        /// <summary>
        /// Procesa audio de clientes para convertir pedidos de voz a texto y procesarlos
        /// </summary>
        [HttpPost("procesar-voz")]
        public async Task<ActionResult<ProcesarVozResponseDto>> ProcesarVozAPedido([FromForm] ProcesarVozRequestDto request)
        {
            try
            {
                if (request.AudioFile == null || request.AudioFile.Length == 0)
                {
                    return BadRequest(new { message = "No se proporcionó archivo de audio" });
                }

                // Simular procesamiento de voz a texto (en implementación real usaríamos Whisper API)
                var textoSimulado = SimularProcesamientoVoz(request.AudioFile.FileName);

                // Procesar el texto como un pedido usando el asistente
                var asistentePedido = new AsistentePedidoRequestDto
                {
                    SolicitudCliente = textoSimulado,
                    NumeroPersonas = 1,
                    OcasionEspecial = "Pedido por voz"
                };

                var resultadoAsistente = await AsistentePedido(asistentePedido);
                
                AsistentePedidoResponseDto? recomendacion = null;
                if (resultadoAsistente.Result is OkObjectResult okResult)
                {
                    recomendacion = okResult.Value as AsistentePedidoResponseDto;
                }

                var respuesta = new ProcesarVozResponseDto
                {
                    TextoDetectado = textoSimulado,
                    ConfianzaDeteccion = 0.85f,
                    PedidoInterpretado = recomendacion,
                    RequiereConfirmacion = true,
                    IdimaDetectado = "es-GT"
                };

                _logger.LogInformation("Procesamiento de voz completado para archivo: {FileName}", request.AudioFile.FileName);
                
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando voz a texto");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS DE UTILIDAD
        // =============================================

        private string ConstruirContextoMenu(List<Models.Productos.Producto> productos, List<Models.Combos.Combo> combos)
        {
            var contexto = "PRODUCTOS DISPONIBLES:\n";
            
            foreach (var producto in productos)
            {
                contexto += $"- {producto.Nombre} (Categoría: {producto.Categoria?.Nombre})\n";
                contexto += $"  Precio base: Q{producto.PrecioBase:F2}\n";
                
                if (producto.Variantes?.Any() == true)
                {
                    contexto += "  Variantes: " + string.Join(", ", producto.Variantes.Select(v => $"{v.Nombre} (x{v.Multiplicador})")) + "\n";
                }
                contexto += "\n";
            }

            contexto += "\nCOMBOS DISPONIBLES:\n";
            foreach (var combo in combos)
            {
                contexto += $"- {combo.Nombre}: Q{combo.Precio:F2}\n";
                contexto += $"  {combo.Descripcion}\n\n";
            }

            return contexto;
        }

        private async Task EnriquecerRecomendacionConDatosReales(AsistentePedidoResponseDto recomendacion)
        {
            if (recomendacion?.Recomendaciones == null) return;

            // Validar que los productos recomendados existen y ajustar precios reales
            foreach (var rec in recomendacion.Recomendaciones)
            {
                if (rec.Tipo == "producto")
                {
                    var producto = await _context.Productos
                        .Include(p => p.Variantes)
                        .FirstOrDefaultAsync(p => p.Nombre.Contains(rec.Nombre) && p.Activo);
                    
                    if (producto != null)
                    {
                        rec.PrecioAproximado = (double)producto.PrecioBase;
                        rec.ProductoId = producto.Id;
                    }
                }
                else if (rec.Tipo == "combo")
                {
                    var combo = await _context.Combos
                        .FirstOrDefaultAsync(c => c.Nombre.Contains(rec.Nombre) && c.Activo);
                    
                    if (combo != null)
                    {
                        rec.PrecioAproximado = (double)combo.Precio;
                        rec.ComboId = combo.Id;
                    }
                }
            }

            recomendacion.TotalAproximado = recomendacion.Recomendaciones.Sum(r => r.PrecioAproximado * r.Cantidad);
        }

        private async Task<string> ObtenerDatosVentasParaAnalisis(DateTime fechaInicio, DateTime fechaFin, int? sucursalId)
        {
            var query = _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Combo)
                .Where(v => v.FechaVenta >= fechaInicio && v.FechaVenta <= fechaFin);

            if (sucursalId.HasValue)
            {
                query = query.Where(v => v.SucursalId == sucursalId.Value);
            }

            var ventas = await query.ToListAsync();

            var resumen = $"PERÍODO: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}\n";
            resumen += $"TOTAL VENTAS: {ventas.Count}\n";
            resumen += $"INGRESOS TOTALES: Q{ventas.Sum(v => v.Total):F2}\n\n";

            // Productos más vendidos
            var productosVendidos = ventas
                .SelectMany(v => v.Detalles)
                .Where(d => d.Producto != null)
                .GroupBy(d => d.Producto!.Nombre)
                .Select(g => new { Producto = g.Key, Cantidad = g.Sum(d => d.Cantidad), Ingresos = g.Sum(d => d.Subtotal) })
                .OrderByDescending(p => p.Cantidad)
                .Take(10);

            resumen += "TOP 10 PRODUCTOS:\n";
            foreach (var producto in productosVendidos)
            {
                resumen += $"- {producto.Producto}: {producto.Cantidad} unidades, Q{producto.Ingresos:F2}\n";
            }

            return resumen;
        }

        private async Task<string> ObtenerContextoRelevante(string pregunta)
        {
            var contextoBase = @"
LA CAZUELA CHAPINA - INFORMACIÓN GENERAL:
- Especialistas en tamales guatemaltecos tradicionales y bebidas de maíz/cacao
- Productos principales: Tamales (varios rellenos) y bebidas artesanales
- Personalizaciones: masa (maíz amarillo/blanco/arroz), relleno, envoltura, picante
- Horarios: Lunes a Domingo 7:00 AM - 8:00 PM
- Entregas a domicilio disponibles
- Aceptamos efectivo, tarjeta y transferencias

PRODUCTOS ESTRELLA:
- Tamal Tradicional (recado rojo de cerdo)
- Combo Familiar 'Fiesta Patronal'
- Bebidas de cacao y atol de elote
- Combos estacionales (fiambre, navideños, cuaresma)

PRECIOS APROXIMADOS:
- Tamales: Q8-15 según variante
- Bebidas: Q12-25 según tamaño
- Combos: Q145-385
";

            // Si la pregunta menciona productos específicos, agregar más detalle
            if (pregunta.ToLower().Contains("tamal"))
            {
                var productos = await _context.Productos
                    .Where(p => p.Categoria.Nombre.Contains("Tamal") && p.Activo)
                    .Include(p => p.Variantes)
                    .ToListAsync();

                contextoBase += "\nDETALLE TAMALES:\n";
                foreach (var producto in productos)
                {
                    contextoBase += $"- {producto.Nombre}: Q{producto.PrecioBase:F2}\n";
                }
            }

            return contextoBase;
        }

        private async Task GuardarInteraccionChatbot(string pregunta, string respuesta, string sesionId)
        {
            // En una implementación completa, guardaríamos esto en una tabla de interacciones
            // para análisis posterior y mejora del chatbot
            _logger.LogInformation("Interacción chatbot - Sesión: {SesionId}, Pregunta: {Pregunta}", sesionId, pregunta);
            
            // TODO: Implementar guardado en base de datos para análisis y mejora continua
            await Task.CompletedTask;
        }

        private async Task<string> ObtenerDatosInventarioParaAnalisis(int? sucursalId)
        {
            var query = _context.StockSucursal
                .Include(s => s.MateriaPrima)
                    .ThenInclude(mp => mp.Categoria)
                .Include(s => s.Sucursal)
                .AsQueryable();

            if (sucursalId.HasValue)
            {
                query = query.Where(s => s.SucursalId == sucursalId.Value);
            }

            var stocks = await query.ToListAsync();

            var resumen = "ESTADO ACTUAL DEL INVENTARIO:\n\n";
            
            foreach (var stock in stocks.Take(20)) // Limitar para no sobrecargar el prompt
            {
                var porcentajeStock = stock.MateriaPrima.StockMinimo > 0 ? 
                    (stock.CantidadActual / stock.MateriaPrima.StockMinimo) * 100 : 100;

                resumen += $"- {stock.MateriaPrima.Nombre} ({stock.MateriaPrima.Categoria.Nombre})\n";
                resumen += $"  Stock actual: {stock.CantidadActual} {stock.MateriaPrima.UnidadMedida}\n";
                resumen += $"  Stock mínimo: {stock.MateriaPrima.StockMinimo} {stock.MateriaPrima.UnidadMedida}\n";
                resumen += $"  Nivel: {porcentajeStock:F0}% del mínimo\n";
                resumen += $"  Costo promedio: Q{stock.MateriaPrima.CostoPromedio:F2}\n\n";
            }

            return resumen;
        }

        private string SimularProcesamientoVoz(string nombreArchivo)
        {
            // Simulación de diferentes tipos de pedidos por voz
            var pedidosSimulados = new[]
            {
                "Hola, quisiera pedir dos tamales de recado rojo y una bebida de cacao para llevar",
                "Buenos días, necesito el combo familiar para cuatro personas",
                "Quiero tres tamales, uno sin chile y dos con picante suave, más dos atoles de elote",
                "Me puede dar información sobre sus combos estacionales",
                "Quisiera hacer un pedido grande para una celebración familiar",
                "Buenos días, me gustaría ordenar tamales de chipilín con masa de arroz",
                "Necesito bebidas tradicionales para una reunión, que me recomienda",
                "Quiero probar algo nuevo, que combo me sugieren para dos personas"
            };

            var random = new Random();
            var pedidoSeleccionado = pedidosSimulados[random.Next(pedidosSimulados.Length)];
            
            _logger.LogInformation("Simulando procesamiento de voz para archivo: {Archivo}, texto detectado: {Texto}", 
                nombreArchivo, pedidoSeleccionado);
                
            return pedidoSeleccionado;
        }

        // =============================================
        // ENDPOINTS ADICIONALES DE UTILIDAD
        // =============================================

        /// <summary>
        /// Obtiene información sobre los modelos de IA disponibles
        /// </summary>
        [HttpGet("modelos-disponibles")]
        public async Task<ActionResult<List<ModeloDisponible>>> GetModelosDisponibles()
        {
            try
            {
                var modelos = await _openRouterService.ObtenerModelosDisponiblesAsync();
                return Ok(modelos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo modelos disponibles");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Verifica el estado de la conexión con OpenRouter
        /// </summary>
        [HttpGet("verificar-conexion")]
        public async Task<ActionResult<object>> VerificarConexion()
        {
            try
            {
                var estaConectado = await _openRouterService.VerificarConexionAsync();
                
                return Ok(new
                {
                    conectado = estaConectado,
                    timestamp = DateTime.UtcNow,
                    servicio = "OpenRouter",
                    estado = estaConectado ? "Operativo" : "No disponible"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando conexión");
                return Ok(new
                {
                    conectado = false,
                    timestamp = DateTime.UtcNow,
                    servicio = "OpenRouter",
                    estado = "Error de conexión",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Endpoint de prueba rápida para verificar funcionalidad básica del LLM
        /// </summary>
        [HttpPost("prueba-rapida")]
        public async Task<ActionResult<object>> PruebaRapida([FromBody] string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    prompt = "Saluda como asistente de La Cazuela Chapina y menciona brevemente nuestros productos principales.";
                }

                _logger.LogInformation("Iniciando prueba rápida con prompt: {Prompt}", prompt);

                // Usar el método principal en lugar del simple para mejor debugging
                var respuesta = await _openRouterService.GenerarRespuestaAsync(prompt, "meta-llama/llama-3.2-3b-instruct:free");
                
                _logger.LogInformation("Respuesta recibida exitosamente: {Respuesta}", respuesta);
                
                return Ok(new
                {
                    prompt_enviado = prompt,
                    respuesta_llm = respuesta,
                    timestamp = DateTime.UtcNow,
                    modelo_usado = "meta-llama/llama-3.2-3b-instruct:free",
                    estado = "exitoso"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP en prueba rápida");
                return StatusCode(500, new 
                { 
                    message = "Error de conectividad con OpenRouter",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Verifica tu API Key y conexión a internet"
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout en prueba rápida");
                return StatusCode(408, new 
                { 
                    message = "Timeout conectando con OpenRouter",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Intenta nuevamente, el servicio puede estar ocupado"
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parseando respuesta JSON");
                return StatusCode(500, new 
                { 
                    message = "Error procesando respuesta de IA",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "El modelo puede haber devuelto formato inesperado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en prueba rápida");
                return StatusCode(500, new 
                { 
                    message = "Error inesperado en prueba rápida",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow,
                    api_key_configurada = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")) || 
                                         !string.IsNullOrEmpty(_configuration["OpenRouter:ApiKey"])
                });
            }
        }
    }
}