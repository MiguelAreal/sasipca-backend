namespace sasipca_API.Dtos
{
    public class AnuncioResumoDTO
    {
        public int Id { get; set; }
        public string Tipo { get; set; } // Produto, Servico, Evento
        public string Nome { get; set; }
        public string Estado { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
