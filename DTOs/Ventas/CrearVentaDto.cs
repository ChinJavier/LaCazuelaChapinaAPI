using LaCazuelaChapina.API.Models.Enums;

namespace LaCazuelaChapina.API.DTOs.Ventas
{
    public class CrearVentaDto
    {
        public int SucursalId { get; set; }
        public string? ClienteNombre { get; set; }
        public string? ClienteTelefono { get; set; }
        public TipoPago TipoPago { get; set; }
        public List<DetalleVentaDto> Detalles { get; set; } = new();
    }
}