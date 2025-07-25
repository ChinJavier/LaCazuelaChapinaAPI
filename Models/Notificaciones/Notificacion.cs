using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.Models.Sucursales;

namespace LaCazuelaChapina.API.Models.Notificaciones
{
    [Table("notificaciones")]
    public class Notificacion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sucursal_id")]
        public int SucursalId { get; set; }

        [Column("tipo_notificacion")]
        public TipoNotificacion TipoNotificacion { get; set; }

        [Required(ErrorMessage = "El título de la notificación es obligatorio")]
        [MaxLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje de la notificación es obligatorio")]
        [MaxLength(300, ErrorMessage = "El mensaje no puede exceder 300 caracteres")]
        [Column("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_envio")]
        public DateTime? FechaEnvio { get; set; }

        [Column("enviada")]
        public bool Enviada { get; set; } = false;

        [Column("referencia_id")]
        public int? ReferenciaId { get; set; }

        // Navegación
        public virtual Sucursal Sucursal { get; set; } = null!;

        // Solo método básico de cambio de estado
        public void MarcarComoEnviada()
        {
            Enviada = true;
            FechaEnvio = DateTime.UtcNow;
        }
    }
}