using LaCazuelaChapina.API.Models.Enums;

namespace LaCazuelaChapina.API.DTOs.Combos
{
    public class ComboDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public TipoCombo TipoCombo { get; set; }
        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public bool Activo { get; set; }
        public List<ComboComponenteDto> Componentes { get; set; } = new();
    }
}