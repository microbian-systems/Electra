using System;
using System.Text.Json.Serialization;

namespace Electra.Crypto.Solana;

public record TokenAssetInfo
{
    [JsonPropertyName("mint")]
    public string MintAddress { get; init; } = string.Empty;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("decimals")]
    public int Decimals { get; init; }
    
    [JsonPropertyName("balance")]
    public decimal Balance { get; init; }
    
    [JsonPropertyName("balanceRaw")]
    public ulong BalanceRaw { get; init; }
    
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }
    
    [JsonPropertyName("value")]
    public decimal? Value => Price.HasValue ? Balance * Price.Value : null;
    
    [JsonPropertyName("logoUri")]
    public string? LogoUri { get; init; }
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("website")]
    public string? Website { get; init; }
    
    [JsonPropertyName("twitter")]
    public string? Twitter { get; init; }
    
    [JsonPropertyName("discord")]
    public string? Discord { get; init; }
    
    [JsonPropertyName("isNft")]
    public bool IsNft { get; init; }
    
    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; init; }
    
    [JsonPropertyName("tags")]
    public string[] Tags { get; init; } = [];
    
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    
    public static TokenAssetInfo CreateSolToken(decimal balance)
    {
        return new TokenAssetInfo
        {
            MintAddress = "So11111111111111111111111111111111111111112", // Wrapped SOL mint
            Symbol = "SOL",
            Name = "Solana",
            Decimals = 9,
            Balance = balance,
            BalanceRaw = (ulong)(balance * (decimal)Math.Pow(10, 9)),
            LogoUri = "https://raw.githubusercontent.com/solana-labs/token-list/main/assets/mainnet/So11111111111111111111111111111111111111112/logo.png",
            Description = "Solana is a decentralized blockchain built to enable scalable, user-friendly apps for the world.",
            Website = "https://solana.com",
            Twitter = "solana",
            IsVerified = true,
            Tags = new[] { "wrapped", "native" }
        };
    }
    
    public TokenAssetInfo WithBalance(decimal newBalance, ulong newBalanceRaw)
    {
        return this with { Balance = newBalance, BalanceRaw = newBalanceRaw, LastUpdated = DateTime.UtcNow };
    }
    
    public TokenAssetInfo WithPrice(decimal newPrice)
    {
        return this with { Price = newPrice, LastUpdated = DateTime.UtcNow };
    }
}