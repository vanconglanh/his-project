namespace ProDiabHis.Application.Common;

/// <summary>Luu tru file tren object storage (MinIO)</summary>
public interface IFileStorage
{
    /// <summary>Upload file len bucket, tra ve object key</summary>
    Task<string> UploadAsync(
        string bucket,
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Tao signed URL tam thoi (TTL tinh bang giay)</summary>
    Task<string> GetSignedUrlAsync(
        string bucket,
        string objectKey,
        int ttlSeconds = 900,
        CancellationToken cancellationToken = default);

    /// <summary>Xoa object khoi bucket</summary>
    Task DeleteAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>Dam bao bucket ton tai, tao neu chua co</summary>
    Task EnsureBucketExistsAsync(
        string bucket,
        CancellationToken cancellationToken = default);

    /// <summary>Download object tu bucket, tra ve Stream</summary>
    Task<Stream> DownloadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default);
}

/// <summary>Ten cac MinIO bucket</summary>
public static class FileBuckets
{
    public const string Avatars = "avatars";
    public const string ClsUploads = "cls-uploads";
    public const string FilesGeneric = "files-generic";
}
