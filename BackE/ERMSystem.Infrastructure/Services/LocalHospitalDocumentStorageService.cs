using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class LocalHospitalDocumentStorageService : IHospitalDocumentStorageService
{
    private static readonly Regex UnsafeFileNameCharsRegex = new(@"[^a-zA-Z0-9._-]+", RegexOptions.Compiled);

    private readonly LocalDocumentStorageOptions _options;
    private readonly string _rootPath;
    private readonly IDistributedCache _distributedCache;

    public string Provider => string.IsNullOrWhiteSpace(_options.Provider) ? "local" : _options.Provider.Trim();

    public LocalHospitalDocumentStorageService(
        IOptions<LocalDocumentStorageOptions> options,
        IDistributedCache distributedCache,
        IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        _distributedCache = distributedCache;
        var configuredRoot = string.IsNullOrWhiteSpace(_options.RootPath)
            ? "App_Data/ObjectStorage"
            : _options.RootPath.Trim();

        _rootPath = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(hostEnvironment.ContentRootPath, configuredRoot);
    }

    public async Task<HospitalDocumentStorageWriteResult> StoreEncounterAttachmentAsync(
        Guid encounterId,
        string fileName,
        string? contentType,
        long contentLength,
        Stream content,
        CancellationToken ct = default)
    {
        if (contentLength <= 0)
        {
            throw new InvalidOperationException("Tep tai len khong hop le hoac khong co du lieu.");
        }

        if (contentLength > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"Tep tai lieu vuot qua gioi han {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var normalizedFileName = NormalizeFileName(fileName);
        var extension = Path.GetExtension(normalizedFileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Dinh dang tep khong duoc ho tro cho tai lieu y te.");
        }

        var nowUtc = DateTime.UtcNow;
        var storedFileName = $"{Guid.NewGuid():N}_{normalizedFileName}";
        var relativeDirectory = Path.Combine(
            "encounter-attachments",
            encounterId.ToString("N"),
            nowUtc.ToString("yyyy", CultureInfo.InvariantCulture),
            nowUtc.ToString("MM", CultureInfo.InvariantCulture));
        var absoluteDirectory = Path.Combine(_rootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var absoluteFilePath = Path.Combine(absoluteDirectory, storedFileName);
        await using (var target = new FileStream(
                         absoluteFilePath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         81920,
                         useAsync: true))
        {
            await content.CopyToAsync(target, ct);
        }

        var storageUri = Path.Combine(relativeDirectory, storedFileName)
            .Replace('\\', '/');

        return new HospitalDocumentStorageWriteResult
        {
            Provider = Provider,
            StorageUri = storageUri,
            FileName = normalizedFileName,
            ContentType = NormalizeContentType(contentType),
            ContentLength = contentLength
        };
    }

    public async Task<HospitalDocumentStorageAccessTicket> CreateReadTicketAsync(
        string storageUri,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var normalizedStorageUri = NormalizeStorageUri(storageUri);
        var accessToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.DownloadTicketExpiryMinutes));
        var payload = string.Join('|', normalizedStorageUri, fileName.Trim(), NormalizeContentType(contentType), expiresAtUtc.ToString("O", CultureInfo.InvariantCulture));

        await _distributedCache.SetStringAsync(
            BuildTicketKey(accessToken),
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiresAtUtc
            },
            ct);

        return new HospitalDocumentStorageAccessTicket
        {
            Provider = Provider,
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public Task<HospitalDocumentStorageReadResult?> OpenReadAsync(
        string storageUri,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedStorageUri = NormalizeStorageUri(storageUri);
        var absolutePath = Path.GetFullPath(Path.Combine(_rootPath, normalizedStorageUri.Replace('/', Path.DirectorySeparatorChar)));
        var rootFullPath = Path.GetFullPath(_rootPath);

        if (!absolutePath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Duong dan tai lieu khong hop le.");
        }

        if (!File.Exists(absolutePath))
        {
            return Task.FromResult<HospitalDocumentStorageReadResult?>(null);
        }

        var fileName = ExtractOriginalFileName(Path.GetFileName(absolutePath));
        var stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        HospitalDocumentStorageReadResult result = new()
        {
            FileName = fileName,
            ContentType = InferContentType(fileName),
            Content = stream,
            ContentLength = stream.Length
        };

        return Task.FromResult<HospitalDocumentStorageReadResult?>(result);
    }

    public async Task<HospitalDocumentStorageReadResult?> OpenReadByTicketAsync(
        string accessToken,
        CancellationToken ct = default)
    {
        var rawPayload = await _distributedCache.GetStringAsync(BuildTicketKey(accessToken.Trim()), ct);
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return null;
        }

        var parts = rawPayload.Split('|', 4, StringSplitOptions.None);
        if (parts.Length != 4)
        {
            return null;
        }

        var result = await OpenReadAsync(parts[0], ct);
        if (result == null)
        {
            return null;
        }

        result.FileName = string.IsNullOrWhiteSpace(parts[1]) ? result.FileName : parts[1];
        result.ContentType = string.IsNullOrWhiteSpace(parts[2]) ? result.ContentType : parts[2];
        return result;
    }

    public Task<int> CleanupOrphanedFilesAsync(
        IReadOnlyCollection<string> referencedStorageUris,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!Directory.Exists(_rootPath))
        {
            return Task.FromResult(0);
        }

        var retentionThresholdUtc = DateTime.UtcNow.AddDays(-Math.Max(1, _options.OrphanFileRetentionDays));
        var referenced = new HashSet<string>(
            referencedStorageUris.Select(NormalizeStorageUri),
            StringComparer.OrdinalIgnoreCase);

        var deletedCount = 0;
        foreach (var filePath in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var relativeUri = Path.GetRelativePath(_rootPath, filePath).Replace('\\', '/');
            if (referenced.Contains(relativeUri))
            {
                continue;
            }

            var lastWriteUtc = File.GetLastWriteTimeUtc(filePath);
            if (lastWriteUtc > retentionThresholdUtc)
            {
                continue;
            }

            File.Delete(filePath);
            deletedCount += 1;
        }

        foreach (var directory in Directory.EnumerateDirectories(_rootPath, "*", SearchOption.AllDirectories)
                     .OrderByDescending(x => x.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory, false);
            }
        }

        return Task.FromResult(deletedCount);
    }

    private static string NormalizeFileName(string fileName)
    {
        var rawName = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rawName))
        {
            throw new InvalidOperationException("Ten tep tai lieu khong hop le.");
        }

        var safeName = UnsafeFileNameCharsRegex.Replace(rawName, "_");
        return safeName.Length > 200 ? safeName[^200..] : safeName;
    }

    private static string NormalizeStorageUri(string storageUri)
    {
        var normalized = storageUri?.Trim().Replace('\\', '/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage key cua tai lieu khong hop le.");
        }

        return normalized.TrimStart('/');
    }

    private static string NormalizeContentType(string? contentType)
        => string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();

    private static string BuildTicketKey(string accessToken)
        => $"document-storage:ticket:{accessToken}";

    private static string ExtractOriginalFileName(string storedFileName)
    {
        var underscoreIndex = storedFileName.IndexOf('_');
        return underscoreIndex > -1 && underscoreIndex < storedFileName.Length - 1
            ? storedFileName[(underscoreIndex + 1)..]
            : storedFileName;
    }

    private static string InferContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
