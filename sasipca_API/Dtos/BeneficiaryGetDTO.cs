using sasipca_API.DBModels;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para ir buscar beneficiários. O ID também é apresentado.
    /// </summary>
    public class BeneficiaryGetDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public string Course { get; set; }
        public int? CurricularYear { get; set; }
        public int? StudentNum { get; set; }
        public int? Nif { get; set; }
    }
}
