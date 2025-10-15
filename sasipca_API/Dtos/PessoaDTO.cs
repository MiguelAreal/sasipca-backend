using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{


    /// <summary>
    /// Classe base de Data Transfer Object para users.
    /// </summary>
    public class UserDTO
    {
        /// <summary>
        /// Nome da pessoa.
        /// </summary>
        [Required]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Morada da pessoa.
        /// </summary>
        [Required]
        public string Morada { get; set; } = string.Empty;

        /// <summary>
        /// E-mail da pessoa. Único.
        /// </summary>
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Contacto da pessoa. Único e tem de incluir indicativo.
        /// </summary>
        [Required, Phone]
        public string Contacto { get; set; } = string.Empty;

        /// <summary>
        /// Código-Postal da pessoa. Este tem que existir no sistema para ser válido.
        /// </summary>
        [Required]
        public string CodigoPostal { get; set; } = string.Empty;
    }
}
