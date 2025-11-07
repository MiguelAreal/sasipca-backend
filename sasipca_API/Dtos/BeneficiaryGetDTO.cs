using sasipca_API.DBModels;
using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Utilizado para ir buscar beneficiários. Inclui também o endereço completo.
    /// </summary>
    public class BeneficiaryGetDTO
    {
        public int BeneficiaryId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Contact { get; set; } = null!;
        public string? Course { get; set; }
        public int? CurricularYear { get; set; }
        public int? StudentNum { get; set; }
        public int? Nif { get; set; }
        public string? GlobalObs { get; set; }
        public string? ParticularObs { get; set; }

        // Endereço
        public string? Street { get; set; }
        public int? Number { get; set; }
        public string? PostalCode { get; set; }
    }
}
