namespace LaCazuelaChapina.API.DTOs.Dashboard
{
    public class DesperdicioDto
    {
        public string MateriaPrima { get; set; } = string.Empty;
        public decimal CantidadDesperdiciada { get; set; }
        public decimal CostoDelDesperdicio { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
    }
}