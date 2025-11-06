using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Bid_Go.Tests.Integration.Utils
{
 // Fake Cloudflare R2 service for tests that returns a deterministic URL based on file name
 public class FakeCloudflareR2Service : ICloudflareR2Service
 {
 public Task DeleteImageAsync(string fileName)
 {
 return Task.CompletedTask;
 }

 public Task<string> UploadImageAsync(IFormFile file)
 {
 var name = string.IsNullOrWhiteSpace(file?.FileName) ? "unnamed" : file!.FileName;
 return Task.FromResult($"https://fake.cdn/{name}");
 }
 }
}
