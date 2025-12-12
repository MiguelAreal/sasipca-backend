namespace sasipca_API.Dtos
{
    // DTO para listar
    public class AdminListDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; } = null!;
        public string Contact { get; set; } = null!;
    }
}
