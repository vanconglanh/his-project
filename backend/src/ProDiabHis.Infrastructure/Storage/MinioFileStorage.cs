using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Storage;

/// <summary>IFileStorage implementation dung MinIO SDK</summary>
public class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly IMinioClient _publicClient;
    private readonly ILogger<MinioFileStorage> _logger;

    public MinioFileStorage(
        IMinioClient client,
        [FromKeyedServices("public")] IMinioClient publicClient,
        ILogger<MinioFileStorage> logger)
    {
        _client = client;
        _publicClient = publicClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        string bucket,
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(bucket, cancellationToken);

        var putArgs = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(stream.CanSeek ? stream.Length : -1)
            .WithContentType(contentType);

        await _client.PutObjectAsync(putArgs, cancellationToken);
        _logger.LogInformation("Uploaded {ObjectKey} to bucket {Bucket}", objectKey, bucket);
        return objectKey;
    }

    public async Task<string> GetSignedUrlAsync(
        string bucket,
        string objectKey,
        int ttlSeconds = 900,
        CancellationToken cancellationToken = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithExpiry(ttlSeconds);

        // Dung client "public" (endpoint truy cap duoc tu ben ngoai docker network) de sinh URL
        // tra ve cho FE/trinh duyet nguoi dung. _client (endpoint noi bo, vd "minio:9000") van
        // dung cho cac thao tac server-to-server (Upload/Download/Delete/EnsureBucket) o tren.
        return await _publicClient.PresignedGetObjectAsync(args);
    }

    public async Task DeleteAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var removeArgs = new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey);

        await _client.RemoveObjectAsync(removeArgs, cancellationToken);
        _logger.LogInformation("Deleted {ObjectKey} from bucket {Bucket}", objectKey, bucket);
    }

    public async Task<Stream> DownloadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var ms = new MemoryStream();
        var getArgs = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(ms));

        await _client.GetObjectAsync(getArgs, cancellationToken);
        ms.Position = 0;
        _logger.LogInformation("Downloaded {ObjectKey} from bucket {Bucket}", objectKey, bucket);
        return ms;
    }

    public async Task EnsureBucketExistsAsync(
        string bucket,
        CancellationToken cancellationToken = default)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(bucket);
        var exists = await _client.BucketExistsAsync(existsArgs, cancellationToken);
        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(bucket);
            await _client.MakeBucketAsync(makeArgs, cancellationToken);
            _logger.LogInformation("Created MinIO bucket: {Bucket}", bucket);
        }
    }
}
