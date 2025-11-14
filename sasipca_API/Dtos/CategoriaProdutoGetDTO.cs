namespace sasipca_API.Dtos
{
    /// <summary>
    /// Objeto de listas.
    /// </summary>
    public class ListsGetDTO
    {
        /// <summary>
        /// Lista de categorias.
        /// </summary>
        public List<CategoriesGetDTO> Categories { get; set; } = new();

        /// <summary>
        /// Lista de tipos de unidades.
        /// </summary>
        public List<UnitTypesGetDTO> Types { get; set; } = new();
    }

    public class CategoriesGetDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
    }

    public class UnitTypesGetDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
    }
}
