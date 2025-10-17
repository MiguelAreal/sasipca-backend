using Microsoft.EntityFrameworkCore;
using sasipca_API.Hubs;
using sasipca_API.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using sasipca_API.Dtos;
using sasipca_API.Services.Interfaces;
using sasipca_API.DBModels;
using sasipca_API.Enumerators;

namespace sasipca_API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly SasipcaContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(SasipcaContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

    }
}
