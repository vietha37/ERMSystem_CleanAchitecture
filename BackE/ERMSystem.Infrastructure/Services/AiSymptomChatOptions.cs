namespace ERMSystem.Infrastructure.Services;

public class AiSymptomChatOptions
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Ollama";
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string GeneratePath { get; set; } = "/api/generate";
    public string Model { get; set; } = "llama3.1:8b";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxInputCharacters { get; set; } = 1200;
}
