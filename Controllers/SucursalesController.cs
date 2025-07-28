// =============================================
// ARCHIVO: Controllers/SucursalesController.cs
// Gestión de Sucursales - La Cazuela Chapina
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Sucursales;
using LaCazuelaChapina.API.DTOs.Sucursales;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión de sucursales del sistema
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SucursalesController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SucursalesController> _logger;

        public SucursalesController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<SucursalesController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // =============================================
        // CONSULTAS GENERALES
        // =============================================

        /// <summary>
        /// Obtiene todas las sucursales activas
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SucursalDto>>> GetSucursales()
        {
            try
            {
                var sucursales = await _context.Sucursales
                    .Where(s => s.Activa)
                    .OrderBy(s => s.Nombre)
                    .ToListAsync();

                var sucursalesDto = _mapper.Map<List<SucursalDto>>(sucursales);
                return Ok(sucursalesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las sucursales");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene una sucursal específica por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SucursalDetalleDto>> GetSucursal(int id)
        {
            try
            {
                var sucursal = await _context.Sucursales
                    .Include(s => s.Ventas.Where(v => v.FechaVenta.Date == DateTime.UtcNow.Date))
                    .Include(s => s.Stocks)
                        .ThenInclude(st => st.MateriaPrima)
                    .Include(s => s.MovimientosInventario.Where(m => m.FechaMovimiento.Date == DateTime.UtcNow.Date))
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sucursal == null)
                {
                    return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });
                }

                var sucursalDetalle = _mapper.Map<SucursalDetalleDto>(sucursal);
                
                // Calcular estadísticas del día
                sucursalDetalle.VentasHoy = sucursal.Ventas.Count;
                sucursalDetalle.IngresosDiarios = sucursal.Ventas.Sum(v => v.Total);
                sucursalDetalle.ProductosCriticos = sucursal.Stocks
                    .Count(s => s.CantidadActual <= s.MateriaPrima.StockMinimo);

                return Ok(sucursalDetalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la sucursal {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // REPORTES POR SUCURSAL
        // =============================================

        /// <summary>
        /// Obtiene el reporte de ventas de una sucursal por período
        /// </summary>
        [HttpGet("{id}/reportes/ventas")]
        public async Task<ActionResult<ReporteVentasSucursalDto>> GetReporteVentas(
            int id,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                // Valores por defecto: últimos 30 días
                fechaInicio ??= DateTime.UtcNow.AddDays(-30).Date;
                fechaFin ??= DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

                var sucursal = await _context.Sucursales
                    .FirstOrDefaultAsync(s => s.Id == id && s.Activa);

                if (sucursal == null)
                {
                    return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });
                }

                var ventas = await _context.Ventas
                    .Where(v => v.SucursalId == id && 
                               v.FechaVenta >= fechaInicio && 
                               v.FechaVenta <= fechaFin &&
                               v.EstadoVenta == Models.Enums.EstadoVenta.Completada)
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Combo)
                    .ToListAsync();

                var reporte = new ReporteVentasSucursalDto
                {
                    SucursalId = id,
                    SucursalNombre = sucursal.Nombre,
                    FechaInicio = fechaInicio.Value,
                    FechaFin = fechaFin.Value,
                    TotalVentas = ventas.Count,
                    MontoTotal = ventas.Sum(v => v.Total),
                    TicketPromedio = ventas.Any() ? ventas.Average(v => v.Total) : 0,
                    VentasPorDia = ventas
                        .GroupBy(v => v.FechaVenta.Date)
                        .Select(g => new VentaDiariaDto
                        {
                            Fecha = g.Key,
                            CantidadVentas = g.Count(),
                            MontoTotal = g.Sum(v => v.Total),
                            TicketPromedio = g.Average(v => v.Total)
                        })
                        .OrderBy(vd => vd.Fecha)
                        .ToList(),
                    ProductosMasVendidos = ventas
                        .SelectMany(v => v.Detalles)
                        .Where(d => d.Producto != null)
                        .GroupBy(d => d.Producto!.Nombre)
                        .Select(g => new ProductoVendidoSucursalDto
                        {
                            Nombre = g.Key,
                            CantidadVendida = g.Sum(d => d.Cantidad),
                            MontoTotal = g.Sum(d => d.Subtotal)
                        })
                        .OrderByDescending(p => p.CantidadVendida)
                        .Take(10)
                        .ToList()
                };

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de ventas para sucursal {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el estado del inventario de una sucursal
        /// </summary>
        [HttpGet("{id}/inventario/estado")]
        public async Task<ActionResult<EstadoInventarioSucursalDto>> GetEstadoInventario(int id)
        {
            try
            {
                var sucursal = await _context.Sucursales
                    .FirstOrDefaultAsync(s => s.Id == id && s.Activa);

                if (sucursal == null)
                {
                    return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });
                }

                var stocks = await _context.StockSucursal
                    .Where(s => s.SucursalId == id)
                    .Include(s => s.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .ToListAsync();

                var estado = new EstadoInventarioSucursalDto
                {
                    SucursalId = id,
                    SucursalNombre = sucursal.Nombre,
                    TotalProductos = stocks.Count,
                    ProductosCriticos = stocks.Count(s => s.CantidadActual <= s.MateriaPrima.StockMinimo),
                    ProductosAgotados = stocks.Count(s => s.CantidadActual <= 0),
                    ValorTotalInventario = stocks.Sum(s => s.CantidadActual * s.MateriaPrima.CostoPromedio),
                    ProductosPorCategoria = stocks
                        .GroupBy(s => s.MateriaPrima.Categoria.Nombre)
                        .Select(g => new CategoriStockDto
                        {
                            Categoria = g.Key,
                            CantidadProductos = g.Count(),
                            ProductosCriticos = g.Count(s => s.CantidadActual <= s.MateriaPrima.StockMinimo),
                            ValorInventario = g.Sum(s => s.CantidadActual * s.MateriaPrima.CostoPromedio)
                        })
                        .OrderBy(c => c.Categoria)
                        .ToList(),
                    ProductosCriticosDetalle = stocks
                        .Where(s => s.CantidadActual <= s.MateriaPrima.StockMinimo)
                        .Select(s => new ProductoCriticoDto
                        {
                            Nombre = s.MateriaPrima.Nombre,
                            CantidadActual = s.CantidadActual,
                            StockMinimo = s.MateriaPrima.StockMinimo,
                            UnidadMedida = s.MateriaPrima.UnidadMedida,
                            Categoria = s.MateriaPrima.Categoria.Nombre
                        })
                        .OrderBy(p => p.CantidadActual / p.StockMinimo)
                        .ToList()
                };

                return Ok(estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de inventario para sucursal {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // GESTIÓN DE SUCURSALES
        // =============================================

        /// <summary>
        /// Crea una nueva sucursal
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SucursalDto>> CrearSucursal(CrearSucursalDto crearSucursalDto)
        {
            try
            {
                var sucursal = _mapper.Map<Sucursal>(crearSucursalDto);
                sucursal.FechaCreacion = DateTime.UtcNow;
                sucursal.Activa = true;

                _context.Sucursales.Add(sucursal);
                await _context.SaveChangesAsync();

                var sucursalDto = _mapper.Map<SucursalDto>(sucursal);
                
                _logger.LogInformation("Sucursal {Nombre} creada con ID {Id}", sucursal.Nombre, sucursal.Id);
                
                return CreatedAtAction(nameof(GetSucursal), new { id = sucursal.Id }, sucursalDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear sucursal");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza una sucursal existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarSucursal(int id, ActualizarSucursalDto actualizarSucursalDto)
        {
            try
            {
                var sucursal = await _context.Sucursales.FindAsync(id);
                
                if (sucursal == null)
                {
                    return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });
                }

                _mapper.Map(actualizarSucursalDto, sucursal);
                sucursal.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Sucursal {Id} actualizada", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar sucursal {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Desactiva una sucursal (no elimina físicamente)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DesactivarSucursal(int id)
        {
            try
            {
                var sucursal = await _context.Sucursales.FindAsync(id);
                
                if (sucursal == null)
                {
                    return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });
                }

                sucursal.Activa = false;
                sucursal.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Sucursal {Id} desactivada", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar sucursal {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // ESTADO Y SINCRONIZACIÓN
        // =============================================

        /// <summary>
        /// Obtiene el estado general de todas las sucursales
        /// </summary>
        [HttpGet("estado")]
        public async Task<ActionResult<EstadoGeneralSucursalesDto>> GetEstadoGeneral()
        {
            try
            {
                var sucursales = await _context.Sucursales
                    .Where(s => s.Activa)
                    .Include(s => s.Ventas.Where(v => v.FechaVenta.Date == DateTime.UtcNow.Date))
                    .Include(s => s.Stocks)
                        .ThenInclude(st => st.MateriaPrima)
                    .ToListAsync();

                var estado = new EstadoGeneralSucursalesDto
                {
                    TotalSucursales = sucursales.Count,
                    SucursalesActivas = sucursales.Count(s => s.Activa),
                    VentasTotalesHoy = sucursales.Sum(s => s.Ventas.Count),
                    IngresosTotalesHoy = sucursales.Sum(s => s.Ventas.Sum(v => v.Total)),
                    SucursalesConAlertas = sucursales.Count(s => 
                        s.Stocks.Any(st => st.CantidadActual <= st.MateriaPrima.StockMinimo)),
                    ResumenPorSucursal = sucursales.Select(s => new ResumenSucursalDto
                    {
                        Id = s.Id,
                        Nombre = s.Nombre,
                        VentasHoy = s.Ventas.Count,
                        IngresosHoy = s.Ventas.Sum(v => v.Total),
                        ProductosCriticos = s.Stocks.Count(st => 
                            st.CantidadActual <= st.MateriaPrima.StockMinimo),
                        UltimaActualizacion = s.FechaActualizacion
                    }).ToList()
                };

                return Ok(estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado general de sucursales");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}