using Bid_Go_Backend.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IAuthService
    {
        string GeneratePasswordResetToken();
        string GenerateJwtToken(User user);
    }

}
