namespace ERMSystem.Infrastructure.Services;

public class HospitalDocumentStorageOptions
{
    public string DefaultProvider { get; set; } = "LocalStorage";
    public Dictionary<string, HospitalDocumentStorageProviderOptions> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public class HospitalDocumentStorageProviderOptions
{
    public string Type { get; set; } = "Local";
    public bool Enabled { get; set; } = true;
    public string AccessMode { get; set; } = "ProxyTicket";
    public string? PublicBaseUrl { get; set; }
    public bool RequireSignedDirectUrls { get; set; } = false;
    public string DirectUrlSigningSecret { get; set; } = string.Empty;
    public string RootPath { get; set; } = "App_Data/ObjectStorage";
    public int DownloadTicketExpiryMinutes { get; set; } = 5;
    public int CleanupIntervalHours { get; set; } = 24;
    public int OrphanFileRetentionDays { get; set; } = 7;
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".txt",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx"
    ];
}
