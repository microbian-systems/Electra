# Electra.Crypto.Solana

Solana blockchain integration for the Aero framework using Solnet libraries.

## Overview

`Electra.Crypto.Solana` provides comprehensive Solana blockchain integration, enabling wallet management, token operations, NFT handling, DeFi interactions, and price oracle data access.

## Key Components

### Wallet Management

```csharp
public interface ISolanaWallet
{
    string PublicKey { get; }
    Task<string> SignMessageAsync(byte[] message);
    Task<Transaction> SignTransactionAsync(Transaction transaction);
}

public class SolanaWallet : ISolanaWallet
{
    private readonly Wallet _wallet;
    private readonly IRpcClient _rpcClient;

    public SolanaWallet(Wallet wallet, IRpcClient rpcClient)
    {
        _wallet = wallet;
        _rpcClient = rpcClient;
        PublicKey = wallet.Account.PublicKey.Key;
    }

    public async Task<string> SignMessageAsync(byte[] message)
    {
        var signature = _wallet.Account.Sign(message);
        return Encoders.Base58.EncodeData(signature);
    }

    public async Task<Transaction> SignTransactionAsync(Transaction transaction)
    {
        transaction.Sign(_wallet.Account);
        return transaction;
    }

    public async Task<ulong> GetBalanceAsync()
    {
        var balanceResult = await _rpcClient.GetBalanceAsync(PublicKey);
        return balanceResult.Result.Value;
    }
}
```

### Wallet Manager

```csharp
public class SolanaWalletManager
{
    private readonly IRpcClient _rpcClient;
    private readonly ITokenAccountService _tokenService;

    public async Task<SolanaWallet> CreateWalletAsync(string? mnemonic = null)
    {
        var wallet = string.IsNullOrEmpty(mnemonic) 
            ? new Wallet(WordCount.TwentyFour, WordList.English)
            : new Wallet(mnemonic, WordList.English);

        return new SolanaWallet(wallet, _rpcClient);
    }

    public async Task<SolanaWallet> ImportWalletAsync(string privateKey)
    {
        var account = new Account(privateKey);
        var wallet = new Wallet(new Account[] { account });
        return new SolanaWallet(wallet, _rpcClient);
    }

    public async Task<SolanaWallet> ImportFromMnemonicAsync(string mnemonic)
    {
        var wallet = new Wallet(mnemonic, WordList.English);
        return new SolanaWallet(wallet, _rpcClient);
    }

    public async Task<bool> ValidateAddressAsync(string address)
    {
        try
        {
            _ = new PublicKey(address);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### Token Operations

```csharp
public class TokenAssetService
{
    private readonly IRpcClient _rpcClient;
    private readonly ITokenAccountService _tokenAccountService;

    public async Task<List<TokenAssetInfo>> GetTokenAccountsAsync(string walletAddress)
    {
        var owner = new PublicKey(walletAddress);
        var accounts = await _tokenAccountService.GetTokenAccountsByOwnerAsync(
            owner, 
            TokenProgram.ProgramIdKey);

        var tokens = new List<TokenAssetInfo>();
        
        foreach (var account in accounts.Value)
        {
            var tokenInfo = new TokenAssetInfo
            {
                Mint = account.Account.Data.Parsed.Info.Mint,
                Amount = ulong.Parse(account.Account.Data.Parsed.Info.TokenAmount.Amount),
                Decimals = account.Account.Data.Parsed.Info.TokenAmount.Decimals,
                UiAmount = account.Account.Data.Parsed.Info.TokenAmount.UiAmount
            };

            // Get token metadata
            var metadata = await GetTokenMetadataAsync(tokenInfo.Mint);
            tokenInfo.Symbol = metadata?.Symbol;
            tokenInfo.Name = metadata?.Name;

            tokens.Add(tokenInfo);
        }

        return tokens;
    }

    public async Task<string> TransferTokenAsync(
        SolanaWallet wallet,
        string recipientAddress,
        string mintAddress,
        ulong amount)
    {
        var mint = new PublicKey(mintAddress);
        var recipient = new PublicKey(recipientAddress);
        var owner = new PublicKey(wallet.PublicKey);

        // Get associated token accounts
        var sourceATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
            owner, mint);
        var destinationATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
            recipient, mint);

        // Build transaction
        var transaction = new TransactionBuilder()
            .SetRecentBlockHash(await GetRecentBlockHashAsync())
            .SetFeePayer(owner)
            .AddInstruction(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                owner, recipient, mint))
            .AddInstruction(TokenProgram.Transfer(
                sourceATA, destinationATA, amount, owner))
            .Build();

        // Sign and send
        var signedTx = await wallet.SignTransactionAsync(transaction);
        var result = await _rpcClient.SendTransactionAsync(signedTx.Serialize());

        return result.Result;
    }

    private async Task<TokenMetadata> GetTokenMetadataAsync(string mintAddress)
    {
        var client = ClientFactory.GetClient(Cluster.MainNet);
        var metadata = await MetadataAccount.GetAccount(client, new PublicKey(mintAddress));
        
        return new TokenMetadata
        {
            Mint = mintAddress,
            Name = metadata?.Data?.Name,
            Symbol = metadata?.Data?.Symbol,
            Uri = metadata?.Data?.Uri
        };
    }
}
```

### Swap Service (Raydium Integration)

```csharp
public class SwapService
{
    private readonly IRpcClient _rpcClient;
    private readonly IRaydiumClient _raydiumClient;

