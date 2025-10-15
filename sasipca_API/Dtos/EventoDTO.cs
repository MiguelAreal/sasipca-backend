namespace sasipca_API.Dtos
{
    /// <summary>
    /// Classe base de Data Transfer Object para eventos.
    /// </summary>
    public class EventoDTO
    {
        public string Nome { get; set; } = null!;
        public string Morada { get; set; } = null!;
        public int? NumMinPessoas { get; set; }
        public string? Descricao { get; set; }
        public DateTime DataIni { get; set; }
    }

}
