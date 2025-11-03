using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface ITransportUpdateStatus
    {

        Task<TransportRequestResponseDTO?> UpdateRequestStatusAsync(int id, int companyID, ERequestStatus newStatus);
    }
}
