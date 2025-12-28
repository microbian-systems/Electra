using Electra.Crypto.Solana;
using Electra.Crypto.Solana.Extensions;
using Electra.Crypto.Solana.Utilities;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
using Solnet.Rpc;
using System;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Electra.Crypto.Solana.Examples;

/// <summary>
/// Example demonstrating how to use the Electra Solana Wallet library
/// This showcases wallet creation, management, and token operations similar to Phantom wallet
/// </summary>
public class WalletUsageExample
{
    private readonly ISolanaWalletManager _walletManager;
    private readonly ITokenAssetService _tokenAssetService;

    public WalletUsageExample(ISolanaWalletManager walletManager, ITokenAssetService tokenAssetService)
    {
        _walletManager = walletManager;
        _tokenAssetService = tokenAssetService;
    }

    /// <summary>
    /// Complete example showing various wallet operations
    /// </summary>
    public async Task RunCompleteExampleAsync()
    {
        Console.WriteLine("🚀 Electra Solana Wallet Example");
        Console.WriteLine("================================");

        try
        {
            // 1. Create a standard wallet
            await CreateStandardWalletExample();
            
            // 2. Import a wallet from mnemonic
            await ImportWalletExample();
            
            // 3. Create and manage burner wallets
            await BurnerWalletExample();
            
            // 4. Create sub-wallets
            await SubWalletExample();
            
            // 5. Wallet operations (balance, send, receive)
            await WalletOperationsExample();
            
            // 6. Token management
            await TokenManagementExample();
            
            // 7. Portfolio overview
            await PortfolioExample();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example 1: Creating a standard wallet
    /// </summary>
    private async Task CreateStandardWalletExample()
    {
        Console.WriteLine("\n📝 1. Creating Standard Wallet");
        Console.WriteLine("------------------------------");

        var walletName = "MyMainWallet";
        var passphrase = WalletHelpers.GenerateSecurePassphrase();
        
        Console.WriteLine($"Generated passphrase: {passphrase}");

        var wallet = await _walletManager.CreateWalletAsync(walletName, passphrase);
        
        await wallet.Match(
            Some: async w => {
                Console.WriteLine($"✅ Created wallet: {w.Name}");
                Console.WriteLine($"   Address: {w.PublicKey}");
                Console.WriteLine($"   Type: {w.WalletType}");
                
                // Get and display mnemonic (securely)
                var mnemonic = w.GetMnemonic();
                mnemonic.IfSome(m => Console.WriteLine($"   Mnemonic: {m}"));
                
                // Check balance
                var balance = await w.GetSolBalanceAsync();
                balance.IfSome(b => Console.WriteLine($"   SOL Balance: {WalletHelpers.FormatTokenAmount(b, 9, true, "SOL")}"));
            },
            None: () => {
                Console.WriteLine("❌ Failed to create wallet");
                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Example 2: Importing a wallet from mnemonic
    /// </summary>
    private async Task ImportWalletExample()
    {
        Console.WriteLine("\n📥 2. Importing Wallet from Mnemonic");
        Console.WriteLine("------------------------------------");

        var walletName = "ImportedWallet";
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var passphrase = "";

        // Validate mnemonic first
        var isValidMnemonic = WalletHelpers.IsValidMnemonic(mnemonic);
        if (isValidMnemonic.IfNone(false))
        {
            var wallet = await _walletManager.ImportWalletAsync(walletName, mnemonic, passphrase);
            
            await wallet.Match(
                Some: async w => {
                    Console.WriteLine($"✅ Imported wallet: {w.Name}");
                    Console.WriteLine($"   Address: {w.PublicKey}");
                    
                    var balance = await w.GetSolBalanceAsync();
                    balance.IfSome(b => Console.WriteLine($"   SOL Balance: {WalletHelpers.FormatTokenAmount(b, 9, true, "SOL")}"));
                },
                None: () => {
                    Console.WriteLine("❌ Failed to import wallet");
                    return Task.CompletedTask;
                }
            );
        }
        else
        {
            Console.WriteLine("❌ Invalid mnemonic phrase");
        }
    }

    /// <summary>
    /// Example 3: Creating and managing burner wallets
    /// </summary>
    private async Task BurnerWalletExample()
    {
        Console.WriteLine("\n🔥 3. Burner Wallet Management");
        Console.WriteLine("------------------------------");

        // Create a burner wallet with custom TTL
        var burnerWallet = await _walletManager.CreateBurnerWalletAsync("TempWallet");
        
        await burnerWallet.Match(
            Some: async w => {
                if (w is IBurnerWallet burner)
                {
                    Console.WriteLine($"✅ Created burner wallet: {burner.Name}");
                    Console.WriteLine($"   Address: {burner.PublicKey}");
                    Console.WriteLine($"   Created: {burner.CreatedAt}");
                    Console.WriteLine($"   TTL: {burner.TimeToLive}");
                    Console.WriteLine($"   Expires: {burner.CreatedAt.Add(burner.TimeToLive)}");

                    // Extend lifetime
                    // var extensionResult = await burner.ExtendLifetimeAsync(TimeSpan.FromHours(2));
                    // extensionResult.IfSome(_ => Console.WriteLine("   ⏰ Extended lifetime by 2 hours"));

                    // Check if expired
                    Console.WriteLine($"   Is Expired: {burner.IsExpired}");
                }
            },
            None: () => {
                Console.WriteLine("❌ Failed to create burner wallet");
                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Example 4: Creating sub-wallets (derived accounts)
    /// </summary>
    private async Task SubWalletExample()
    {
        Console.WriteLine("\n🌳 4. Sub-Wallet Creation");
        Console.WriteLine("-------------------------");

        var parentWalletName = "MyMainWallet";
        var subWalletName = "TradingAccount";
        var accountIndex = 1;

        var parentWallet = await _walletManager.GetWalletAsync(parentWalletName);
        
        await parentWallet.Match(
            Some: async _ => {
                var subWallet = await _walletManager.CreateSubWalletAsync(parentWalletName, subWalletName, accountIndex);
                
                await subWallet.Match(
                    Some: w => {
                        if (w is ISubWallet sub)
                        {
                            Console.WriteLine($"✅ Created sub-wallet: {sub.Name}");
                            Console.WriteLine($"   Address: {sub.PublicKey}");
                            Console.WriteLine($"   Parent: {sub.ParentWalletName}");
                            Console.WriteLine($"   Account Index: {sub.AccountIndex}");
                        }
                        return Task.CompletedTask;
                    },
                    None: () => {
                        Console.WriteLine("❌ Failed to create sub-wallet");
                        return Task.CompletedTask;
                    }
                );
            },
            None: () => {
                Console.WriteLine($"❌ Parent wallet '{parentWalletName}' not found");
                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Example 5: Basic wallet operations
    /// </summary>
    private async Task WalletOperationsExample()
    {
        Console.WriteLine("\n💰 5. Wallet Operations");
        Console.WriteLine("-----------------------");

        var wallet = await _walletManager.GetWalletAsync("MyMainWallet");
        
        await wallet.Match(
            Some: async w => {
                // Check SOL balance
                var solBalance = await w.GetSolBalanceAsync();
                solBalance.IfSome(balance => {
                    Console.WriteLine($"SOL Balance: {WalletHelpers.FormatTokenAmount(balance, 9, true, "SOL")}");
                    Console.WriteLine($"USD Value: {WalletHelpers.FormatUsdValue(balance * 100)}"); // Assuming $100 per SOL
                });

                // Get all token assets
                var assets = await w.GetTokenAssetsAsync();
                assets.IfSome(tokenList => {
                    Console.WriteLine($"Total tokens: {tokenList.Count()}");
                    foreach (var token in tokenList.Take(5)) // Show first 5
                    {
                        Console.WriteLine($"  {token.Symbol}: {WalletHelpers.FormatTokenAmount(token.Balance, token.Decimals)}");
                        if (token.Value.HasValue)
                        {
                            Console.WriteLine($"    Value: {WalletHelpers.FormatUsdValue(token.Value.Value)}");
                        }
                    }
                });

                // Example: Send SOL (commented out for safety in demo)
                /*
                var destinationAddress = "11111111111111111111111111111112";
                var amount = 0.001m;
                
                if (WalletHelpers.IsValidSolanaAddress(destinationAddress).IfNone(false) &&
                    WalletHelpers.IsValidTransactionAmount(amount, solBalance.IfNone(0)).IfNone(false))
                {
                    var txSignature = await w.SendSolAsync(destinationAddress, amount);
                    txSignature.IfSome(sig => Console.WriteLine($"✅ Transaction sent: {sig}"));
                }
                */

                // Message signing example
                var message = System.Text.Encoding.UTF8.GetBytes("Hello, Solana!");
                var signature = w.SignMessage(message);
                signature.IfSome(sig => {
                    Console.WriteLine($"✅ Message signed successfully");
                    
                    // Verify the signature
                    var isValid = w.VerifyMessage(message, sig);
                    if(isValid)
                        Console.WriteLine($"   Signature valid: {isValid}");
                });
            },
            None: () => {
                Console.WriteLine("❌ Wallet not found");
                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Example 6: Token management and information
    /// </summary>
    private async Task TokenManagementExample()
    {
        Console.WriteLine("\n🪙 6. Token Management");
        Console.WriteLine("----------------------");

        // Get popular tokens
        var popularTokens = await _tokenAssetService.GetPopularTokensAsync();
        popularTokens.IfSome(tokens => {
            Console.WriteLine("Popular tokens:");
            foreach (var token in tokens.Take(5))
            {
                Console.WriteLine($"  {token.Symbol} ({token.Name})");
                if (token.Price.HasValue)
                {
                    Console.WriteLine($"    Price: {WalletHelpers.FormatUsdValue(token.Price.Value)}");
                }
            }
        });

        // Search for specific tokens
        var searchResults = await _tokenAssetService.SearchTokensAsync("USDC");
        searchResults.IfSome(tokens => {
            Console.WriteLine("\nUSDC search results:");
            foreach (var token in tokens)
            {
                Console.WriteLine($"  {token.Symbol}: {token.Name}");
                Console.WriteLine($"    Mint: {WalletHelpers.TruncateAddress(token.MintAddress)}");
                Console.WriteLine($"    Verified: {token.IsVerified}");
            }
        });

        // Get specific token info
        var usdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
        var tokenInfo = await _tokenAssetService.GetTokenInfoAsync(usdcMint);
        tokenInfo.IfSome(info => {
            Console.WriteLine($"\nToken Info for {info.Symbol}:");
            Console.WriteLine($"  Name: {info.Name}");
            Console.WriteLine($"  Decimals: {info.Decimals}");
            Console.WriteLine($"  Verified: {info.IsVerified}");
            if (!string.IsNullOrEmpty(info.Website))
            {
                Console.WriteLine($"  Website: {info.Website}");
            }
        });
    }

    /// <summary>
    /// Example 7: Portfolio overview and management
    /// </summary>
    private async Task PortfolioExample()
    {
        Console.WriteLine("\n📊 7. Portfolio Overview");
        Console.WriteLine("------------------------");

        // Get all wallets
        var allWallets = await _walletManager.GetAllWalletsAsync();
        allWallets.IfSome(wallets => {
            Console.WriteLine($"Total wallets: {wallets.Count()}");
            foreach (var wallet in wallets)
            {
                Console.WriteLine($"  {wallet.Name} ({wallet.WalletType})");
                Console.WriteLine($"    Address: {WalletHelpers.TruncateAddress(wallet.PublicKey.ToString())}");
                Console.WriteLine($"    Locked: {wallet.IsLocked}");
            }
        });

        // Get total portfolio value
        var totalValue = await _walletManager.GetTotalPortfolioValueAsync();
        if(totalValue != null)
            Console.WriteLine($"\nTotal Portfolio Value: {WalletHelpers.FormatUsdValue(totalValue)}");
        

        // Get all token assets across wallets
        var allAssets = await _walletManager.GetAllTokenAssetsAsync();
        allAssets.IfSome(assets => {
            Console.WriteLine("\nAggregated Token Holdings:");
            foreach (var asset in assets.Where(a => a.Balance > 0).Take(10))
            {
                Console.WriteLine($"  {asset.Symbol}: {WalletHelpers.FormatTokenAmount(asset.Balance, asset.Decimals)}");
                if (asset.Value.HasValue)
                {
                    Console.WriteLine($"    Value: {WalletHelpers.FormatUsdValue(asset.Value.Value)}");
                }
            }
        });

        Console.WriteLine("\n✅ Example completed successfully!");
    }

    /// <summary>
    /// Helper method to demonstrate wallet security operations
    /// </summary>
    private async Task SecurityOperationsExample()
    {
        Console.WriteLine("\n🔒 Security Operations");
        Console.WriteLine("----------------------");

        var wallet = await _walletManager.GetWalletAsync("MyMainWallet");
        
        await wallet.Match(
            Some: async w => {
                // Lock wallet
                var lockResult = await w.Lock();
                
                if(lockResult)
                    Console.WriteLine("🔒 Wallet locked");
                
                Console.WriteLine($"   Is Locked: {w.IsLocked}");

                // Try to access mnemonic while locked (should fail)
                var mnemonic = w.GetMnemonic();
                if (mnemonic.IsNone)
                {
                    Console.WriteLine("✅ Mnemonic access blocked while locked");
                }

                // Unlock wallet
                // var unlockResult = await w.UnlockAsync("secure-passphrase");
                // unlockResult.IfSome(_ => Console.WriteLine("🔓 Wallet unlocked"));
                Console.WriteLine($"   Is Locked: {w.IsLocked}");
            },
            None: () => {
                Console.WriteLine("❌ Wallet not found for security demo");
                return Task.CompletedTask;
            }
        );
    }
}

/// <summary>
/// Program entry point showing how to set up dependency injection
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up dependency injection
        // var host = Host.CreateDefaultBuilder(args)
        //     .ConfigureServices((context, services) =>
        //     {
        //         // Add Solana wallet services for devnet (safer for examples)
        //         services.AddSolanaWalletForDevnet();
        //         
        //         // Register example service
        //         services.AddTransient<WalletUsageExample>();
        //     })
        //     .Build();
        //
        // // Run example
        // var example = host.Services.GetRequiredService<WalletUsageExample>();
        // await example.RunCompleteExampleAsync();
    }
}

/// <summary>
/// Extension methods for easier wallet management
/// </summary>
public static class WalletExtensions
{
    /// <summary>
    /// Get formatted address for display
    /// </summary>
    public static string GetDisplayAddress(this ISolanaWallet wallet, int startChars = 4, int endChars = 4)
    {
        return WalletHelpers.TruncateAddress(wallet.PublicKey.ToString(), startChars, endChars);
    }

    /// <summary>
    /// Check if wallet has sufficient balance for transaction
    /// </summary>
    public static async Task<bool> HasSufficientBalanceAsync(this ISolanaWallet wallet, decimal amount, bool includeTransactionFee = true)
    {
        var balance = await wallet.GetSolBalanceAsync();
        if (balance.IsNone) return false;

        var requiredAmount = includeTransactionFee ? amount + WalletHelpers.EstimateSolTransferFee() : amount;
        return balance.IfNone(0) >= requiredAmount;
    }

    /// <summary>
    /// Get wallet summary information
    /// </summary>
    public static async Task<WalletSummary> GetSummaryAsync(this ISolanaWallet wallet)
    {
        var solBalance = await wallet.GetSolBalanceAsync();
        var tokens = await wallet.GetTokenAssetsAsync();
        
        return new WalletSummary(
            wallet.Name,
            wallet.GetDisplayAddress(),
            wallet.WalletType,
            wallet.IsLocked,
            solBalance.IfNone(0),
            tokens.Match(
                Some: t => t.Count(),
                None: () => 0
            ),
            tokens.Match(
                Some: t => t.Where(x => x.Value.HasValue).Sum(x => x.Value!.Value),
                None: () => 0
            )
        );
    }
}

/// <summary>
/// Wallet summary record for easy display
/// </summary>
public record WalletSummary(
    string Name,
    string DisplayAddress,
    WalletType Type,
    bool IsLocked,
    decimal SolBalance,
    int TokenCount,
    decimal TotalValue
);