namespace LaCazuelaChapina.API.DTOs.Ventas
{
    public class DetalleVentaDto
    {
        public int? ProductoId { get; set; }
        public int? VarianteProductoId { get; set; }
        public int? ComboId { get; set; }
        public int Cantidad { get; set; }
        public string? Notas { get; set; }
        public List<PersonalizacionDto> Personalizaciones { get; set; } = new();
    }
}