using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.Models.Ventas;

namespace LaCazuelaChapina.API.Models.Combos
{
    [Table("combos")]
    public class Combo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del combo es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("precio", TypeName = "decimal(10,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Column("tipo_combo")]
        public TipoCombo TipoCombo { get; set; }

        [Column("fecha_inicio_vigencia")]
        public DateTime? FechaInicioVigencia { get; set; }

        [Column("fecha_fin_vigencia")]
        public DateTime? FechaFinVigencia { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("es_editable")]
        public bool EsEditable { get; set; } = false;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual ICollection<ComboComponente> Componentes { get; set; } = new List<ComboComponente>();
        public virtual ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
    }
}