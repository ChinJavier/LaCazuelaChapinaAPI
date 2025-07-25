namespace LaCazuelaChapina.API.DTOs.Dashboard
{
    public class DashboardDto
    {
        public decimal VentasDiarias { get; set; }
        public decimal VentasMensuales { get; set; }
        public List<ProductoVendidoDto> TamalesMasVendidos { get; set; } = new();
        public List<BebidaPorHorarioDto> BebidasPorHorario { get; set; } = new();
        public ProporcionPicanteDto ProporcionPicante { get; set; } = new();
        public List<UtilidadLineaDto> UtilidadesPorLinea { get; set; } = new();
        public List<DesperdicioDto> DesperdicioMateriasPrimas { get; set; } = new();
    }
}