namespace Electra.Core.Ai;

public class UserSettings
{
    public int UserId { get; set; }
    public AiProvider PreferredProvider { get; set; } = AiProvider.Groq;
    
    public string? EncryptedOpenAIKey { get; set; }
    public string? EncryptedAnthropicKey { get; set; }
    public string? EncryptedDeepSeekKey { get; set; } // Add this
}