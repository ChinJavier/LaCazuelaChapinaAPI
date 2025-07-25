// =============================================
// ARCHIVO: Controllers/ProductosController.cs
// Controlador para gestión de productos
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.DTOs.Productos;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión de productos (tamales y bebidas)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductosController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductosController> _logger;

        public ProductosController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<ProductosController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los productos con sus variantes y atributos personalizables
        /// </summary>
        /// <returns>Lista de productos disponibles</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ProductoDto>), 200)]
        public async Task<ActionResult<List<ProductoDto>>> GetProductos()
        {
            try 
            {
                var productos = await _context.Productos
                    .Include(p => p.Categoria)
                        .ThenInclude(c => c.TiposAtributo
                            .Where(ta => ta.EsObligatorio || ta.Opciones.Any(o => o.Activa)))
                        .ThenInclude(ta => ta.Opciones.Where(o => o.Activa).OrderBy(o => o.Orden))
                    .Include(p => p.Variantes.Where(v => v.Activa).OrderBy(v => v.Multiplicador))
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Categoria.Nombre)
                    .ThenBy(p => p.Nombre)
                    .ToListAsync();

                var productosDto = _mapper.Map<List<ProductoDto>>(productos);
                
                _logger.LogInformation("Se obtuvieron {Count} productos", productosDto.Count);
                return Ok(productosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo productos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un producto específico por ID
        /// </summary>
        /// <param name="id">ID del producto</param>
        /// <returns>Producto con detalles completos</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductoDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProductoDto>> GetProducto(int id)
        {
            try 
            {
                var producto = await _context.Productos
                    .Include(p => p.Categoria)
                        .ThenInclude(c => c.TiposAtributo)
                        .ThenInclude(ta => ta.Opciones.Where(o => o.Activa).OrderBy(o => o.Orden))
                    .Include(p => p.Variantes.Where(v => v.Activa).OrderBy(v => v.Multiplicador))
                    .FirstOrDefaultAsync(p => p.Id == id && p.Activo);

                if (producto == null)
                {
                    _logger.LogWarning("Producto con ID {Id} no encontrado", id);
                    return NotFound(new { message = $"Producto con ID {id} no encontrado" });
                }

                var productoDto = _mapper.Map<ProductoDto>(producto);
                return Ok(productoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo producto {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene productos por categoría (Tamales o Bebidas)
        /// </summary>
        /// <param name="categoriaId">ID de la categoría</param>
        /// <returns>Lista de productos de la categoría</returns>
        [HttpGet("categoria/{categoriaId}")]
        [ProducesResponseType(typeof(List<ProductoDto>), 200)]
        public async Task<ActionResult<List<ProductoDto>>> GetProductosPorCategoria(int categoriaId)
        {
            try 
            {
                var productos = await _context.Productos
                    .Include(p => p.Categoria)
                        .ThenInclude(c => c.TiposAtributo)
                        .ThenInclude(ta => ta.Opciones.Where(o => o.Activa).OrderBy(o => o.Orden))
                    .Include(p => p.Variantes.Where(v => v.Activa).OrderBy(v => v.Multiplicador))
                    .Where(p => p.CategoriaId == categoriaId && p.Activo)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                var productosDto = _mapper.Map<List<ProductoDto>>(productos);
                
                _logger.LogInformation("Se obtuvieron {Count} productos para categoría {CategoriaId}", 
                    productosDto.Count, categoriaId);
                
                return Ok(productosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo productos por categoría {CategoriaId}", categoriaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Calcula el precio final de un producto con sus personalizaciones
        /// </summary>
        /// <param name="request">Datos para cálculo de precio</param>
        /// <returns>Precio calculado</returns>
        [HttpPost("calcular-precio")]
        [ProducesResponseType(typeof(CalculoPrecioResponseDto), 200)]
        public async Task<ActionResult<CalculoPrecioResponseDto>> CalcularPrecio(CalculoPrecioRequestDto request)
        {
            try 
            {
                // Obtener producto y variante
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Id == request.ProductoId && p.Activo);
                
                if (producto == null)
                    return BadRequest(new { message = "Producto no encontrado" });

                var variante = await _context.VariantesProducto
                    .FirstOrDefaultAsync(v => v.Id == request.VarianteId && v.Activa);
                
                if (variante == null)
                    return BadRequest(new { message = "Variante no encontrada" });

                // Calcular precio base
                decimal precioBase = producto.PrecioBase * variante.Multiplicador;

                // Calcular precios adicionales por personalización
                decimal precioPersonalizaciones = 0;
                var personalizacionesDetalle = new List<PersonalizacionPrecioDto>();

                if (request.PersonalizacionIds?.Any() == true)
                {
                    var opciones = await _context.OpcionesAtributo
                        .Include(oa => oa.TipoAtributo)
                        .Where(oa => request.PersonalizacionIds.Contains(oa.Id) && oa.Activa)
                        .ToListAsync();

                    foreach (var opcion in opciones)
                    {
                        precioPersonalizaciones += opcion.PrecioAdicional;
                        personalizacionesDetalle.Add(new PersonalizacionPrecioDto
                        {
                            TipoAtributo = opcion.TipoAtributo.Nombre,
                            Opcion = opcion.Nombre,
                            PrecioAdicional = opcion.PrecioAdicional
                        });
                    }
                }

                var response = new CalculoPrecioResponseDto
                {
                    ProductoNombre = producto.Nombre,
                    VarianteNombre = variante.Nombre,
                    PrecioBase = precioBase,
                    PrecioPersonalizaciones = precioPersonalizaciones,
                    PrecioTotal = precioBase + precioPersonalizaciones,
                    Cantidad = request.Cantidad,
                    PrecioFinal = (precioBase + precioPersonalizaciones) * request.Cantidad,
                    Personalizaciones = personalizacionesDetalle
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando precio");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }

    // =============================================
    // DTOs ESPECÍFICOS PARA CÁLCULO DE PRECIOS
    // =============================================

    public class CalculoPrecioRequestDto
    {
        public int ProductoId { get; set; }
        public int VarianteId { get; set; }
        public int Cantidad { get; set; } = 1;
        public List<int>? PersonalizacionIds { get; set; }
    }

    public class CalculoPrecioResponseDto
    {
        public string ProductoNombre { get; set; } = string.Empty;
        public string VarianteNombre { get; set; } = string.Empty;
        public decimal PrecioBase { get; set; }
        public decimal PrecioPersonalizaciones { get; set; }
        public decimal PrecioTotal { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioFinal { get; set; }
        public List<PersonalizacionPrecioDto> Personalizaciones { get; set; } = new();
    }

    public class PersonalizacionPrecioDto
    {
        public string TipoAtributo { get; set; } = string.Empty;
        public string Opcion { get; set; } = string.Empty;
        public decimal PrecioAdicional { get; set; }
    }
}