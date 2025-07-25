namespace LaCazuelaChapina.API.DTOs.Dashboard
{
    public class BebidaPorHorarioDto
    {
        public string TipoBebida { get; set; } = string.Empty;
        public int Hora { get; set; }
        public int CantidadVendida { get; set; }
    }
}