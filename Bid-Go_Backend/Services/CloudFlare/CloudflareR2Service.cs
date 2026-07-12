using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;

/// <summary>
/// Cloudflare R2 storage service for uploading and deleting images using MinIO SDK.
/// </summary>
public class CloudflareR2Service : ICloudflareR2Service
{
    private readonly IMinioClient? _minioClient;
    private readonly string _bucketName;
    private readonly string _accountId;

    /// <summary>
    /// True when every R2 setting is present. When storage is not configured the service stays
    /// inert instead of throwing: uploads return an empty URL and deletes are skipped, so the
    /// app still runs (without images) rather than failing to build the services that depend on it.
    /// </summary>
    private readonly bool _enabled;

    public CloudflareR2Service(string accessKey, string secretKey, string accountId, string bucketName)
    {
        _bucketName = bucketName;
        _accountId = accountId;

        _enabled = !string.IsNullOrWhiteSpace(accessKey)
                   && !string.IsNullOrWhiteSpace(secretKey)
                   && !string.IsNullOrWhiteSpace(accountId)
                   && !string.IsNullOrWhiteSpace(bucketName);

        if (!_enabled)
        {
            Console.WriteLine("Cloudflare R2 is not configured (CloudflareR2__* settings missing). Image uploads are disabled.");
            return;
        }

        _minioClient = new MinioClient()
            .WithEndpoint($"{accountId}.r2.cloudflarestorage.com")
            .WithCredentials(accessKey, secretKey)
            .WithSSL()
            .Build();
    }

    /// <summary>
    /// Upload an image file and return a pre-signed URL valid for 7 days.
    /// </summary>
    /// <param name="file">Image file to upload.</param>
    /// <returns>Pre-signed URL for accessing the uploaded image, or an empty string when storage is not configured.</returns>
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (!_enabled || _minioClient is null)
        {
            return string.Empty;
        }

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        using var stream = file.OpenReadStream();

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(file.ContentType);

        await _minioClient.PutObjectAsync(putObjectArgs);

        var urlArgs = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithExpiry(7 * 24 * 60 * 60); // 7 dias

        string url = await _minioClient.PresignedGetObjectAsync(urlArgs);

        return url;
    }

    /// <summary>
    /// Delete an image by file name or URL.
    /// </summary>
    /// <param name="fileName">Name or URL of the image.</param>
    public async Task DeleteImageAsync(string fileName)
    {
        if (!_enabled || _minioClient is null)
        {
            return;
        }

        try
        {
            // Extract file name in case a full URL is provided
            var name = Path.GetFileName(fileName);

            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(name));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao apagar imagem antiga: {ex.Message}");
        }
    }
}
