using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class ConfigurableHospitalDocumentStorageService : IHospitalDocumentStorageService
{
    private const string LocalProviderType = "Local";
    private static readonly string[] SupportedAccessModes = ["ProxyTicket", "DirectUrl"];

    private readonly HospitalDocumentStorageOptions _options;
    private readonly IDistributedCache _distributedCache;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly Dictionary<string, LocalHospitalDocumentStorageService> _localProviders =
        new(StringComparer.OrdinalIgnoreCase);

    public string Provider => ResolveDefaultProvider().ProviderName;

    public ConfigurableHospitalDocumentStorageService(
        IOptions<HospitalDocumentStorageOptions> options,
        IDistributedCache distributedCache,
        IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        _distributedCache = distributedCache;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<HospitalDocumentStorageWriteResult> StoreEncounterAttachmentAsync(
        Guid encounterId,
        string fileName,
        string? contentType,
        long contentLength,
        Stream content,
        CancellationToken ct = default)
    {
        var provider = ResolveDefaultProvider();
        var backend = GetLocalBackend(provider);
        var result = await backend.StoreEncounterAttachmentAsync(
            encounterId,
            fileName,
            contentType,
            contentLength,
            content,
            ct);

        result.Provider = provider.ProviderName;
        result.StorageUri = BuildStorageUri(provider.ProviderName, result.StorageUri);
        return result;
    }

    public async Task<HospitalDocumentStorageAccessTicket> CreateReadTicketAsync(
        string storageUri,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var reference = ParseStorageReference(storageUri);
        var backend = GetLocalBackend(reference.Provider);
        var ticket = await backend.CreateReadTicketAsync(reference.RelativeStorageUri, fileName, contentType, ct);
        ticket.Provider = reference.Provider.ProviderName;
        return ticket;
    }

    public Task<HospitalDocumentStorageReadResult?> OpenReadAsync(
        string storageUri,
        CancellationToken ct = default)
    {
        var reference = ParseStorageReference(storageUri);
        var backend = GetLocalBackend(reference.Provider);
        return backend.OpenReadAsync(reference.RelativeStorageUri, ct);
    }

    public async Task<HospitalDocumentStorageReadResult?> OpenReadByTicketAsync(
        string accessToken,
        CancellationToken ct = default)
    {
        foreach (var provider in GetEnabledProviders())
        {
            var backend = GetLocalBackend(provider);
            var result = await backend.OpenReadByTicketAsync(accessToken, ct);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public async Task<int> CleanupOrphanedFilesAsync(
        IReadOnlyCollection<string> referencedStorageUris,
        CancellationToken ct = default)
    {
        var groupedReferences = referencedStorageUris
            .Select(ParseStorageReference)
            .GroupBy(x => x.Provider.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.Select(x => x.RelativeStorageUri).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var deletedCount = 0;
        foreach (var provider in GetEnabledProviders())
        {
            var backend = GetLocalBackend(provider);
            groupedReferences.TryGetValue(provider.ProviderName, out var providerReferences);
            deletedCount += await backend.CleanupOrphanedFilesAsync(providerReferences ?? Array.Empty<string>(), ct);
        }

        return deletedCount;
    }

    private IReadOnlyCollection<ResolvedDocumentStorageProvider> GetEnabledProviders()
    {
        if (_options.Providers.Count == 0)
        {
            throw new InvalidOperationException("DocumentStorage:Providers chua duoc cau hinh.");
        }

        var providers = _options.Providers
            .Where(entry => entry.Value.Enabled)
            .Select(entry => ResolveProvider(entry.Key))
            .ToArray();

        if (providers.Length == 0)
        {
            throw new InvalidOperationException("Khong co document storage provider nao dang duoc bat.");
        }

        return providers;
    }

    private ResolvedDocumentStorageProvider ResolveDefaultProvider()
    {
        var providerName = string.IsNullOrWhiteSpace(_options.DefaultProvider)
            ? _options.Providers.Keys.FirstOrDefault()
            : _options.DefaultProvider.Trim();

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("DocumentStorage:DefaultProvider chua duoc cau hinh.");
        }

        return ResolveProvider(providerName);
    }

    private ResolvedDocumentStorageProvider ResolveProvider(string providerName)
    {
        providerName = NormalizeProviderName(providerName);
        if (!_options.Providers.TryGetValue(providerName, out var providerOptions))
        {
            throw new InvalidOperationException($"Khong tim thay document storage provider '{providerName}'.");
        }

        if (!providerOptions.Enabled)
        {
            throw new InvalidOperationException($"Document storage provider '{providerName}' hien dang tat.");
        }

        if (!string.Equals(providerOptions.Type, LocalProviderType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Document storage provider '{providerName}' co type '{providerOptions.Type}' chua duoc ho tro.");
        }

        ValidateProviderConfiguration(providerName, providerOptions);

        return new ResolvedDocumentStorageProvider(providerName, providerOptions);
    }

    private static void ValidateProviderConfiguration(
        string providerName,
        HospitalDocumentStorageProviderOptions providerOptions)
    {
        if (string.IsNullOrWhiteSpace(providerOptions.Type))
        {
            throw new InvalidOperationException($"Document storage provider '{providerName}' chua cau hinh Type.");
        }

        var accessMode = string.IsNullOrWhiteSpace(providerOptions.AccessMode)
            ? "ProxyTicket"
            : providerOptions.AccessMode.Trim();
        if (!SupportedAccessModes.Any(mode => string.Equals(mode, accessMode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Document storage provider '{providerName}' co AccessMode '{providerOptions.AccessMode}' khong duoc ho tro.");
        }

        if (string.Equals(accessMode, "DirectUrl", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(providerOptions.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                $"Document storage provider '{providerName}' dung DirectUrl nhung chua cau hinh PublicBaseUrl.");
        }

        if (!string.IsNullOrWhiteSpace(providerOptions.PublicBaseUrl) &&
            (!Uri.TryCreate(providerOptions.PublicBaseUrl.Trim(), UriKind.Absolute, out var publicBaseUri) ||
             publicBaseUri.Scheme is not ("http" or "https")))
        {
            throw new InvalidOperationException(
                $"Document storage provider '{providerName}' co PublicBaseUrl khong hop le.");
        }

        if (string.IsNullOrWhiteSpace(providerOptions.RootPath))
        {
            throw new InvalidOperationException($"Document storage provider '{providerName}' chua cau hinh RootPath.");
        }

        if (providerOptions.MaxFileSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Document storage provider '{providerName}' phai cau hinh MaxFileSizeBytes lon hon 0.");
        }

        if (providerOptions.DownloadTicketExpiryMinutes <= 0)
        {
            throw new InvalidOperationException(
                $"Document storage provider '{providerName}' phai cau hinh DownloadTicketExpiryMinutes lon hon 0.");
        }

        if (providerOptions.AllowedExtensions.Length == 0 ||
            providerOptions.AllowedExtensions.Any(extension =>
                string.IsNullOrWhiteSpace(extension) || !extension.Trim().StartsWith(".", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Document storage provider '{providerName}' phai cau hinh AllowedExtensions hop le.");
        }
    }

    private LocalHospitalDocumentStorageService GetLocalBackend(ResolvedDocumentStorageProvider provider)
    {
        if (_localProviders.TryGetValue(provider.ProviderName, out var existing))
        {
            return existing;
        }

        var localOptions = new LocalDocumentStorageOptions
        {
            Provider = provider.ProviderName,
            AccessMode = provider.Options.AccessMode,
            PublicBaseUrl = provider.Options.PublicBaseUrl,
            RootPath = provider.Options.RootPath,
            DownloadTicketExpiryMinutes = provider.Options.DownloadTicketExpiryMinutes,
            CleanupIntervalHours = provider.Options.CleanupIntervalHours,
            OrphanFileRetentionDays = provider.Options.OrphanFileRetentionDays,
            MaxFileSizeBytes = provider.Options.MaxFileSizeBytes,
            AllowedExtensions = provider.Options.AllowedExtensions
        };

        var backend = new LocalHospitalDocumentStorageService(
            Options.Create(localOptions),
            _distributedCache,
            _hostEnvironment);
        _localProviders[provider.ProviderName] = backend;
        return backend;
    }

    private ParsedStorageReference ParseStorageReference(string storageUri)
    {
        var normalized = storageUri?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Storage uri cua tai lieu khong hop le.");
        }

        foreach (var providerName in _options.Providers.Keys)
        {
            var prefix = providerName + "://";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var provider = ResolveProvider(providerName);
                return new ParsedStorageReference(
                    provider,
                    normalized[prefix.Length..].TrimStart('/'));
            }
        }

        return new ParsedStorageReference(ResolveDefaultProvider(), normalized);
    }

    private static string BuildStorageUri(string providerName, string relativeStorageUri)
        => $"{providerName}://{relativeStorageUri.TrimStart('/')}";

    private static string NormalizeProviderName(string providerName)
    {
        var normalized = providerName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Contains("://", StringComparison.Ordinal) ||
            normalized.Contains('/') ||
            normalized.Contains('\\'))
        {
            throw new InvalidOperationException("Ten document storage provider khong hop le.");
        }

        return normalized;
    }

    private sealed record ResolvedDocumentStorageProvider(
        string ProviderName,
        HospitalDocumentStorageProviderOptions Options);

    private sealed record ParsedStorageReference(
        ResolvedDocumentStorageProvider Provider,
        string RelativeStorageUri);
}
