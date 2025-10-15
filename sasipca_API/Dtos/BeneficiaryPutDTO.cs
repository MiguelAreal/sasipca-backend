using sasipca_API.DBModels;
using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para atualizar os dados de perfis de beneficiários.
    /// Tem dados para atualizar também dados de morada.
    /// </summary>
    public class BeneficiaryPutDTO
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

        [Required]
        public string Course { get; set; }

        [Required]
        public int CurricularYear { get; set; }

        public string GlobalObs { get; set; }

        public string ParticularObs { get; set; }   

        // Para atualização de morada.
        [MaxLength(255)]
        public string Street { get; set; }

        public int Number { get; set; }

        [MaxLength(9)]
        public string PostalCode { get; set; }

    }

}
