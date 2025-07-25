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
}
