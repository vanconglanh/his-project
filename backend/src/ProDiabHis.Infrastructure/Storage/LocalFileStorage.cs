using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Storage;

/// <summary>
/// IFileStorage fallback dung o dev khi khong co MinIO (docker khong san sang tren may dev).
/// Luu file xuong dia local theo cau truc {BasePath}/{bucket}/{objectKey}.
/// Bat qua config "Storage:Provider" = "Local" trong appsettings (mac dinh van la MinIO o production).
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
    {
        _basePath = configuration["Storage:LocalPath"] ?? Path.Combine(AppContext.BaseDirectory, "local-storage");
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    private string ResolvePath(string bucket, string objectKey)
        => Path.Combine(_basePath, bucket, objectKey.Replace('/', Path.DirectorySeparatorChar));

    public async Task<string> UploadAsync(string bucket, string objectKey, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(bucket, objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        if (stream.CanSeek) stream.Position = 0;
        await stream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Da luu file {ObjectKey} vao bucket local {Bucket}", objectKey, bucket);
        return objectKey;
    }

    public Task<string> GetSignedUrlAsync(string bucket, string objectKey, int ttlSeconds = 900, CancellationToken cancellationToken = default)
    {
        // Khong co server tinh phuc vu file local trong pham vi fix nay -> tra ve duong dan tham chieu
        var fullPath = ResolvePath(bucket, objectKey);
        return Task.FromResult($"local-storage://{bucket}/{objectKey}?path={Uri.EscapeDataString(fullPath)}");
    }

    public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(bucket, objectKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        _logger.LogInformation("Da xoa file {ObjectKey} khoi bucket local {Bucket}", objectKey, bucket);
        return Task.CompletedTask;
    }

    public Task EnsureBucketExistsAsync(string bucket, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.Combine(_basePath, bucket));
        return Task.CompletedTask;
    }

    public async Task<Stream> DownloadAsync(string bucket, string objectKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(bucket, objectKey);
        var ms = new MemoryStream();
        await using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
        {
            await fileStream.CopyToAsync(ms, cancellationToken);
        }
        ms.Position = 0;
        return ms;
    }
}
