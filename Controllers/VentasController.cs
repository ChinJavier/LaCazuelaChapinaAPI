// =============================================
// ARCHIVO: Controllers/VentasController.cs
// Controlador para sistema de ventas
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Ventas;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Ventas;
using LaCazuelaChapina.API.Services;
using LaCazuelaChapina.API.Mappings;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para el sistema de ventas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class VentasController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VentasController> _logger;

        public VentasController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<VentasController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Registra una nueva venta
        /// </summary>
        /// <param name="crearVentaDto">Datos de la venta</param>
        /// <returns>Venta registrada</returns>
        [HttpPost]
        [ProducesResponseType(typeof(VentaDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<VentaDto>> RegistrarVenta(CrearVentaDto crearVentaDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Validar sucursal
                var sucursal = await _context.Sucursales
                    .FirstOrDefaultAsync(s => s.Id == crearVentaDto.SucursalId && s.Activa);

                if (sucursal == null)
                    return BadRequest(new { message = "Sucursal no válida" });

                // Generar número de venta
                var fechaHoy = DateTime.UtcNow.Date;
                var ventasHoy = await _context.Ventas
                    .Where(v => v.SucursalId == crearVentaDto.SucursalId && v.FechaVenta.Date == fechaHoy)
                    .CountAsync();

                var numeroVenta = $"{sucursal.Id:D2}{DateTime.UtcNow:yyyyMMdd}{(ventasHoy + 1):D4}";

                // Crear venta
                var venta = new Venta
                {
                    SucursalId = crearVentaDto.SucursalId,
                    NumeroVenta = numeroVenta,
                    FechaVenta = DateTime.UtcNow,
                    TipoPago = crearVentaDto.TipoPago,
                    EstadoVenta = EstadoVenta.Completada,
                    ClienteNombre = crearVentaDto.ClienteNombre,
                    ClienteTelefono = crearVentaDto.ClienteTelefono,
                    EsVentaOffline = false
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                decimal subtotalVenta = 0;

                // Procesar detalles de venta
                foreach (var detalleDto in crearVentaDto.Detalles)
                {
                    decimal precioUnitario = 0;
                    decimal subtotalDetalle = 0;

                    // Calcular precio según sea producto individual o combo
                    if (detalleDto.ComboId.HasValue)
                    {
                        var combo = await _context.Combos
                            .FirstOrDefaultAsync(c => c.Id == detalleDto.ComboId && c.Activo);

                        if (combo == null)
                            return BadRequest(new { message = $"Combo {detalleDto.ComboId} no encontrado" });

                        precioUnitario = combo.Precio;
                    }
                    else if (detalleDto.ProductoId.HasValue && detalleDto.VarianteProductoId.HasValue)
                    {
                        var producto = await _context.Productos
                            .FirstOrDefaultAsync(p => p.Id == detalleDto.ProductoId && p.Activo);

                        var variante = await _context.VariantesProducto
                            .FirstOrDefaultAsync(v => v.Id == detalleDto.VarianteProductoId && v.Activa);

                        if (producto == null || variante == null)
                            return BadRequest(new { message = "Producto o variante no encontrados" });

                        precioUnitario = producto.PrecioBase * variante.Multiplicador;

                        // Calcular precios adicionales por personalización
                        if (detalleDto.Personalizaciones?.Any() == true)
                        {
                            var opcionesIds = detalleDto.Personalizaciones.Select(p => p.OpcionAtributoId).ToList();
                            var opciones = await _context.OpcionesAtributo
                                .Where(oa => opcionesIds.Contains(oa.Id) && oa.Activa)
                                .ToListAsync();

                            precioUnitario += opciones.Sum(o => o.PrecioAdicional);
                        }
                    }
                    else
                    {
                        return BadRequest(new { message = "Detalle de venta inválido" });
                    }

                    subtotalDetalle = precioUnitario * detalleDto.Cantidad;
                    subtotalVenta += subtotalDetalle;

                    // Crear detalle de venta
                    var detalleVenta = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoId = detalleDto.ProductoId,
                        VarianteProductoId = detalleDto.VarianteProductoId,
                        ComboId = detalleDto.ComboId,
                        Cantidad = detalleDto.Cantidad,
                        PrecioUnitario = precioUnitario,
                        Subtotal = subtotalDetalle,
                        Notas = detalleDto.Notas
                    };

                    _context.DetalleVentas.Add(detalleVenta);
                    await _context.SaveChangesAsync();

                    // Crear personalizaciones
                    if (detalleDto.Personalizaciones?.Any() == true)
                    {
                        foreach (var personalizacionDto in detalleDto.Personalizaciones)
                        {
                            var opcion = await _context.OpcionesAtributo
                                .Include(oa => oa.TipoAtributo)
                                .FirstOrDefaultAsync(oa => oa.Id == personalizacionDto.OpcionAtributoId);

                            if (opcion != null)
                            {
                                var personalizacion = new PersonalizacionVenta
                                {
                                    DetalleVentaId = detalleVenta.Id,
                                    TipoAtributoId = personalizacionDto.TipoAtributoId,
                                    OpcionAtributoId = personalizacionDto.OpcionAtributoId,
                                    PrecioAdicional = opcion.PrecioAdicional
                                };

                                _context.PersonalizacionesVenta.Add(personalizacion);
                            }
                        }
                    }
                }

                // Actualizar totales de venta
                venta.Subtotal = subtotalVenta;
                venta.Descuento = 0; // Por ahora sin descuentos
                venta.Total = subtotalVenta;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // TODO: Actualizar inventario automáticamente
                // TODO: Enviar notificación push

                // Obtener venta completa para respuesta
                var ventaCompleta = await _context.Ventas
                    .Include(v => v.Sucursal)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Producto)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.VarianteProducto)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Combo)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Personalizaciones)
                            .ThenInclude(pv => pv.TipoAtributo)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Personalizaciones)
                            .ThenInclude(pv => pv.OpcionAtributo)
                    .FirstAsync(v => v.Id == venta.Id);

                var ventaDto = _mapper.Map<VentaDto>(ventaCompleta);

                _logger.LogInformation("Venta {NumeroVenta} registrada por Q{Total}",
                    venta.NumeroVenta, venta.Total);

                return CreatedAtAction(nameof(GetVenta), new { id = venta.Id }, ventaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando venta");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene una venta por ID
        /// </summary>
        /// <param name="id">ID de la venta</param>
        /// <returns>Venta con detalles completos</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VentaDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<VentaDto>> GetVenta(int id)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Sucursal)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Producto)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.VarianteProducto)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Combo)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Personalizaciones)
                            .ThenInclude(pv => pv.TipoAtributo)
                    .Include(v => v.Detalles)
                        .ThenInclude(dv => dv.Personalizaciones)
                            .ThenInclude(pv => pv.OpcionAtributo)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (venta == null)
                {
                    _logger.LogWarning("Venta con ID {Id} no encontrada", id);
                    return NotFound(new { message = $"Venta con ID {id} no encontrada" });
                }

                var ventaDto = _mapper.Map<VentaDto>(venta);
                return Ok(ventaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo venta {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene ventas por sucursal con filtros opcionales
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <param name="fechaDesde">Fecha desde (opcional)</param>
        /// <param name="fechaHasta">Fecha hasta (opcional)</param>
        /// <param name="pagina">Número de página</param>
        /// <param name="tamanoPagina">Tamaño de página</param>
        /// <returns>Lista paginada de ventas</returns>
        [HttpGet("sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(VentasPaginadasDto), 200)]
        public async Task<ActionResult<VentasPaginadasDto>> GetVentasPorSucursal(
            int sucursalId,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 20)
        {
            try
            {
                var query = _context.Ventas
                    .Include(v => v.Sucursal)
                    .Where(v => v.SucursalId == sucursalId);

                if (fechaDesde.HasValue)
                    query = query.Where(v => v.FechaVenta.Date >= fechaDesde.Value.Date);

                if (fechaHasta.HasValue)
                    query = query.Where(v => v.FechaVenta.Date <= fechaHasta.Value.Date);

                var totalVentas = await query.CountAsync();

                var ventas = await query
                    .OrderByDescending(v => v.FechaVenta)
                    .Skip((pagina - 1) * tamanoPagina)
                    .Take(tamanoPagina)
                    .ToListAsync();

                var ventasDto = _mapper.Map<List<VentaResumenDto>>(ventas);

                var resultado = new VentasPaginadasDto
                {
                    Ventas = ventasDto,
                    TotalVentas = totalVentas,
                    PaginaActual = pagina,
                    TamanoPagina = tamanoPagina,
                    TotalPaginas = (int)Math.Ceiling((double)totalVentas / tamanoPagina)
                };

                _logger.LogInformation("Se obtuvieron {Count} ventas para sucursal {SucursalId}",
                    ventas.Count, sucursalId);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ventas por sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene ventas del día actual por sucursal
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>Ventas del día</returns>
        [HttpGet("sucursal/{sucursalId}/hoy")]
        [ProducesResponseType(typeof(List<VentaResumenDto>), 200)]
        public async Task<ActionResult<List<VentaResumenDto>>> GetVentasHoy(int sucursalId)
        {
            try
            {
                var fechaHoy = DateTime.UtcNow.Date;

                var ventas = await _context.Ventas
                    .Include(v => v.Sucursal)
                    .Where(v => v.SucursalId == sucursalId && v.FechaVenta.Date == fechaHoy)
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                var ventasDto = _mapper.Map<List<VentaResumenDto>>(ventas);

                _logger.LogInformation("Se obtuvieron {Count} ventas del día para sucursal {SucursalId}",
                    ventas.Count, sucursalId);

                return Ok(ventasDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ventas del día para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Cancela una venta (si está permitido)
        /// </summary>
        /// <param name="id">ID de la venta</param>
        /// <param name="motivo">Motivo de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        [HttpPatch("{id}/cancelar")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> CancelarVenta(int id, [FromBody] CancelarVentaDto cancelarDto)
        {
            try
            {
                var venta = await _context.Ventas.FindAsync(id);

                if (venta == null)
                    return NotFound(new { message = "Venta no encontrada" });

                if (venta.EstadoVenta == EstadoVenta.Cancelada)
                    return BadRequest(new { message = "La venta ya está cancelada" });

                // Verificar que la venta sea del día actual (regla de negocio)
                if (venta.FechaVenta.Date != DateTime.UtcNow.Date)
                    return BadRequest(new { message = "Solo se pueden cancelar ventas del día actual" });

                venta.EstadoVenta = EstadoVenta.Cancelada;

                // TODO: Revertir movimientos de inventario
                // TODO: Registrar motivo de cancelación en tabla de auditoría

                await _context.SaveChangesAsync();

                _logger.LogInformation("Venta {NumeroVenta} cancelada. Motivo: {Motivo}",
                    venta.NumeroVenta, cancelarDto.Motivo);

                return Ok(new { message = "Venta cancelada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando venta {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }

    // =============================================
    // DTOs ESPECÍFICOS PARA VENTAS
    // =============================================

    public class VentasPaginadasDto
    {
        public List<VentaResumenDto> Ventas { get; set; } = new();
        public int TotalVentas { get; set; }
        public int PaginaActual { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas { get; set; }
    }

    public class VentaResumenDto
    {
        public int Id { get; set; }
        public string NumeroVenta { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public TipoPago TipoPago { get; set; }
        public EstadoVenta EstadoVenta { get; set; }
        public string? ClienteNombre { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public int CantidadItems { get; set; }
    }

    public class CancelarVentaDto
    {
        public string Motivo { get; set; } = string.Empty;
    }
}