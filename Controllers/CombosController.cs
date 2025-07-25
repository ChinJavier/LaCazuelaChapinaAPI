// =============================================
// ARCHIVO: Controllers/CombosController.cs
// Controlador para gestión de combos
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using FluentValidation;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Combos;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Combos;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión de combos (Fijos y Estacionales)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CombosController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CombosController> _logger;

        public CombosController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<CombosController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los combos activos y vigentes
        /// </summary>
        /// <returns>Lista de combos disponibles</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ComboDto>), 200)]
        public async Task<ActionResult<List<ComboDto>>> GetCombos()
        {
            try
            {
                var fechaActual = DateTime.UtcNow.Date;
                
                var combos = await _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .Where(c => c.Activo && 
                        (c.TipoCombo == TipoCombo.Fijo || 
                         (c.FechaInicioVigencia <= fechaActual && c.FechaFinVigencia >= fechaActual)))
                    .OrderBy(c => c.TipoCombo)
                    .ThenBy(c => c.Nombre)
                    .ToListAsync();

                var combosDto = _mapper.Map<List<ComboDto>>(combos);
                
                _logger.LogInformation("Se obtuvieron {Count} combos vigentes", combosDto.Count);
                return Ok(combosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo combos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un combo específico por ID
        /// </summary>
        /// <param name="id">ID del combo</param>
        /// <returns>Combo con detalles completos</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ComboDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ComboDto>> GetCombo(int id)
        {
            try
            {
                var combo = await _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

                if (combo == null)
                {
                    _logger.LogWarning("Combo con ID {Id} no encontrado", id);
                    return NotFound(new { message = $"Combo con ID {id} no encontrado" });
                }

                var comboDto = _mapper.Map<ComboDto>(combo);
                return Ok(comboDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crea un nuevo combo estacional (para administradores)
        /// </summary>
        /// <param name="crearComboDto">Datos del combo a crear</param>
        /// <returns>Combo creado</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ComboDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ComboDto>> CrearCombo(CrearComboDto crearComboDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Validar que si es estacional tenga fechas
                if (crearComboDto.TipoCombo == TipoCombo.Estacional)
                {
                    if (!crearComboDto.FechaInicioVigencia.HasValue || !crearComboDto.FechaFinVigencia.HasValue)
                    {
                        return BadRequest(new { message = "Los combos estacionales requieren fechas de vigencia" });
                    }

                    if (crearComboDto.FechaInicioVigencia > crearComboDto.FechaFinVigencia)
                    {
                        return BadRequest(new { message = "La fecha de inicio debe ser anterior a la fecha de fin" });
                    }
                }

                // Crear combo
                var combo = new Combo
                {
                    Nombre = crearComboDto.Nombre,
                    Descripcion = crearComboDto.Descripcion,
                    Precio = crearComboDto.Precio,
                    TipoCombo = crearComboDto.TipoCombo,
                    FechaInicioVigencia = crearComboDto.FechaInicioVigencia,
                    FechaFinVigencia = crearComboDto.FechaFinVigencia,
                    Activo = true,
                    EsEditable = crearComboDto.TipoCombo == TipoCombo.Estacional,
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Combos.Add(combo);
                await _context.SaveChangesAsync();

                // Agregar componentes
                foreach (var componenteDto in crearComboDto.Componentes)
                {
                    var componente = new ComboComponente
                    {
                        ComboId = combo.Id,
                        ProductoId = componenteDto.ProductoId,
                        VarianteProductoId = componenteDto.VarianteProductoId,
                        Cantidad = componenteDto.Cantidad,
                        NombreEspecial = componenteDto.NombreEspecial,
                        PrecioEspecial = componenteDto.PrecioEspecial
                    };

                    _context.ComboComponentes.Add(componente);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Obtener combo completo para respuesta
                var comboCreado = await _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .FirstAsync(c => c.Id == combo.Id);

                var comboDto = _mapper.Map<ComboDto>(comboCreado);
                
                _logger.LogInformation("Combo {Nombre} creado con ID {Id}", combo.Nombre, combo.Id);
                return CreatedAtAction(nameof(GetCombo), new { id = combo.Id }, comboDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando combo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza un combo estacional existente
        /// </summary>
        /// <param name="id">ID del combo</param>
        /// <param name="actualizarComboDto">Datos actualizados</param>
        /// <returns>Combo actualizado</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ComboDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ComboDto>> ActualizarCombo(int id, ActualizarComboDto actualizarComboDto)
        {
            try
            {
                var combo = await _context.Combos
                    .Include(c => c.Componentes)
                    .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

                if (combo == null)
                    return NotFound(new { message = "Combo no encontrado" });

                if (!combo.EsEditable)
                    return BadRequest(new { message = "Este combo no puede ser editado" });

                using var transaction = await _context.Database.BeginTransactionAsync();

                // Actualizar datos básicos
                combo.Nombre = actualizarComboDto.Nombre;
                combo.Descripcion = actualizarComboDto.Descripcion;
                combo.Precio = actualizarComboDto.Precio;
                
                if (combo.TipoCombo == TipoCombo.Estacional)
                {
                    combo.FechaInicioVigencia = actualizarComboDto.FechaInicioVigencia;
                    combo.FechaFinVigencia = actualizarComboDto.FechaFinVigencia;
                }

                // Eliminar componentes existentes
                _context.ComboComponentes.RemoveRange(combo.Componentes);

                // Agregar nuevos componentes
                foreach (var componenteDto in actualizarComboDto.Componentes)
                {
                    var componente = new ComboComponente
                    {
                        ComboId = combo.Id,
                        ProductoId = componenteDto.ProductoId,
                        VarianteProductoId = componenteDto.VarianteProductoId,
                        Cantidad = componenteDto.Cantidad,
                        NombreEspecial = componenteDto.NombreEspecial,
                        PrecioEspecial = componenteDto.PrecioEspecial
                    };

                    _context.ComboComponentes.Add(componente);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Obtener combo actualizado
                var comboActualizado = await _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .FirstAsync(c => c.Id == id);

                var comboDto = _mapper.Map<ComboDto>(comboActualizado);
                
                _logger.LogInformation("Combo {Id} actualizado", id);
                return Ok(comboDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Desactiva un combo (soft delete)
        /// </summary>
        /// <param name="id">ID del combo</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DesactivarCombo(int id)
        {
            try
            {
                var combo = await _context.Combos.FindAsync(id);
                
                if (combo == null)
                    return NotFound(new { message = "Combo no encontrado" });

                if (!combo.EsEditable)
                    return BadRequest(new { message = "Este combo no puede ser eliminado" });

                combo.Activo = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Combo {Id} desactivado", id);
                return Ok(new { message = "Combo desactivado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene combos por tipo (Fijo o Estacional)
        /// </summary>
        /// <param name="tipo">Tipo de combo</param>
        /// <returns>Lista de combos del tipo especificado</returns>
        [HttpGet("tipo/{tipo}")]
        [ProducesResponseType(typeof(List<ComboDto>), 200)]
        public async Task<ActionResult<List<ComboDto>>> GetCombosPorTipo(TipoCombo tipo)
        {
            try
            {
                var fechaActual = DateTime.UtcNow.Date;
                
                var combos = await _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .Where(c => c.Activo && c.TipoCombo == tipo &&
                        (tipo == TipoCombo.Fijo || 
                         (c.FechaInicioVigencia <= fechaActual && c.FechaFinVigencia >= fechaActual)))
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                var combosDto = _mapper.Map<List<ComboDto>>(combos);
                
                _logger.LogInformation("Se obtuvieron {Count} combos de tipo {Tipo}", combosDto.Count, tipo);
                return Ok(combosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo combos por tipo {Tipo}", tipo);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }

    // =============================================
    // DTOs ESPECÍFICOS PARA COMBOS
    // =============================================

    public class CrearComboDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public TipoCombo TipoCombo { get; set; }
        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public List<CrearComboComponenteDto> Componentes { get; set; } = new();
    }

    public class ActualizarComboDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public List<CrearComboComponenteDto> Componentes { get; set; } = new();
    }

    public class CrearComboComponenteDto
    {
        public int? ProductoId { get; set; }
        public int? VarianteProductoId { get; set; }
        public int Cantidad { get; set; }
        public string? NombreEspecial { get; set; }
        public decimal? PrecioEspecial { get; set; }
    }
}