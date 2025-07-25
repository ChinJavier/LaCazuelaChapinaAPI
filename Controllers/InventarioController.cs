// =============================================
// ARCHIVO: Controllers/InventarioController.cs
// Controlador para gestión de inventario
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Inventario;
using LaCazuelaChapina.API.Models.Enums;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión de inventario y materias primas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InventarioController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<InventarioController> _logger;

        public InventarioController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<InventarioController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el stock actual de una sucursal
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>Lista de stock por materia prima</returns>
        [HttpGet("stock/sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(List<StockSucursalDto>), 200)]
        public async Task<ActionResult<List<StockSucursalDto>>> GetStockSucursal(int sucursalId)
        {
            try
            {
                var stocks = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .Where(ss => ss.SucursalId == sucursalId)
                    .OrderBy(ss => ss.MateriaPrima.Categoria.Nombre)
                    .ThenBy(ss => ss.MateriaPrima.Nombre)
                    .ToListAsync();

                var stocksDto = stocks.Select(ss => new StockSucursalDto
                {
                    Id = ss.Id,
                    SucursalNombre = ss.Sucursal.Nombre,
                    MateriaPrimaNombre = ss.MateriaPrima.Nombre,
                    CategoriaNombre = ss.MateriaPrima.Categoria.Nombre,
                    CantidadActual = ss.CantidadActual,
                    UnidadMedida = ss.MateriaPrima.UnidadMedida,
                    StockMinimo = ss.MateriaPrima.StockMinimo,
                    StockMaximo = ss.MateriaPrima.StockMaximo,
                    CostoPromedio = ss.MateriaPrima.CostoPromedio,
                    EstadoStock = DeterminarEstadoStock(ss.CantidadActual, ss.MateriaPrima.StockMinimo),
                    FechaUltimaActualizacion = ss.FechaUltimaActualizacion
                }).ToList();

                _logger.LogInformation("Se obtuvo stock de {Count} materias primas para sucursal {SucursalId}",
                    stocksDto.Count, sucursalId);

                return Ok(stocksDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo stock de sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene alertas de stock bajo para una sucursal
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>Lista de materias primas con stock bajo</returns>
        [HttpGet("alertas/sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(List<AlertaStockDto>), 200)]
        public async Task<ActionResult<List<AlertaStockDto>>> GetAlertasStock(int sucursalId)
        {
            try
            {
                var alertas = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .Where(ss => ss.SucursalId == sucursalId &&
                               ss.CantidadActual <= ss.MateriaPrima.StockMinimo)
                    .OrderBy(ss => ss.CantidadActual / ss.MateriaPrima.StockMinimo)
                    .Select(ss => new AlertaStockDto
                    {
                        MateriaPrimaNombre = ss.MateriaPrima.Nombre,
                        CategoriaNombre = ss.MateriaPrima.Categoria.Nombre,
                        CantidadActual = ss.CantidadActual,
                        StockMinimo = ss.MateriaPrima.StockMinimo,
                        UnidadMedida = ss.MateriaPrima.UnidadMedida,
                        PorcentajeStock = ss.CantidadActual / ss.MateriaPrima.StockMinimo * 100,
                        TipoAlerta = ss.CantidadActual <= 0 ? "AGOTADO" : "STOCK_BAJO",
                        FechaDeteccion = DateTime.UtcNow
                    })
                    .ToListAsync();

                _logger.LogInformation("Se encontraron {Count} alertas de stock para sucursal {SucursalId}",
                    alertas.Count, sucursalId);

                return Ok(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo alertas de stock para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Registra una entrada de inventario (compra)
        /// </summary>
        /// <param name="entradaDto">Datos de la entrada</param>
        /// <returns>Movimiento de inventario registrado</returns>
        [HttpPost("entrada")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MovimientoInventarioDto>> RegistrarEntrada(RegistrarEntradaDto entradaDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Validar que existe la materia prima y sucursal
                var stock = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                    .Include(ss => ss.Sucursal)
                    .FirstOrDefaultAsync(ss => ss.SucursalId == entradaDto.SucursalId &&
                                             ss.MateriaPrimaId == entradaDto.MateriaPrimaId);

                if (stock == null)
                    return BadRequest(new { message = "Stock no encontrado para la sucursal y materia prima especificadas" });

                // Registrar movimiento de entrada
                var movimiento = new MovimientoInventario
                {
                    SucursalId = entradaDto.SucursalId,
                    MateriaPrimaId = entradaDto.MateriaPrimaId,
                    TipoMovimiento = TipoMovimiento.Entrada,
                    Cantidad = entradaDto.Cantidad,
                    CostoUnitario = entradaDto.CostoUnitario,
                    MontoTotal = entradaDto.Cantidad * entradaDto.CostoUnitario,
                    Motivo = entradaDto.Motivo,
                    DocumentoReferencia = entradaDto.DocumentoReferencia,
                    FechaMovimiento = DateTime.UtcNow
                };

                _context.MovimientosInventario.Add(movimiento);

                // Actualizar stock actual
                stock.CantidadActual += entradaDto.Cantidad;
                stock.FechaUltimaActualizacion = DateTime.UtcNow;

                // Actualizar costo promedio (FIFO ponderado)
                var costoTotalAnterior = stock.MateriaPrima.CostoPromedio * (stock.CantidadActual - entradaDto.Cantidad);
                var costoTotalNuevo = entradaDto.CostoUnitario * entradaDto.Cantidad;
                stock.MateriaPrima.CostoPromedio = (costoTotalAnterior + costoTotalNuevo) / stock.CantidadActual;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Obtener movimiento completo para respuesta
                var movimientoCompleto = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstAsync(mi => mi.Id == movimiento.Id);

                var movimientoDto = new MovimientoInventarioDto
                {
                    Id = movimientoCompleto.Id,
                    SucursalNombre = movimientoCompleto.Sucursal.Nombre,
                    MateriaPrimaNombre = movimientoCompleto.MateriaPrima.Nombre,
                    TipoMovimiento = movimientoCompleto.TipoMovimiento,
                    Cantidad = movimientoCompleto.Cantidad,
                    CostoUnitario = movimientoCompleto.CostoUnitario,
                    MontoTotal = movimientoCompleto.MontoTotal,
                    Motivo = movimientoCompleto.Motivo,
                    DocumentoReferencia = movimientoCompleto.DocumentoReferencia,
                    FechaMovimiento = movimientoCompleto.FechaMovimiento
                };

                _logger.LogInformation("Entrada de inventario registrada: {Cantidad} {UnidadMedida} de {MateriaPrima}",
                    entradaDto.Cantidad, stock.MateriaPrima.UnidadMedida, stock.MateriaPrima.Nombre);

                return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.Id }, movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando entrada de inventario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Registra una merma de inventario
        /// </summary>
        /// <param name="mermaDto">Datos de la merma</param>
        /// <returns>Movimiento de inventario registrado</returns>
        [HttpPost("merma")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MovimientoInventarioDto>> RegistrarMerma(RegistrarMermaDto mermaDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var stock = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                    .Include(ss => ss.Sucursal)
                    .FirstOrDefaultAsync(ss => ss.SucursalId == mermaDto.SucursalId &&
                                             ss.MateriaPrimaId == mermaDto.MateriaPrimaId);

                if (stock == null)
                    return BadRequest(new { message = "Stock no encontrado" });

                if (stock.CantidadActual < mermaDto.Cantidad)
                    return BadRequest(new { message = "No hay suficiente stock para registrar la merma" });

                // Registrar movimiento de merma
                var movimiento = new MovimientoInventario
                {
                    SucursalId = mermaDto.SucursalId,
                    MateriaPrimaId = mermaDto.MateriaPrimaId,
                    TipoMovimiento = TipoMovimiento.Merma,
                    Cantidad = mermaDto.Cantidad,
                    CostoUnitario = stock.MateriaPrima.CostoPromedio,
                    MontoTotal = mermaDto.Cantidad * stock.MateriaPrima.CostoPromedio,
                    Motivo = mermaDto.Motivo,
                    FechaMovimiento = DateTime.UtcNow
                };

                _context.MovimientosInventario.Add(movimiento);

                // Actualizar stock actual
                stock.CantidadActual -= mermaDto.Cantidad;
                stock.FechaUltimaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // TODO: Enviar notificación si el stock queda por debajo del mínimo

                var movimientoCompleto = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstAsync(mi => mi.Id == movimiento.Id);

                var movimientoDto = new MovimientoInventarioDto
                {
                    Id = movimientoCompleto.Id,
                    SucursalNombre = movimientoCompleto.Sucursal.Nombre,
                    MateriaPrimaNombre = movimientoCompleto.MateriaPrima.Nombre,
                    TipoMovimiento = movimientoCompleto.TipoMovimiento,
                    Cantidad = movimientoCompleto.Cantidad,
                    CostoUnitario = movimientoCompleto.CostoUnitario,
                    MontoTotal = movimientoCompleto.MontoTotal,
                    Motivo = movimientoCompleto.Motivo,
                    FechaMovimiento = movimientoCompleto.FechaMovimiento
                };

                _logger.LogInformation("Merma registrada: {Cantidad} {UnidadMedida} de {MateriaPrima}. Motivo: {Motivo}",
                    mermaDto.Cantidad, stock.MateriaPrima.UnidadMedida, stock.MateriaPrima.Nombre, mermaDto.Motivo);

                return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.Id }, movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando merma de inventario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un movimiento de inventario por ID
        /// </summary>
        /// <param name="id">ID del movimiento</param>
        /// <returns>Movimiento de inventario</returns>
        [HttpGet("movimiento/{id}")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MovimientoInventarioDto>> GetMovimiento(int id)
        {
            try
            {
                var movimiento = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstOrDefaultAsync(mi => mi.Id == id);

                if (movimiento == null)
                    return NotFound(new { message = "Movimiento no encontrado" });

                var movimientoDto = new MovimientoInventarioDto
                {
                    Id = movimiento.Id,
                    SucursalNombre = movimiento.Sucursal.Nombre,
                    MateriaPrimaNombre = movimiento.MateriaPrima.Nombre,
                    TipoMovimiento = movimiento.TipoMovimiento,
                    Cantidad = movimiento.Cantidad,
                    CostoUnitario = movimiento.CostoUnitario,
                    MontoTotal = movimiento.MontoTotal,
                    Motivo = movimiento.Motivo,
                    DocumentoReferencia = movimiento.DocumentoReferencia,
                    FechaMovimiento = movimiento.FechaMovimiento
                };

                return Ok(movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo movimiento {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene historial de movimientos de una sucursal
        /// </summary>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <param name="fechaDesde">Fecha desde (opcional)</param>
        /// <param name="fechaHasta">Fecha hasta (opcional)</param>
        /// <param name="tipoMovimiento">Tipo de movimiento (opcional)</param>
        /// <returns>Lista de movimientos</returns>
        [HttpGet("movimientos/sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(List<MovimientoInventarioDto>), 200)]
        public async Task<ActionResult<List<MovimientoInventarioDto>>> GetMovimientosSucursal(
            int sucursalId,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] TipoMovimiento? tipoMovimiento = null)
        {
            try
            {
                var query = _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .Where(mi => mi.SucursalId == sucursalId);

                if (fechaDesde.HasValue)
                    query = query.Where(mi => mi.FechaMovimiento.Date >= fechaDesde.Value.Date);

                if (fechaHasta.HasValue)
                    query = query.Where(mi => mi.FechaMovimiento.Date <= fechaHasta.Value.Date);

                if (tipoMovimiento.HasValue)
                    query = query.Where(mi => mi.TipoMovimiento == tipoMovimiento.Value);

                var movimientos = await query
                    .OrderByDescending(mi => mi.FechaMovimiento)
                    .Take(100) // Limitar a los últimos 100 movimientos
                    .ToListAsync();

                var movimientosDto = movimientos.Select(mi => new MovimientoInventarioDto
                {
                    Id = mi.Id,
                    SucursalNombre = mi.Sucursal.Nombre,
                    MateriaPrimaNombre = mi.MateriaPrima.Nombre,
                    TipoMovimiento = mi.TipoMovimiento,
                    Cantidad = mi.Cantidad,
                    CostoUnitario = mi.CostoUnitario,
                    MontoTotal = mi.MontoTotal,
                    Motivo = mi.Motivo,
                    DocumentoReferencia = mi.DocumentoReferencia,
                    FechaMovimiento = mi.FechaMovimiento
                }).ToList();

                _logger.LogInformation("Se obtuvieron {Count} movimientos para sucursal {SucursalId}",
                    movimientosDto.Count, sucursalId);

                return Ok(movimientosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo movimientos de sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS
        // =============================================

        private static string DeterminarEstadoStock(decimal cantidadActual, decimal stockMinimo)
        {
            if (cantidadActual <= 0)
                return "🔴 AGOTADO";
            else if (cantidadActual <= stockMinimo)
                return "🟡 REORDENAR";
            else
                return "🟢 OK";
        }
    }

    // =============================================
    // DTOs ESPECÍFICOS PARA INVENTARIO
    // =============================================

    public class StockSucursalDto
    {
        public int Id { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public decimal CantidadActual { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockMinimo { get; set; }
        public decimal StockMaximo { get; set; }
        public decimal CostoPromedio { get; set; }
        public string EstadoStock { get; set; } = string.Empty;
        public DateTime FechaUltimaActualizacion { get; set; }
    }

    public class AlertaStockDto
    {
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public decimal CantidadActual { get; set; }
        public decimal StockMinimo { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal PorcentajeStock { get; set; }
        public string TipoAlerta { get; set; } = string.Empty;
        public DateTime FechaDeteccion { get; set; }
    }

    public class MovimientoInventarioDto
    {
        public int Id { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public TipoMovimiento TipoMovimiento { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        public decimal? MontoTotal { get; set; }
        public string? Motivo { get; set; }
        public string? DocumentoReferencia { get; set; }
        public DateTime FechaMovimiento { get; set; }
    }

    public class RegistrarEntradaDto
    {
        public int SucursalId { get; set; }
        public int MateriaPrimaId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? DocumentoReferencia { get; set; }
    }

    public class RegistrarMermaDto
    {
        public int SucursalId { get; set; }
        public int MateriaPrimaId { get; set; }
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}