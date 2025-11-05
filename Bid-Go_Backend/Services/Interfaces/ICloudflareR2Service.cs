using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface ICloudflareR2Service
    {
        Task<string> UploadImageAsync(IFormFile file);

        Task DeleteImageAsync(string fileName);
    }
}
