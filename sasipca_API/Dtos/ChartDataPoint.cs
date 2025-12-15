namespace sasipca_API.Dtos
{
    // Usado para gráficos de linhas/barras (Eixo X = Label, Eixo Y = Value)
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty; // Ex: "2023-10-01" ou "Arroz"
        public double Value { get; set; } // Quantidade
        public string? Series { get; set; } // Opcional: "Entrada", "Saída"
    }
}
