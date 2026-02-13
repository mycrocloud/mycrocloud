using Amazon.S3;
using Amazon.S3.Model;

namespace WebApp.Gateway.Services;

public class S3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageProvider(IAmazonS3 s3Client, string bucketName)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }

    public async Task SaveAsync(string path, Stream stream, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = NormalizePath(path),
            InputStream = stream,
            AutoCloseStream = false,
            DisablePayloadSigning = true, // TODO: temporary workaround for R2 compatibility
            DisableDefaultChecksumValidation = true // TODO: temporary workaround for R2 compatibility
        };

        await _s3Client.PutObjectAsync(request, ct);
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = NormalizePath(path)
        };

        var response = await _s3Client.GetObjectAsync(request, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = NormalizePath(path)
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucketName, NormalizePath(path), ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public string GetFullPath(string path)
    {
        // For S3, the unique identifier is the Bucket + Key
        return $"s3://{_bucketName}/{NormalizePath(path)}";
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
