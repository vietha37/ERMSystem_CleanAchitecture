namespace ERMSystem.Infrastructure.Services
{
    public class TwoFactorAuthOptions
    {
        public string Issuer { get; set; } = "ERM Hospital";
        public int ChallengeExpiryMinutes { get; set; } = 5;
        public int SetupExpiryMinutes { get; set; } = 10;
        public int TimeStepSeconds { get; set; } = 30;
        public int Digits { get; set; } = 6;
        public int AllowedDriftWindows { get; set; } = 1;
    }
}
