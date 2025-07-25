namespace LaCazuelaChapina.API.DTOs.Productos
{
    public class VarianteProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Multiplicador { get; set; }
        public int CantidadUnidades { get; set; }
        public int? VolumenMl { get; set; }
    }
}