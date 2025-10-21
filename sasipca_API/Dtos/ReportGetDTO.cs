namespace sasipca_API.Dtos
{
    public class ReportGetDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!; // Nome do arquivo original
        public string CreatorName { get; set; } = null!;
        public int ReportTypeId { get; set; }
        public string ReportTypeName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
