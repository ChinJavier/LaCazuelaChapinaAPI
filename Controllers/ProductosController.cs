using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.DTOs.Productos;

namespace LaCazuelaChapina.API.Controllers
{
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
        [HttpGet]
        [ProducesResponseType(typeof(List<ProductoDto>), 200)]
        public async Task<ActionResult<List<ProductoDto>>> GetProductos()
        {
            try 
            {
                var productos = await _context.Productos
                    .Include(p => p.Categoria)
                        .ThenInclude(c => c.TiposAtributo)
                        .ThenInclude(ta => ta.Opciones.Where(o => o.Activa).OrderBy(o => o.Orden))
                    .Include(p => p.Variantes.Where(v => v.Activa).OrderBy(v => v.Multiplicador))
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Categoria.Nombre)
                    .ThenBy(p => p.Nombre)
                    .ToListAsync();

                var productosDto = _mapper.Map<List<ProductoDto>>(productos);
                
                _logger.LogInformation("✅ Se obtuvieron {Count} productos", productosDto.Count);
                return Ok(productosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo productos");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene un producto específico por ID
        /// </summary>
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
                    _logger.LogWarning("⚠️ Producto con ID {Id} no encontrado", id);
                    return NotFound(new { message = $"Producto con ID {id} no encontrado" });
                }

                var productoDto = _mapper.Map<ProductoDto>(producto);
                return Ok(productoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo producto {Id}", id);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene productos por categoría (Tamales o Bebidas)
        /// </summary>
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
                
                _logger.LogInformation("✅ Se obtuvieron {Count} productos para categoría {CategoriaId}", 
                    productosDto.Count, categoriaId);
                
                return Ok(productosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo productos por categoría {CategoriaId}", categoriaId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Calcula el precio final de un producto con sus personalizaciones
        /// </summary>
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

                _logger.LogInformation("💰 Precio calculado: {ProductoNombre} = Q{PrecioFinal}", 
                    producto.Nombre, response.PrecioFinal);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calculando precio");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene las categorías disponibles
        /// </summary>
        [HttpGet("categorias")]
        [ProducesResponseType(typeof(List<object>), 200)]
        public async Task<ActionResult> GetCategorias()
        {
            try
            {
                var categorias = await _context.Categorias
                    .Where(c => c.Activa)
                    .Select(c => new { 
                        Id = c.Id, 
                        Nombre = c.Nombre, 
                        Descripcion = c.Descripcion,
                        CantidadProductos = c.Productos.Count(p => p.Activo)
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                _logger.LogInformation("✅ Se obtuvieron {Count} categorías", categorias.Count);
                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo categorías");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }
    }
}