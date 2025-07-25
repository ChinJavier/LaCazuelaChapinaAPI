namespace LaCazuelaChapina.API.Models.Enums
{
    /// <summary>
    /// Tipos de movimiento de inventario
    /// </summary>
    public enum TipoMovimiento
    {
        /// <summary>
        /// Entrada de mercancía (compras, recepciones)
        /// </summary>
        Entrada,
        
        /// <summary>
        /// Salida por consumo en producción
        /// </summary>
        Salida,
        
        /// <summary>
        /// Pérdida por deterioro, vencimiento, etc.
        /// </summary>
        Merma,
        
        /// <summary>
        /// Corrección por inventario físico
        /// </summary>
        Ajuste
    }
}