namespace LaCazuelaChapina.API.Models.Enums
{
    /// <summary>
    /// Tipos de notificaciones del sistema
    /// </summary>
    public enum TipoNotificacion
    {
        /// <summary>
        /// Notificación de nueva venta realizada
        /// </summary>
        Venta,
        
        /// <summary>
        /// Notificación de finalización de cocción
        /// </summary>
        FinCoccion,
        
        /// <summary>
        /// Alerta de stock bajo en materias primas
        /// </summary>
        StockBajo,
        
        /// <summary>
        /// Notificaciones del sistema (actualizaciones, errores)
        /// </summary>
        Sistema
    }
}