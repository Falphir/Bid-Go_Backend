using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Bids
{
    public class AutomaticSelectionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AutomaticSelectionBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<BidGoDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<IAutomaticSelectionAlgorithmService>();

                var now = DateTime.UtcNow;

                var pendingRequests = await ctx.TransportRequests
                    .Where(tr =>
                        tr.IsAutomaticSelectionEnabled &&
                        !tr.IsAutomaticSelectionExecuted &&
                        tr.BiddingEndDate <= now &&
                        tr.Status == ERequestStatus.Active)
                    .ToListAsync(stoppingToken);

                foreach (var request in pendingRequests)
                {
                    try
                    {
                        var (success, message, selectedBid) =
                            await service.ExecuteAsync(request.TransportRequestId);

                        request.IsAutomaticSelectionExecuted = true;

                        if (!success)
                        {
                            // aqui podes criar uma notificação à empresa a dizer
                            // "não foi possível selecionar automaticamente"
                            // via INotificationService
                        }

                        await ctx.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        // logar erro e talvez não marcar como executado,
                        // para tentar novamente mais tarde
                    }
                }

                // espera 1 minuto
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
