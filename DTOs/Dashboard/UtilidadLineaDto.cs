namespace LaCazuelaChapina.API.DTOs.Dashboard
{
    public class UtilidadLineaDto
    {
        public string Linea { get; set; } = string.Empty;
        public decimal Ingresos { get; set; }
        public decimal Costos { get; set; }
        public decimal Utilidad { get; set; }
        public decimal MargenPorcentaje { get; set; }
    }
}