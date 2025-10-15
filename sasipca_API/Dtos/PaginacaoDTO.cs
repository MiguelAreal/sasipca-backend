namespace sasipca_API.Dtos
{
    public class PaginacaoDTO<T>
    {
        public List<T> Itens { get; set; }
        public int PaginaAtual { get; set; }
        public int ItensPorPagina { get; set; }
        public int TotalItens { get; set; }
        public int TotalPaginas { get; set; }
    }
}
