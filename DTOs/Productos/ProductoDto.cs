using LaCazuelaChapina.API.DTOs.Personalizacion;

namespace LaCazuelaChapina.API.DTOs.Productos
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioBase { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public List<VarianteProductoDto> Variantes { get; set; } = new();
        public List<TipoAtributoDto> AtributosPersonalizables { get; set; } = new();
    }

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
