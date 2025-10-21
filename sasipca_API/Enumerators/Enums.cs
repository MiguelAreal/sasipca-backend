    namespace sasipca_API.Enumerators
{
    public class Enums
    {
        /// <summary>
        /// Tipos de unidades de produtos.
        /// </summary>
        public enum UnitTypes
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
        public enum NotificationStatus
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
        public enum DeliveryStatus
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

        public enum MovementTypes
        {
            /// <summary>
            /// ID 1 ->  Entrada - Entrada de stock.
            /// </summary>
            Entrada = 1,

            /// <summary>
            /// ID 2 -> Saida - Saída de stock.
            /// </summary>
            Saida = 2,

            /// <summary>
            /// ID 3 ->  AjusteInventario - Correção de stock.
            /// </summary>
            AjusteInventario = 3,
        }

        // Para reports.
        public enum ReportTypesEnum
        {
            /// <summary>
            /// Report para cabeçalhos de movimentos.
            /// </summary>
            MovementHeaders = 1,
            /// <summary>
            /// Report para detalhes de um movimento específico.
            /// </summary>
            MovementDetails = 2,
            /// <summary>
            /// Report para cabeçalhos de entregas.
            /// </summary>
            DeliveryHeaders = 3
        }

        // Formatos de Saída
        public enum ReportFormat
        {
            PDF = 1,
            CSV = 2
        }

    }
}
