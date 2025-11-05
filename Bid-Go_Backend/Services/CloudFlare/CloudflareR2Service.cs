using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;

public class CloudflareR2Service : ICloudflareR2Service
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _accountId;

    public CloudflareR2Service(string accessKey, string secretKey, string accountId, string bucketName)
    {
        _bucketName = bucketName;
        _accountId = accountId;

        _minioClient = new MinioClient()
            .WithEndpoint($"{accountId}.r2.cloudflarestorage.com")
            .WithCredentials(accessKey, secretKey)
            .WithSSL()
            .Build();
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
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


    public async Task DeleteImageAsync(string fileName)
    {
        try
        {
            // extraí apenas o nome do ficheiro, caso tenhas URL completa
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
