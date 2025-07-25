namespace LaCazuelaChapina.API.DTOs.Combos
{
    public class ComboComponenteDto
    {
        public int Id { get; set; }
        public int? ProductoId { get; set; }
        public int? VarianteProductoId { get; set; }
        public int Cantidad { get; set; }
        public string? NombreEspecial { get; set; }
        public decimal? PrecioEspecial { get; set; }
        public string? ProductoNombre { get; set; }
        public string? VarianteNombre { get; set; }
    }
}