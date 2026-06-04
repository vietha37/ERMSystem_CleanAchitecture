namespace ERMSystem.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "ermsystem.events";
    public string NotificationQueue { get; set; } = "ermsystem.notification.events";
    public string NotificationRoutingPattern { get; set; } = "#";
}
