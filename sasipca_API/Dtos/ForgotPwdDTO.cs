using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// DTO utilizado no caso de um utilizador se esquecer da sua palavra-passe.
    /// </summary>
    public class EsqueciPwdDTO
    {
        /// <summary>
        /// E-Mail do utilizador ao qual se manda o e-mail de reposição de palavra-passe.
        /// </summary>
        [Required]
        public string Email { get; set; } = null!;
    }

    /// <summary>
    /// DTO utilizador no caso de atribuir uma nova palavra-passe.
    /// </summary>
    public class AtribuirNovaPwdDTO
    {
        /// <summary>
        /// Token necessário para confirmação de utilizador.
        /// </summary>
        [Required]
        public string Token { get; set; } = null!;

        /// <summary>
        /// Nova palavra-passe a aplicar
        /// </summary>
        [Required]
        public string NovaPassword { get; set; } = null!;
    }
}
