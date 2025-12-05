using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Bid_Go_Backend.Services
{
    /// <summary>
    /// Service that validates status transitions for transport requests and emits notifications.
    /// </summary>
    public class TransportUpdateStatusService : ITransportUpdateStatusService
    {
        private readonly ITransportUpdateStatus _repository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TransportUpdateStatusService> _logger;
        private readonly IAuthorizationRepository _authrepo;

        public TransportUpdateStatusService(
            ITransportUpdateStatus repository,
            INotificationService notificationService,
            ILogger<TransportUpdateStatusService> logger,
            IAuthorizationRepository authrepo)
        {
            _repository = repository;
            _notificationService = notificationService;
            _logger = logger;
            _authrepo = authrepo;
        }

        /// <summary>
        /// Update transport request status based on caller role and current state.
        /// </summary>
        /// <param name="requestId">Transport request identifier.</param>
        /// <param name="user">Caller principal with claims.</param>
        /// <param name="newStatus">Target status.</param>
        /// <returns>Tuple with HTTP-like status code and response body.</returns>
        public async Task<(int StatusCode, object Body)> UpdateRequestStatusAsync(
           int requestId,
           ClaimsPrincipal user,
           ERequestStatus newStatus)
        {
            try
            {
                var userIdClaim = user.FindFirst("userId")?.Value;
                var role = user.FindFirst("userType")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(role))
                    return (401, new { message = "Token inválido." });

                int userId = int.Parse(userIdClaim);

                var request = await _repository.GetTransportRequestWithBidsAsync(requestId);
                if (request == null)
                    return (404, new { message = "Pedido não encontrado." });

                // Access control
                if (role == "Company" && request.CompanyId != userId)
                    return (403, new { message = "Acesso negado." });

                if (role == "Driver")
                {
                    var selectedBid = request.SelectedBid;
                    if (selectedBid == null || selectedBid.DriverId != userId)
                        return (403, new { message = "Acesso negado." });
                }

                bool isValidTransition = ValidateStatusTransition(role, request.Status, newStatus);
                if (!isValidTransition)
                    return (400, new { message = $"Transição inválida de {request.Status} para {newStatus} para o papel {role}." });

                // Update state
                request.Status = newStatus;
                _repository.UpdateTransportRequest(request);

                // Company cancelation -> cancel pending bids and notify drivers
                if (role == "Company" && newStatus == ERequestStatus.Canceled)
                {
                    var pendingBids = request.Bids
                        .Where(b => b.Status == EBidStatus.Pendent)
                        .ToList();

                    foreach (var bid in pendingBids)
                    {
                        bid.Status = EBidStatus.Canceled;
                        await _notificationService.CreateAndSendAsync(
                            bid.DriverId,
                            $"The transport request #{bid.TransportRequestId} associated with your bid has been canceled.",
                            ENotificationType.Canceled,
                            bid.BidId,
                            bid.TransportRequestId
                        );
                    }

                    _repository.UpdateBids(pendingBids);
                }

                await _repository.SaveChangesAsync();

                var response = new TransportRequestResponseDTO
                {
                    Origin = request.Origin,
                    Destination = request.Destination,
                    Package = request.Package,
                    PickupDate = request.PickupDate,
                    DeliveryDate = request.DeliveryDate,
                    Weight = request.Weight,
                    Volume = request.Volume,
                    Length = request.Length,
                    Width = request.Width,
                    Height = request.Height,
                    Image = request.Image,
                    MaxPrice = request.MaxPrice,
                    Status = request.Status
                };

                return (200, new { message = "Estado atualizado com sucesso.", data = response });
            }
            catch (InvalidOperationException ex)
            {
                return (400, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar estado do pedido");
                return (500, new { message = "Erro interno no servidor." });
            }
        }

        private bool ValidateStatusTransition(string role, ERequestStatus current, ERequestStatus target)
        {
            if (role == "Company")
            {
                return current switch
                {
                    ERequestStatus.Active => target is ERequestStatus.Pending or ERequestStatus.Canceled,
                    ERequestStatus.Pending => target == ERequestStatus.WaitingPickup,
                    ERequestStatus.WaitingPickup => target is ERequestStatus.Canceled,
                    ERequestStatus.InTransit => target is ERequestStatus.Canceled,
                    ERequestStatus.Draft => target is ERequestStatus.Canceled,
                    _ => false
                };
            }

            if (role == "Driver")
            {
                return current switch
                {
                    ERequestStatus.WaitingPickup => target == ERequestStatus.InTransit,
                    ERequestStatus.InTransit => target is ERequestStatus.Completed or ERequestStatus.Canceled,
                    _ => false
                };
            }

            return false;
        }
    }
}