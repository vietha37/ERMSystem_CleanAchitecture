using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalDocumentStorageService
{
    string Provider { get; }

    Task<HospitalDocumentStorageWriteResult> StoreEncounterAttachmentAsync(
        Guid encounterId,
        string fileName,
        string? contentType,
        long contentLength,
        Stream content,
        CancellationToken ct = default);

    Task<HospitalDocumentStorageAccessTicket> CreateReadTicketAsync(
        string storageUri,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    Task<HospitalDocumentStorageReadResult?> OpenReadAsync(
        string storageUri,
        CancellationToken ct = default);

    Task<HospitalDocumentStorageReadResult?> OpenReadByTicketAsync(
        string accessToken,
        long? expiresUnixSeconds = null,
        string? signature = null,
        CancellationToken ct = default);

    Task<int> CleanupOrphanedFilesAsync(
        IReadOnlyCollection<string> referencedStorageUris,
        CancellationToken ct = default);
}

public class HospitalDocumentStorageWriteResult
{
    public string Provider { get; set; } = string.Empty;
    public string StorageUri { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long ContentLength { get; set; }
}

public class HospitalDocumentStorageAccessTicket
{
    public string Provider { get; set; } = string.Empty;
    public string AccessMode { get; set; } = "ProxyTicket";
    public string AccessToken { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class HospitalDocumentStorageReadResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public Stream Content { get; set; } = Stream.Null;
    public long? ContentLength { get; set; }
}
