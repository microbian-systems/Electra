namespace Electra.Persistence.Entities    
{
    public record RefreshTokens : Entity<string>
    {
        public string UserId { get; set; }
        public string Token { get; set; }
    }
}