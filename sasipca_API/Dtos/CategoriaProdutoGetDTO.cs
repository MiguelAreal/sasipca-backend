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
        public List<CategoryTypes> Categories { get; set; } = new();

        /// <summary>
        /// Lista de tipos de unidades.
        /// </summary>
        public List<UnitTypes> Units { get; set; } = new();

        /// <summary>
        /// Lista de tipos de movimentos.
        /// </summary>
        public List<MovementTypes> Movements { get; set; } = new();

        /// <summary>
        /// Lista de tipos de Entregas.
        /// </summary>
        public List<DeliveriesStatus> Deliveries { get; set; } = new();

        /// <summary>
        /// Lista de tipos de relatórios.
        /// </summary>
        public List<ReportTypes> Reports { get; set; } = new();
    }

    public class CategoryTypes
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
    }

    public class UnitTypes
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
    }

    public class MovementTypes
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
    }

    public class DeliveriesStatus
    {
        public int Id { get; set; }
        public string Status { get; set; } = null!;
    }

    public class ReportTypes
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
    }

}
