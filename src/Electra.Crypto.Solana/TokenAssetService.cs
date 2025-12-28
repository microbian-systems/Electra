using LanguageExt;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Pyth;
using Solnet.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Electra.Crypto.Solana;

public class TokenAssetService : ITokenAssetService, IDisposable
{
    private readonly IRpcClient _rpcClient;
    private readonly ITokenMintResolver _tokenMintResolver;
    private readonly IPythClient _pythClient;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, TokenAssetInfo> _tokenInfoCache;
    private readonly Dictionary<string, (decimal Price, DateTime LastUpdated)> _priceCache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);
    private bool _disposed;

    public TokenAssetService(IRpcClient rpcClient, ITokenMintResolver tokenMintResolver, IPythClient pythClient = null)
    {
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        _tokenMintResolver = tokenMintResolver ?? throw new ArgumentNullException(nameof(tokenMintResolver));
        _pythClient = pythClient;
        _httpClient = new HttpClient();
        _tokenInfoCache = new Dictionary<string, TokenAssetInfo>();
        _priceCache = new Dictionary<string, (decimal, DateTime)>();
    }

    public async Task<Option<TokenAssetInfo>> GetTokenInfoAsync(string mintAddress)
    {
        if (string.IsNullOrEmpty(mintAddress))
            return None;

        try
        {
            // Check cache first
            if (_tokenInfoCache.ContainsKey(mintAddress))
            {
                var cached = _tokenInfoCache[mintAddress];
                if (DateTime.UtcNow - cached.LastUpdated < _cacheExpiry)
                {
                    return cached;
                }
            }

            // Try to resolve from token mint resolver first
            var tokenDef = _tokenMintResolver.Resolve(mintAddress);
            if (tokenDef != null)
            {
                var basicInfo = new TokenAssetInfo
                {
                    MintAddress = mintAddress,
                    Symbol = tokenDef.Symbol,
                    Name = tokenDef.TokenName,
                    Decimals = tokenDef.DecimalPlaces,
                    Balance = 0,
                    IsVerified = !string.IsNullOrEmpty(tokenDef.TokenLogoUrl),
                    LogoUri = tokenDef.TokenLogoUrl
                };

                // Try to get additional metadata from Solana token registry or other sources
                var enhancedInfo = await EnhanceTokenInfoAsync(basicInfo);
                if (enhancedInfo.IsSome)
                {
                    _tokenInfoCache[mintAddress] = enhancedInfo.IfNone(basicInfo);
                    return enhancedInfo;
                }

                _tokenInfoCache[mintAddress] = basicInfo;
                return basicInfo;
            }

            // Fallback: try to get info from on-chain metadata or token registry APIs
            var onChainInfo = await GetTokenInfoFromRegistryAsync(mintAddress);
            if (onChainInfo.IsSome)
            {
                _tokenInfoCache[mintAddress] = onChainInfo.IfNone(new TokenAssetInfo());
                return onChainInfo;
            }

            return None;
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<decimal>> GetTokenPriceAsync(string mintAddress)
    {
        if (string.IsNullOrEmpty(mintAddress))
            return None;

        try
        {
            // Check cache first
            if (_priceCache.ContainsKey(mintAddress))
            {
                var (price, lastUpdated) = _priceCache[mintAddress];
                if (DateTime.UtcNow - lastUpdated < _cacheExpiry)
                {
                    return price;
                }
            }

            // Try Pyth network for real-time prices if available
            if (_pythClient != null)
            {
                var pythPrice = await GetPriceFromPythAsync(mintAddress);
                if (pythPrice.IsSome)
                {
                    var priceValue = pythPrice.IfNone(0);
                    _priceCache[mintAddress] = (priceValue, DateTime.UtcNow);
                    return priceValue;
                }
            }

            // Fallback to other price APIs (Jupiter, CoinGecko, etc.)
            var jupiterPrice = await GetPriceFromJupiterAsync(mintAddress);
            if (jupiterPrice.IsSome)
            {
                var priceValue = jupiterPrice.IfNone(0);
                _priceCache[mintAddress] = (priceValue, DateTime.UtcNow);
                return priceValue;
            }

            return None;
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<IEnumerable<TokenAssetInfo>>> GetPopularTokensAsync()
    {
        try
        {
            // Return a list of popular Solana tokens
            var popularMints = new[]
            {
                "So11111111111111111111111111111111111111112", // SOL
                "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v", // USDC
                "Es9vMFrzaCERmJfrF4H2FYD4KCoNkY11McCe8BenwNYB", // USDT
                "DezXAZ8z7PnrnRJjz3wXBoRgixCa6xjnB7YaB1pPB263", // BONK
                "5oVNBeEEQvYi1cX3ir8Dx5n1P7pdxydbGF2X4TxVusJm", // INF
                "rndrizKT3MK1iimdxRdWabcF7Zg7AR5T4nud4EkHBof",  // RND
                "SHDWyBxihqiCj6YekG2GUr7wqKLeLAMK1gHZck9pL6y",  // SHDW
                "JUPyiwrYJFskUPiHa7hkeR8VUtAeFoSYbKedZNsDvCN"   // JUP
            };

            var tokens = new List<TokenAssetInfo>();
            
            foreach (var mint in popularMints)
            {
                var tokenInfo = await GetTokenInfoAsync(mint);
                if (tokenInfo.IsSome)
                {
                    var info = tokenInfo.IfNone(new TokenAssetInfo());
                    var price = await GetTokenPriceAsync(mint);
                    if (price.IsSome)
                    {
                        tokens.Add(info.WithPrice(price.IfNone(0)));
                    }
                    else
                    {
                        tokens.Add(info);
                    }
                }
            }

            return Option<IEnumerable<TokenAssetInfo>>.Some(tokens.AsEnumerable());
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<IEnumerable<TokenAssetInfo>>> SearchTokensAsync(string query)
    {
        if (string.IsNullOrEmpty(query))
            return None;

        try
        {
            // In a real implementation, this would search through a token registry
            // For now, we'll do a simple filter on popular tokens
            var popularTokens = await GetPopularTokensAsync();
            
            var ret = popularTokens.Match(
                Some: tokens => tokens.Where(t => 
                    t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    t.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    t.MintAddress.Contains(query, StringComparison.OrdinalIgnoreCase)
                ).AsEnumerable(),
                None: () => Enumerable.Empty<TokenAssetInfo>()
            );
            
            return Option<IEnumerable<TokenAssetInfo>>.Some(ret);
        }
        catch
        {
            return None;
        }
    }

    private async Task<Option<TokenAssetInfo>> EnhanceTokenInfoAsync(TokenAssetInfo basicInfo)
    {
        try
        {
            // Try to get additional metadata from Solana token registry
            var registryInfo = await GetTokenInfoFromRegistryAsync(basicInfo.MintAddress);
            if (registryInfo.IsSome)
            {
                var enhanced = registryInfo.IfNone(basicInfo);
                return basicInfo with
                {
                    Description = enhanced.Description ?? basicInfo.Description,
                    Website = enhanced.Website ?? basicInfo.Website,
                    Twitter = enhanced.Twitter ?? basicInfo.Twitter,
                    Discord = enhanced.Discord ?? basicInfo.Discord,
                    LogoUri = enhanced.LogoUri ?? basicInfo.LogoUri,
                    Tags = enhanced.Tags?.Length > 0 ? enhanced.Tags : basicInfo.Tags
                };
            }

            return basicInfo;
        }
        catch
        {
            return basicInfo;
        }
    }

    private async Task<Option<TokenAssetInfo>> GetTokenInfoFromRegistryAsync(string mintAddress)
    {
        try
        {
            // This would typically call the Solana token registry API
            // For demo purposes, we'll return None - in a real implementation you'd call:
            // https://raw.githubusercontent.com/solana-labs/token-list/main/src/tokens/solana.tokenlist.json
            
            var url = $"https://raw.githubusercontent.com/solana-labs/token-list/main/src/tokens/solana.tokenlist.json";
            var response = await _httpClient.GetStringAsync(url);
            var tokenList = JsonSerializer.Deserialize<TokenRegistryResponse>(response);
            
            var token = tokenList?.Tokens?.FirstOrDefault(t => t.Address == mintAddress);
            if (token != null)
            {
                return new TokenAssetInfo
                {
                    MintAddress = token.Address,
                    Symbol = token.Symbol,
                    Name = token.Name,
                    Decimals = token.Decimals,
                    LogoUri = token.LogoURI,
                    Tags = token.Tags ?? [],
                    IsVerified = true
                };
            }

            return None;
        }
        catch
        {
            return None;
        }
    }

    private async Task<Option<decimal>> GetPriceFromPythAsync(string mintAddress)
    {
        try
        {
            if (_pythClient == null)
                return None;

            // This would use Pyth network to get real-time prices
            // Implementation depends on Pyth price feeds for specific tokens
            // For demo purposes, returning None
            return None;
        }
        catch
        {
            return None;
        }
    }

    private async Task<Option<decimal>> GetPriceFromJupiterAsync(string mintAddress)
    {
        try
        {
            // Jupiter API for token prices
            var url = $"https://price.jup.ag/v4/price?ids={mintAddress}";
            var response = await _httpClient.GetStringAsync(url);
            var priceResponse = JsonSerializer.Deserialize<JupiterPriceResponse>(response);
            
            if (priceResponse?.Data?.ContainsKey(mintAddress) == true)
            {
                return (decimal)priceResponse.Data[mintAddress].Price;
            }

            return None;
        }
        catch
        {
            return None;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

// DTOs for external APIs
public class TokenRegistryResponse
{
    public TokenRegistryToken[] Tokens { get; set; } = [];
}

public class TokenRegistryToken
{
    public string Address { get; set; } = string.Empty;
    public string ChainId { get; set; } = string.Empty;
    public int Decimals { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string LogoURI { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
}

public class JupiterPriceResponse
{
    public Dictionary<string, JupiterTokenPrice> Data { get; set; } = new();
}

public class JupiterTokenPrice
{
    public string Id { get; set; } = string.Empty;
    public double Price { get; set; }
    public string MintSymbol { get; set; } = string.Empty;
    public string VsToken { get; set; } = string.Empty;
}