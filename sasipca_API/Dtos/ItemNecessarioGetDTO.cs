namespace sasipca_API.Dtos
{

    // Classe para busca de Item Necessário
    public class ItemNecessarioGetDTO : ItemNecessarioDTO
    {
        public int IdItem { get; set; }
        public int Quantidade { get; set; }
        public bool isSelecionado { get; set; }// Indica se o item já foi selecionado
    }
}
