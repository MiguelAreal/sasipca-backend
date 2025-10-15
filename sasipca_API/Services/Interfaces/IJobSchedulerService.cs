using System;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IJobSchedulerService
    {
        void AgendarAtualizacaoEvento(int idEvento, DateTime dataIni);

        Task AtualizarEstadoEvento(int idEvento);

        void AgendarAtualizacaoServico(int idServico, DateTime dataIni);

        Task AtualizarEstadoServico(int idServico);

        void AgendarTerminoServico(int idServico, DateTime dataFim);

        Task FinalizarServico(int idServico);
    }
}
