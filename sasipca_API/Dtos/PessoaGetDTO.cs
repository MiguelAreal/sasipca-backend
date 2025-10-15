namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para ir buscar pessoa(s).
    /// </summary>
    public class PessoaGetDTO : UserDTO
    {
        public int IdPessoa { get; set; }
        public double MediaAvaliacoes { get; set; }
        public int NumeroAnuncios { get; set; }
        public DateTime? DataCriacao { get; set; }
    }
}
