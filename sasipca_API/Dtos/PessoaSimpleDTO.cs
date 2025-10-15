namespace sasipca_API.Dtos
{

    /// <summary>
    /// DTO mais simplificada de uma pessoa contendo apenas informações essenciais.
    /// Utilizada para representar um objeto de uma pessoa:
    /// Executor (Serviços),
    /// Criador (Serviços),
    /// Comprador (Produtos)
    /// </summary>
    public class PessoaSimpleDTO
    {
        /// <summary>
        /// ID da pessoa.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome da pessoa.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Contacto da pessoa.
        /// </summary>
        public string Contacto { get; set; } = string.Empty;
    }
}
