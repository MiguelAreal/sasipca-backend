using sasipca_API.DBModels;
using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para registar novos perfis de beneficiários.
    /// Tem dados para criar também dados de morada.
    /// </summary>
    public class BeneficiaryPostDTO
    {

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(9)]
        [Phone]
        public string Contact { get; set; }

        public string? Course { get; set; }

        public int? CurricularYear { get; set; }
        public int? StudentNum { get; set; }
        public int? Nif { get;set;  }
        public string? GlobalObs { get; set; }
        public string? ParticularObs { get; set; }   

        // Para criação de morada.
        [MaxLength(255)]
        public string? Street { get; set; }
        public int? Number { get; set; }

        [MaxLength(9)]
        public string? PostalCode { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool anyFilled = !string.IsNullOrWhiteSpace(Street) || Number.HasValue || !string.IsNullOrWhiteSpace(PostalCode);
            bool allFilled = !string.IsNullOrWhiteSpace(Street) && Number.HasValue && !string.IsNullOrWhiteSpace(PostalCode);

            if (anyFilled && !allFilled)
            {
                yield return new ValidationResult(
                    "Se algum campo de endereço estiver preenchido, todos os campos (Rua, Número, Código Postal) devem estar preenchidos.",
                    new[] { nameof(Street), nameof(Number), nameof(PostalCode) }
                );
            }
        }

    }

}
