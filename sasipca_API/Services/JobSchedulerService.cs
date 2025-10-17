using Hangfire;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace sasipca_API.Services
{
    /// <summary>
    /// Serviço responsável por agendar a atualização do estado de eventos e serviços com base nas suas datas de início.
    /// </summary>
    public class JobSchedulerService : IJobSchedulerService
    {
        private readonly SasipcaContext _dbcontext;
        private readonly INotificationService _notifService;
        private readonly string _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "JobSchedulerService_logs.txt");

        /// <summary>
        /// Construtor que inicializa o serviço com o contexto da base de dados.
        /// </summary>
        /// <param name="dbcontext">Contexto da base de dados.</param>
        public JobSchedulerService(SasipcaContext dbcontext, INotificationService notifService)
        {
            _dbcontext = dbcontext;
            _notifService = notifService;
        }

    }
}
