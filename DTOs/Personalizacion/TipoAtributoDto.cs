namespace LaCazuelaChapina.API.DTOs.Personalizacion
{
    public class TipoAtributoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsObligatorio { get; set; }
        public bool PermiteMultiple { get; set; }
        public List<OpcionAtributoDto> Opciones { get; set; } = new();
    }

}