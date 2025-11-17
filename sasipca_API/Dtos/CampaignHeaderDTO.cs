namespace sasipca_API.Dtos
{
    /// <summary>
    /// Dados resumidos de uma Campanha para listagem.
    /// </summary>
    public class CampaignHeaderDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string CreatorName { get; set; } = null!;
    }
}