    public async Task<SwapQuote> GetSwapQuoteAsync(
        string inputMint,
        string outputMint,
        decimal amount,
        decimal slippageBps = 50)
    {
        var quote = await _raydiumClient.GetSwapQuoteAsync(
            inputMint,
            outputMint,
            amount,
            slippageBps);

        return new SwapQuote
        {
            InputMint = inputMint,
            OutputMint = outputMint,
            InAmount = quote.InAmount,
            OutAmount = quote.OutAmount,
            PriceImpactPct = quote.PriceImpactPct,
            Route = quote.Route
        };
    }

    public async Task<string> ExecuteSwapAsync(
        SolanaWallet wallet,
        SwapQuote quote)
    {
        var swapInstructions = await _raydiumClient.BuildSwapTransactionAsync(
            quote,
            wallet.PublicKey);

        var transaction = new TransactionBuilder()
            .SetRecentBlockHash(await GetRecentBlockHashAsync())
            .SetFeePayer(new PublicKey(wallet.PublicKey))
            .AddInstructions(swapInstructions)
            .Build();

        var signedTx = await wallet.SignTransactionAsync(transaction);
        var result = await _rpcClient.SendTransactionAsync(signedTx.Serialize());

        return result.Result;
    }
}
```

### Price Oracle (Pyth Integration)

```csharp
public class PriceOracleService
{
    private readonly IPythClient _pythClient;

    public async Task<PriceData> GetPriceAsync(string priceFeedId)
    {
        var priceData = await _pythClient.GetPriceAsync(priceFeedId);

        return new PriceData
        {
            Symbol = priceData.Symbol,
            Price = priceData.Price,
            Confidence = priceData.Confidence,
            Exponent = priceData.Exponent,
            PublishTime = priceData.PublishTime
        };
    }

    public async Task<Dictionary<string, PriceData>> GetPricesAsync(
        IEnumerable<string> priceFeedIds)
    {
        var prices = new Dictionary<string, PriceData>();
        
        foreach (var feedId in priceFeedIds)
        {
            var price = await GetPriceAsync(feedId);
            prices[feedId] = price;
        }

        return prices;
    }

    public async Task<List<PriceFeed>> GetAvailablePriceFeedsAsync()
    {
        return await _pythClient.GetPriceFeedsAsync();
    }
}
```

### NFT Operations (Metaplex)

```csharp
public class NftService
{
    private readonly IRpcClient _rpcClient;
    private readonly IMetaplexClient _metaplexClient;

    public async Task<List<NftInfo>> GetNftsByOwnerAsync(string walletAddress)
    {
        var owner = new PublicKey(walletAddress);
        var accounts = await _rpcClient.GetTokenAccountsByOwnerAsync(
            owner,
            TokenProgram.ProgramIdKey,
            TokenProgram.ProgramIdKey);

        var nfts = new List<NftInfo>();

        foreach (var account in accounts.Value)
        {
            var mint = account.Account.Data.Parsed.Info.Mint;
            
            // Check if it's an NFT (supply = 1, decimals = 0)
            var mintInfo = await _rpcClient.GetAccountInfoAsync(mint);
            if (IsNft(mintInfo))
            {
                var metadata = await _metaplexClient.GetMetadataAsync(mint);
                var nftInfo = new NftInfo
                {
                    Mint = mint,
                    Name = metadata?.Data?.Name,
                    Symbol = metadata?.Data?.Symbol,
                    Uri = metadata?.Data?.Uri,
                    ImageUrl = await GetImageUrlAsync(metadata?.Data?.Uri)
                };
                nfts.Add(nftInfo);
            }
        }

        return nfts;
    }

    public async Task<string> MintNftAsync(
        SolanaWallet wallet,
        NftMetadata metadata,
        string? recipientAddress = null)
    {
        var mint = new Account();
        var owner = new PublicKey(wallet.PublicKey);
        var recipient = recipientAddress != null 
            ? new PublicKey(recipientAddress) 
            : owner;

        // Create metadata
        var metadataAccount = MetadataAccount.DerivePda(mint.PublicKey);

        var transaction = new TransactionBuilder()
            .SetRecentBlockHash(await GetRecentBlockHashAsync())
            .SetFeePayer(owner)
            .AddInstruction(SystemProgram.CreateAccount(
                owner,
                mint.PublicKey,
                await GetRentExemptBalanceAsync(MetadataAccount.Layout.SpanLength),
                MetadataAccount.Layout.SpanLength,
                MetadataProgram.ProgramIdKey))
            .AddInstruction(MetadataProgram.CreateMetadataAccount(
                metadataAccount,
                mint.PublicKey,
                owner,
                owner,
                owner,
                metadata.Name,
                metadata.Symbol,
                metadata.Uri,
                null,
                0,
                false,
                false))
            .Build();

        transaction.Sign(wallet.Account, mint);
        
        var result = await _rpcClient.SendTransactionAsync(transaction.Serialize());
        return result.Result;
    }
}
```

### Burner Wallet

```csharp
public class BurnerWallet : ISolanaWallet
{
    private readonly Wallet _wallet;
    private readonly IRpcClient _rpcClient;

