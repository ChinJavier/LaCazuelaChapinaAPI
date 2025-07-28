// =============================================
// ARCHIVO: Controllers/NotificacionesController.cs
// Gestión de Notificaciones - La Cazuela Chapina
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Notificaciones;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Notificaciones;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión de notificaciones push y alertas del sistema
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NotificacionesController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<NotificacionesController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // =============================================
        // CONSULTAS DE NOTIFICACIONES
        // =============================================

        /// <summary>
        /// Obtiene las notificaciones pendientes para una sucursal
        /// </summary>
        [HttpGet("sucursal/{sucursalId}/pendientes")]
        public async Task<ActionResult<List<NotificacionDto>>> GetNotificacionesPendientes(int sucursalId)
        {
            try
            {
                var notificaciones = await _context.Notificaciones
                    .Where(n => n.SucursalId == sucursalId && !n.Enviada)
                    .Include(n => n.Sucursal)
                    .OrderByDescending(n => n.FechaCreacion)
                    .Take(50) // Limitar para performance
                    .ToListAsync();

                var notificacionesDto = _mapper.Map<List<NotificacionDto>>(notificaciones);
                return Ok(notificacionesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificaciones pendientes para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el historial de notificaciones de una sucursal
        /// </summary>
        [HttpGet("sucursal/{sucursalId}/historial")]
        public async Task<ActionResult<List<NotificacionDto>>> GetHistorialNotificaciones(
            int sucursalId,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamaño = 20,
            [FromQuery] TipoNotificacion? tipo = null)
        {
            try
            {
                var query = _context.Notificaciones
                    .Where(n => n.SucursalId == sucursalId)
                    .Include(n => n.Sucursal)
                    .AsQueryable();

                if (tipo.HasValue)
                {
                    query = query.Where(n => n.TipoNotificacion == tipo.Value);
                }

                var notificaciones = await query
                    .OrderByDescending(n => n.FechaCreacion)
                    .Skip((pagina - 1) * tamaño)
                    .Take(tamaño)
                    .ToListAsync();

                var notificacionesDto = _mapper.Map<List<NotificacionDto>>(notificaciones);
                return Ok(notificacionesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de notificaciones para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de notificaciones del sistema
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<ActionResult<EstadisticasNotificacionesDto>> GetEstadisticas(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                fechaInicio ??= DateTime.UtcNow.AddDays(-7);
                fechaFin ??= DateTime.UtcNow;

                var notificaciones = await _context.Notificaciones
                    .Where(n => n.FechaCreacion >= fechaInicio && n.FechaCreacion <= fechaFin)
                    .Include(n => n.Sucursal)
                    .ToListAsync();

                var estadisticas = new EstadisticasNotificacionesDto
                {
                    FechaInicio = fechaInicio.Value,
                    FechaFin = fechaFin.Value,
                    TotalNotificaciones = notificaciones.Count,
                    NotificacionesEnviadas = notificaciones.Count(n => n.Enviada),
                    NotificacionesPendientes = notificaciones.Count(n => !n.Enviada),
                    PorcentajeExito = notificaciones.Count > 0 ? 
                        (notificaciones.Count(n => n.Enviada) * 100.0) / notificaciones.Count : 100,
                    NotificacionesPorTipo = notificaciones
                        .GroupBy(n => n.TipoNotificacion)
                        .Select(g => new NotificacionPorTipoDto
                        {
                            Tipo = g.Key,
                            Cantidad = g.Count(),
                            Enviadas = g.Count(n => n.Enviada),
                            Pendientes = g.Count(n => !n.Enviada)
                        })
                        .OrderByDescending(x => x.Cantidad)
                        .ToList(),
                    NotificacionesPorSucursal = notificaciones
                        .GroupBy(n => new { n.SucursalId, n.Sucursal.Nombre })
                        .Select(g => new NotificacionPorSucursalDto
                        {
                            SucursalId = g.Key.SucursalId,
                            SucursalNombre = g.Key.Nombre,
                            Cantidad = g.Count(),
                            Enviadas = g.Count(n => n.Enviada),
                            Pendientes = g.Count(n => !n.Enviada)
                        })
                        .OrderByDescending(x => x.Cantidad)
                        .ToList()
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de notificaciones");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // CREACIÓN DE NOTIFICACIONES
        // =============================================

        /// <summary>
        /// Crea una notificación de venta completada
        /// </summary>
        [HttpPost("venta")]
        public async Task<ActionResult<NotificacionDto>> CrearNotificacionVenta(CrearNotificacionVentaDto dto)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Sucursal)
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.Id == dto.VentaId);

                if (venta == null)
                {
                    return NotFound(new { message = $"Venta con ID {dto.VentaId} no encontrada" });
                }

                var notificacion = new Notificacion
                {
                    SucursalId = venta.SucursalId,
                    TipoNotificacion = TipoNotificacion.Venta,
                    Titulo = "Nueva Venta Registrada",
                    Mensaje = $"Venta #{venta.NumeroVenta} - Q{venta.Total:F2} - {venta.Detalles.Sum(d => d.Cantidad)} items",
                    FechaCreacion = DateTime.UtcNow,
                    ReferenciaId = venta.Id,
                    Enviada = false
                };

                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                // Enviar notificación push (implementación pendiente)
                await EnviarNotificacionPush(notificacion);

                var notificacionDto = _mapper.Map<NotificacionDto>(notificacion);
                _logger.LogInformation("Notificación de venta creada: {NotificacionId}", notificacion.Id);

                return CreatedAtAction(nameof(GetNotificacion), new { id = notificacion.Id }, notificacionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear notificación de venta");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crea una notificación de fin de cocción
        /// </summary>
        [HttpPost("fin-coccion")]
        public async Task<ActionResult<NotificacionDto>> CrearNotificacionFinCoccion(CrearNotificacionFinCoccionDto dto)
        {
            try
            {
                var sucursal = await _context.Sucursales.FindAsync(dto.SucursalId);
                if (sucursal == null)
                {
                    return NotFound(new { message = $"Sucursal con ID {dto.SucursalId} no encontrada" });
                }

                var notificacion = new Notificacion
                {
                    SucursalId = dto.SucursalId,
                    TipoNotificacion = TipoNotificacion.FinCoccion,
                    Titulo = "Lote de Cocción Completado",
                    Mensaje = $"Lote de {dto.TipoProducto} completado - {dto.Cantidad} unidades listas",
                    FechaCreacion = DateTime.UtcNow,
                    ReferenciaId = dto.LoteId,
                    Enviada = false
                };

                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                await EnviarNotificacionPush(notificacion);

                var notificacionDto = _mapper.Map<NotificacionDto>(notificacion);
                _logger.LogInformation("Notificación de fin de cocción creada: {NotificacionId}", notificacion.Id);

                return CreatedAtAction(nameof(GetNotificacion), new { id = notificacion.Id }, notificacionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear notificación de fin de cocción");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crea una notificación de stock bajo
        /// </summary>
        [HttpPost("stock-bajo")]
        public async Task<ActionResult<NotificacionDto>> CrearNotificacionStockBajo(CrearNotificacionStockBajoDto dto)
        {
            try
            {
                var stock = await _context.StockSucursal
                    .Include(s => s.Sucursal)
                    .Include(s => s.MateriaPrima)
                    .FirstOrDefaultAsync(s => s.Id == dto.StockId);

                if (stock == null)
                {
                    return NotFound(new { message = $"Stock con ID {dto.StockId} no encontrado" });
                }

                // Verificar si ya existe una notificación reciente para este stock
                var notificacionExistente = await _context.Notificaciones
                    .Where(n => n.SucursalId == stock.SucursalId && 
                               n.TipoNotificacion == TipoNotificacion.StockBajo &&
                               n.ReferenciaId == dto.StockId &&
                               n.FechaCreacion >= DateTime.UtcNow.AddHours(-6)) // No duplicar en 6 horas
                    .FirstOrDefaultAsync();

                if (notificacionExistente != null)
                {
                    return Ok(_mapper.Map<NotificacionDto>(notificacionExistente));
                }

                var nivelCriticidad = stock.CantidadActual <= 0 ? "AGOTADO" : "STOCK BAJO";
                var notificacion = new Notificacion
                {
                    SucursalId = stock.SucursalId,
                    TipoNotificacion = TipoNotificacion.StockBajo,
                    Titulo = $"{nivelCriticidad} - {stock.MateriaPrima.Nombre}",
                    Mensaje = $"Stock actual: {stock.CantidadActual} {stock.MateriaPrima.UnidadMedida} " +
                             $"(Mínimo: {stock.MateriaPrima.StockMinimo})",
                    FechaCreacion = DateTime.UtcNow,
                    ReferenciaId = dto.StockId,
                    Enviada = false
                };

                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                await EnviarNotificacionPush(notificacion);

                var notificacionDto = _mapper.Map<NotificacionDto>(notificacion);
                _logger.LogInformation("Notificación de stock bajo creada: {NotificacionId}", notificacion.Id);

                return CreatedAtAction(nameof(GetNotificacion), new { id = notificacion.Id }, notificacionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear notificación de stock bajo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crea una notificación personalizada del sistema
        /// </summary>
        [HttpPost("sistema")]
        public async Task<ActionResult<NotificacionDto>> CrearNotificacionSistema(CrearNotificacionSistemaDto dto)
        {
            try
            {
                var sucursal = await _context.Sucursales.FindAsync(dto.SucursalId);
                if (sucursal == null)
                {
                    return NotFound(new { message = $"Sucursal con ID {dto.SucursalId} no encontrada" });
                }

                var notificacion = new Notificacion
                {
                    SucursalId = dto.SucursalId,
                    TipoNotificacion = TipoNotificacion.Sistema,
                    Titulo = dto.Titulo,
                    Mensaje = dto.Mensaje,
                    FechaCreacion = DateTime.UtcNow,
                    ReferenciaId = dto.ReferenciaId,
                    Enviada = false
                };

                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                if (dto.EnviarInmediatamente)
                {
                    await EnviarNotificacionPush(notificacion);
                }

                var notificacionDto = _mapper.Map<NotificacionDto>(notificacion);
                _logger.LogInformation("Notificación de sistema creada: {NotificacionId}", notificacion.Id);

                return CreatedAtAction(nameof(GetNotificacion), new { id = notificacion.Id }, notificacionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear notificación de sistema");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // GESTIÓN DE NOTIFICACIONES
        // =============================================

        /// <summary>
        /// Obtiene una notificación específica por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificacionDto>> GetNotificacion(int id)
        {
            try
            {
                var notificacion = await _context.Notificaciones
                    .Include(n => n.Sucursal)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (notificacion == null)
                {
                    return NotFound(new { message = $"Notificación con ID {id} no encontrada" });
                }

                var notificacionDto = _mapper.Map<NotificacionDto>(notificacion);
                return Ok(notificacionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificación {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Marca una notificación como enviada
        /// </summary>
        [HttpPut("{id}/marcar-enviada")]
        public async Task<IActionResult> MarcarComoEnviada(int id)
        {
            try
            {
                var notificacion = await _context.Notificaciones.FindAsync(id);
                if (notificacion == null)
                {
                    return NotFound(new { message = $"Notificación con ID {id} no encontrada" });
                }

                notificacion.Enviada = true;
                notificacion.FechaEnvio = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Notificación {Id} marcada como enviada", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar notificación {Id} como enviada", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Marca múltiples notificaciones como enviadas
        /// </summary>
        [HttpPut("marcar-enviadas")]
        public async Task<IActionResult> MarcarMultiplesComoEnviadas(MarcarEnviadasDto dto)
        {
            try
            {
                var notificaciones = await _context.Notificaciones
                    .Where(n => dto.NotificacionIds.Contains(n.Id) && !n.Enviada)
                    .ToListAsync();

                foreach (var notificacion in notificaciones)
                {
                    notificacion.Enviada = true;
                    notificacion.FechaEnvio = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marcadas {Cantidad} notificaciones como enviadas", notificaciones.Count);
                return Ok(new { notificacionesActualizadas = notificaciones.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar múltiples notificaciones como enviadas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Procesa y envía todas las notificaciones pendientes
        /// </summary>
        [HttpPost("procesar-pendientes")]
        public async Task<ActionResult<ResultadoProcesamientoDto>> ProcesarNotificacionesPendientes()
        {
            try
            {
                var notificacionesPendientes = await _context.Notificaciones
                    .Where(n => !n.Enviada && n.FechaCreacion >= DateTime.UtcNow.AddDays(-7))
                    .Include(n => n.Sucursal)
                    .OrderBy(n => n.FechaCreacion)
                    .ToListAsync();

                int exitosas = 0;
                int fallidas = 0;

                foreach (var notificacion in notificacionesPendientes)
                {
                    try
                    {
                        await EnviarNotificacionPush(notificacion);
                        exitosas++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar notificación {Id}", notificacion.Id);
                        fallidas++;
                    }
                }

                var resultado = new ResultadoProcesamientoDto
                {
                    TotalProcesadas = notificacionesPendientes.Count,
                    Exitosas = exitosas,
                    Fallidas = fallidas,
                    PorcentajeExito = notificacionesPendientes.Count > 0 ? 
                        (exitosas * 100.0) / notificacionesPendientes.Count : 100
                };

                _logger.LogInformation("Procesamiento completado: {Exitosas}/{Total} notificaciones enviadas", 
                    exitosas, notificacionesPendientes.Count);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar notificaciones pendientes");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS
        // =============================================

        /// <summary>
        /// Simula el envío de notificación push (implementar con servicio real)
        /// </summary>
        private async Task EnviarNotificacionPush(Notificacion notificacion)
        {
            try
            {
                // TODO: Implementar con servicio real de push notifications
                // Por ejemplo: Firebase Cloud Messaging, OneSignal, etc.
                
                // Simulación de envío
                await Task.Delay(100); // Simular latencia de red
                
                // Marcar como enviada
                notificacion.Enviada = true;
                notificacion.FechaEnvio = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Notificación push enviada: {Id} - {Titulo}", 
                    notificacion.Id, notificacion.Titulo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación push {Id}", notificacion.Id);
                throw; // Re-lanzar para manejar en el método padre
            }
        }
    }
}