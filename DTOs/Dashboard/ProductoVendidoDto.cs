namespace LaCazuelaChapina.API.DTOs.Dashboard
{
    public class ProductoVendidoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal MontoTotal { get; set; }
    }
}