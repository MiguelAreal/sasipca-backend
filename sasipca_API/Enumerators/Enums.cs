    namespace sasipca_API.Enumerators
{
    public class Enums
    {

        /// <summary>
        /// Tipos de utilizadores.
        /// </summary>
        public enum TipoUser
        {
            /// <summary>
            /// ID 1 -> Admin - Utilizador com perfil de administrador, pode fazer tudo dentro da app.
            /// </summary>
            Admin = 1,

            /// <summary>
            /// ID 2 -> Beneficiário - Utilizador com perfil de beneficiário, não pode entrar na app.
            /// </summary>
            Beneficiario = 2
        }

        /// <summary>
        /// Tipos de unidades de produtos.
        /// </summary>
        public enum TipoUnidade
        {
            /// <summary>
            /// ID 1 -> Unidade
            /// </summary>
            Unidade = 1,

            /// <summary>
            /// ID 2 -> Kg - Kilo.
            /// </summary>
            Kg = 2,

            /// <summary>
            /// ID 3 -> L - Litros.
            /// </summary>
            L = 3,
        }

        /// <summary>
        /// Estados de Notificações.
        /// </summary>
        public enum EstadoNotificacoes
        {
            /// <summary>
            /// ID 1 -> NaoLida - Notificação foi criada, mas ainda não foi marcada como lida.
            /// </summary>
            NaoLida = 1,

            /// <summary>
            /// ID 2 -> Lida - A notificação foi marcada como Lida pelo utilizador.
            /// </summary>
            Lida = 2,

            /// <summary>
            /// ID 3 -> Arquivada - A notificação foi 'eliminada' pelo utilizador.
            /// </summary>
            Arquivada = 3
        }

        /// <summary>
        /// Estados de entregas planeadas de stock a beneficiário.
        /// </summary>
        public enum EstadoEntrega
        {
            /// <summary>
            /// ID 1 ->  Agendado - Entrega foi criada e de momento está agendada.
            /// </summary>
            Agendada = 1,

            /// <summary>
            /// ID 2 -> Entregue - A entrega foi realizada e marcada como terminada.
            /// </summary>
            Entregue = 2,

            /// <summary>
            /// ID 3 ->  Cancelada - A entrega não foi realizada e marcada como terminada.
            /// </summary>
            Cancelada = 3,
        }







        // Daqui para baixo só existe para não dar erros.

        /// <summary>
        /// Estados que um serviço pode ter.
        /// </summary>
        public enum EstadoServico
        {
            /// <summary>
            /// ID 1 -> Criado - Anúncio de serviço criado e publicado.
            /// </summary>
            Criado = 1,

            /// <summary>
            /// ID 2 -> Cancelado - O utilizador criador cancelou o serviço. Só pode ser cancelado se ainda não tiver começado.
            /// </summary>
            Cancelado = 2,

            /// <summary>
            /// ID 3 -> Não Cumpre Requisitos Mínimos - O serviço não constatou executor a tempo da data de início. 
            /// </summary>
            NaoCumpreRequisitosMinimos = 3,

            /// <summary>
            /// ID 4 -> Aceite - O criador selecionou um prestador para realizar um serviço.
            /// </summary>
            Aceite = 4,

            /// <summary>
            /// ID 5 -> A decorrer - O serviço está a decorrer no momento, passou a hora de início.
            /// </summary>
            ADecorrer = 5,

            /// <summary>
            /// ID 6 -> Terminado - O serviço foi terminado pelo utilizador criador manualmente, ou quando atinge DataFim (se houver).
            /// </summary>
            Terminado = 6,

            /// <summary>
            /// ID 7 -> O prestador realizou o serviço. Apenas pode acontecer se tiver avaliação.
            /// </summary>
            Concluido = 7
        }

        /// <summary>
        /// Estados que um produto pode ter.
        /// </summary>
        public enum EstadoProduto
        {
            /// <summary>
            /// ID 1 -> Criado - Anúncio de produto criado e publicado.
            /// </summary>
            Criado = 1,

            /// <summary>
            /// ID 2 -> Cancelado - Anúncio cancelado. Só pode ser cancelado se ainda não tiver sido vendido.
            /// </summary>
            Cancelado = 2,

            /// <summary>
            /// ID 3 -> A transação foi concluída.
            /// </summary>
            Vendido = 3,
        }

        /// <summary>
        /// Estados que um evento pode ter.
        /// </summary>
        public enum EstadoEvento
        {
            /// <summary>
            /// ID 1 -> Criado - Anúncio de evento criado e publicado.
            /// </summary>
            Criado = 1,

            /// <summary>
            /// ID 2 -> Cancelado - O utilizador criador cancelou o evento. Só pode ser cancelado se ainda não estiver A decorrer ou concluído.
            /// </summary>
            Cancelado = 2,

            /// <summary>
            /// ID 3 -> O evento não cumpre os requisitos para ser realizado. Estes são o número mínimo de pessoas inscritas e os itens necessários selecionados.
            /// </summary>
            NaoCumpreRequisitosMinimos = 3,

            /// <summary>
            /// ID 4 ->  O evento está a decorrer no momento, passou a hora de início.
            /// </summary>
            ADecorrer = 4,

            /// <summary>
            /// ID 5 -> O evento foi dado como concluído pelo utilizador criador.
            /// </summary>
            Concluido = 5
        }

        /// <summary>
        /// Estados que uma proposta a um produto ou serviço podem ter.
        /// </summary>
        public enum EstadoProposta
        {
            /// <summary>
            /// ID 1 ->  A proposta foi criada para um serviço ou produto.
            /// </summary>
            Criada = 1,
            /// <summary>
            /// ID 2 ->  A proposta está a aguardar confirmação ou negação.
            /// </summary>
            Standby = 2,

            /// <summary>
            /// ID 3 ->  A proposta foi aceite.
            /// </summary>
            Aceite = 3,

            /// <summary>
            /// ID 3 ->  A proposta foi negada.
            /// </summary>
            Negada = 4,
        }




    }
}
