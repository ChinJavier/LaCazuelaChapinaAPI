using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Ventas;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Ventas;

namespace LaCazuelaChapina.API.Controllers
{
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

                // Generar número de venta único
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

                // Procesar cada detalle de venta
                foreach (var detalleDto in crearVentaDto.Detalles)
                {
                    decimal precioUnitario = 0;

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

                        // Sumar precios de personalizaciones
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
                        return BadRequest(new { message = "Detalle de venta inválido: debe especificar producto+variante O combo" });
                    }

                    decimal subtotalDetalle = precioUnitario * detalleDto.Cantidad;
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

                    // Crear personalizaciones si existen
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
                venta.Descuento = 0; // Sin descuentos por ahora
                venta.Total = subtotalVenta;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Obtener venta completa para respuesta
                var ventaCompleta = await GetVentaCompleta(venta.Id);
                var ventaDto = _mapper.Map<VentaDto>(ventaCompleta);

                _logger.LogInformation("💰 Venta {NumeroVenta} registrada por Q{Total}", 
                    venta.NumeroVenta, venta.Total);

                return CreatedAtAction(nameof(GetVenta), new { id = venta.Id }, ventaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error registrando venta");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene una venta por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VentaDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<VentaDto>> GetVenta(int id)
        {
            try
            {
                var venta = await GetVentaCompleta(id);

                if (venta == null)
                {
                    _logger.LogWarning("⚠️ Venta con ID {Id} no encontrada", id);
                    return NotFound(new { message = $"Venta con ID {id} no encontrada" });
                }

                var ventaDto = _mapper.Map<VentaDto>(venta);
                return Ok(ventaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo venta {Id}", id);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene ventas por sucursal con filtros opcionales
        /// </summary>
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
                    .Include(v => v.Detalles)
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

                _logger.LogInformation("✅ Se obtuvieron {Count} ventas para sucursal {SucursalId}", 
                    ventas.Count, sucursalId);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo ventas por sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene ventas del día actual por sucursal
        /// </summary>
        [HttpGet("sucursal/{sucursalId}/hoy")]
        [ProducesResponseType(typeof(List<VentaResumenDto>), 200)]
        public async Task<ActionResult<List<VentaResumenDto>>> GetVentasHoy(int sucursalId)
        {
            try
            {
                var fechaHoy = DateTime.UtcNow.Date;

                var ventas = await _context.Ventas
                    .Include(v => v.Sucursal)
                    .Include(v => v.Detalles)
                    .Where(v => v.SucursalId == sucursalId && v.FechaVenta.Date == fechaHoy)
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                var ventasDto = _mapper.Map<List<VentaResumenDto>>(ventas);

                _logger.LogInformation("✅ Se obtuvieron {Count} ventas del día para sucursal {SucursalId}", 
                    ventas.Count, sucursalId);

                return Ok(ventasDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo ventas del día para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene resumen de ventas por sucursal
        /// </summary>
        [HttpGet("resumen/{sucursalId}")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> GetResumenVentas(int sucursalId)
        {
            try
            {
                var fechaHoy = DateTime.UtcNow.Date;
                var fechaInicioMes = new DateTime(fechaHoy.Year, fechaHoy.Month, 1);

                var resumen = new
                {
                    VentasHoy = await _context.Ventas
                        .Where(v => v.SucursalId == sucursalId && v.FechaVenta.Date == fechaHoy && v.EstadoVenta == EstadoVenta.Completada)
                        .SumAsync(v => v.Total),
                    
                    VentasMes = await _context.Ventas
                        .Where(v => v.SucursalId == sucursalId && v.FechaVenta >= fechaInicioMes && v.EstadoVenta == EstadoVenta.Completada)
                        .SumAsync(v => v.Total),
                    
                    TransaccionesHoy = await _context.Ventas
                        .Where(v => v.SucursalId == sucursalId && v.FechaVenta.Date == fechaHoy && v.EstadoVenta == EstadoVenta.Completada)
                        .CountAsync(),
                    
                    TransaccionesMes = await _context.Ventas
                        .Where(v => v.SucursalId == sucursalId && v.FechaVenta >= fechaInicioMes && v.EstadoVenta == EstadoVenta.Completada)
                        .CountAsync(),
                    
                    TicketPromedio = await _context.Ventas
                        .Where(v => v.SucursalId == sucursalId && v.FechaVenta >= fechaInicioMes && v.EstadoVenta == EstadoVenta.Completada)
                        .AverageAsync(v => (double?)v.Total) ?? 0
                };

                _logger.LogInformation("📊 Resumen de ventas generado para sucursal {SucursalId}", sucursalId);

                return Ok(resumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando resumen de ventas para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS
        // =============================================

        private async Task<Venta?> GetVentaCompleta(int ventaId)
        {
            return await _context.Ventas
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
                .FirstOrDefaultAsync(v => v.Id == ventaId);
        }
    }
}