namespace ERMSystem.Infrastructure.Services;

public class AiSymptomChatOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Provider name. Supported: "Gemini".
    /// </summary>
    public string Provider { get; set; } = "Gemini";

    /// <summary>
    /// Google Gemini API Key. Obtain from https://aistudio.google.com/app/apikey
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gemini model name. Default: gemini-2.0-flash-lite
    /// </summary>
    public string Model { get; set; } = "gemini-2.0-flash-lite";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum input characters from the user message.
    /// </summary>
    public int MaxInputCharacters { get; set; } = 1200;
}