    public BurnerWallet(IRpcClient rpcClient)
    {
        _wallet = new Wallet(WordCount.Twelve, WordList.English);
        _rpcClient = rpcClient;
        PublicKey = _wallet.Account.PublicKey.Key;
    }

    public string PublicKey { get; }

    public async Task AirdropAsync(ulong lamports = 1000000000) // 1 SOL
    {
        if (_rpcClient.NodeAddress.ToString().Contains("devnet") ||
            _rpcClient.NodeAddress.ToString().Contains("testnet"))
        {
            await _rpcClient.RequestAirdropAsync(PublicKey, lamports);
        }
    }

    public async Task<string> SignMessageAsync(byte[] message)
    {
        var signature = _wallet.Account.Sign(message);
        return Encoders.Base58.EncodeData(signature);
    }

    public async Task<Transaction> SignTransactionAsync(Transaction transaction)
    {
        transaction.Sign(_wallet.Account);
        return transaction;
    }
}
```

## Setup

### Configuration

```csharp
// Program.cs
builder.Services.AddSolanaServices(builder.Configuration);

public static class SolanaServiceExtensions
{
    public static IServiceCollection AddSolanaServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        var cluster = config["Solana:Cluster"] switch
        {
            "MainNet" => Cluster.MainNet,
            "TestNet" => Cluster.TestNet,
            "DevNet" => Cluster.DevNet,
            _ => Cluster.DevNet
        };

        services.AddSingleton<IRpcClient>(sp => 
            ClientFactory.GetClient(cluster));

        services.AddSingleton<StreamingRpcClient>(sp =>
            new StreamingRpcClient(cluster));

        services.AddScoped<SolanaWalletManager>();
        services.AddScoped<TokenAssetService>();
        services.AddScoped<SwapService>();
        services.AddScoped<PriceOracleService>();
        services.AddScoped<NftService>();

        return services;
    }
}
```

### appsettings.json

```json
{
  "Solana": {
    "Cluster": "DevNet",
    "CustomRpcUrl": null,
    "Commitment": "Confirmed"
  }
}
```

## Usage Examples

### Creating a Wallet

```csharp
public class WalletController : ControllerBase
{
    private readonly SolanaWalletManager _walletManager;

    [HttpPost("create")]
    public async Task<ActionResult<WalletDto>> CreateWallet()
    {
        var wallet = await _walletManager.CreateWalletAsync();
        
        return Ok(new WalletDto
        {
            PublicKey = wallet.PublicKey,
            Mnemonic = wallet.Mnemonic, // Only return on creation!
            Balance = await wallet.GetBalanceAsync()
        });
    }

    [HttpPost("import")]
    public async Task<ActionResult<WalletDto>> ImportWallet([FromBody] ImportRequest request)
    {
        var wallet = request.Type switch
        {
            "mnemonic" => await _walletManager.ImportFromMnemonicAsync(request.Key),
            "privatekey" => await _walletManager.ImportWalletAsync(request.Key),
            _ => throw new ArgumentException("Invalid import type")
        };

        return Ok(new WalletDto
        {
            PublicKey = wallet.PublicKey,
            Balance = await wallet.GetBalanceAsync()
        });
    }
}
```

### Checking Token Balances

```csharp
[HttpGet("tokens/{walletAddress}")]
public async Task<ActionResult<List<TokenInfo>>> GetTokens(string walletAddress)
{
    var tokens = await _tokenService.GetTokenAccountsAsync(walletAddress);
    
    return Ok(tokens.Select(t => new TokenInfo
    {
        Mint = t.Mint,
        Symbol = t.Symbol,
        Name = t.Name,
        Balance = t.UiAmount,
        Decimals = t.Decimals
    }));
}
```

## Best Practices

1. **Secure Private Keys** - Never store private keys in plain text
2. **Use Transaction Simulations** - Simulate transactions before sending
3. **Handle Failures** - Properly handle network and RPC failures
4. **Rate Limiting** - Apply rate limits to blockchain operations
5. **Caching** - Cache price data and metadata when appropriate
6. **Confirmation Levels** - Choose appropriate commitment levels
7. **Error Handling** - Handle insufficient funds and invalid accounts

## Related Packages

- `Electra.Crypto.Core` - Core crypto interfaces
- `Aero.Core` - Entity definitions
- `Solnet.*` - Solana libraries (submodules)
