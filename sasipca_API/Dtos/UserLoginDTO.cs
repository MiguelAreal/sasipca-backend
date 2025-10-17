using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para efetuar login.
    /// </summary>
    public class UserLoginDTO
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
