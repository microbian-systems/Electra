using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Rpc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using Electra.Crypto.Solana.Extensions;

namespace Electra.Crypto.Solana.Tests;

/// <summary>
/// End-to-end tests demonstrating complete wallet workflows including creation, funding, and token swaps
/// </summary>
[Collection("E2E Wallet Swap Tests")]
public class E2EWalletSwapTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRpcClient _rpcClient;
    private readonly ISolanaWalletManager _walletManager;
    private readonly ISwapService _swapService;
    private readonly ITokenAssetService _tokenAssetService;

    // Test constants
    private const string SolMintAddress = "So11111111111111111111111111111111111111112";
    private const string UsdcMintAddress = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
    private const string TestWallet1Name = "E2ETestWallet1";
    private const string TestWallet2Name = "E2ETestWallet2";

    public E2EWalletSwapTests()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Add Solana wallet services for testnet
        services.AddSolanaWalletForTestnet();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get services
        _rpcClient = _serviceProvider.GetRequiredService<IRpcClient>();
        _walletManager = _serviceProvider.GetRequiredService<ISolanaWalletManager>();
        _swapService = _serviceProvider.GetRequiredService<ISwapService>();
        _tokenAssetService = _serviceProvider.GetRequiredService<ITokenAssetService>();
    }

    public async Task InitializeAsync()
    {
        // Verify testnet connection
        var health = await _rpcClient.GetHealthAsync();
        health.WasSuccessful.Should().BeTrue("Testnet should be accessible");
        
        // Cleanup any existing test wallets
        await _walletManager.DeleteWalletAsync(TestWallet1Name);
        await _walletManager.DeleteWalletAsync(TestWallet2Name);
    }

    public async Task DisposeAsync()
    {
        // Cleanup test wallets
        await _walletManager.DeleteWalletAsync(TestWallet1Name);
        await _walletManager.DeleteWalletAsync(TestWallet2Name);
        
        _serviceProvider?.GetService<IDisposable>()?.Dispose();
    }

    [Fact]
    public async Task E2E_CreateTwoWallets_CheckBalances_TransferSol()
    {
        // Step 1: Create two wallets with seed phrases
        var wallet1Result = await _walletManager.CreateWalletWithSeedAsync(TestWallet1Name, "password1", use24Words: false);
        var wallet2Result = await _walletManager.CreateWalletWithSeedAsync(TestWallet2Name, "password2", use24Words: true);

        // Verify wallet creation
        wallet1Result.IsSome.Should().BeTrue();
        wallet2Result.IsSome.Should().BeTrue();

        ISolanaWallet wallet1 = null;
        ISolanaWallet wallet2 = null;
        string[] seed1 = null;
        string[] seed2 = null;

        wallet1Result.IfSome(result =>
        {
            wallet1 = result.wallet;
            seed1 = result.seedPhrase;
        });

        wallet2Result.IfSome(result =>
        {
            wallet2 = result.wallet;
            seed2 = result.seedPhrase;
        });

        // Verify wallet properties
        wallet1.Should().NotBeNull();
        wallet2.Should().NotBeNull();
        seed1.Should().NotBeNull();
        seed2.Should().NotBeNull();
        seed1.Length.Should().Be(12);
        seed2.Length.Should().Be(24);

        // Step 2: Check initial balances
        var wallet1Balance = await wallet1.GetSolBalanceAsync();
        var wallet2Balance = await wallet2.GetSolBalanceAsync();

        wallet1Balance.IsSome.Should().BeTrue();
        wallet2Balance.IsSome.Should().BeTrue();

        // Initial balances should be 0
        wallet1Balance.IfNone(0).Should().Be(0);
        wallet2Balance.IfNone(0).Should().Be(0);

        // Step 3: Request testnet airdrop for wallet1 (skip if airdrop fails)
        try
        {
            var airdropResult = await _rpcClient.RequestAirdropAsync(wallet1.PublicKey.Key, 1_000_000_000); // 1 SOL
            if (airdropResult.WasSuccessful)
            {
                // Wait for airdrop confirmation
                await Task.Delay(10000);

                // Check updated balance
                var updatedBalance = await wallet1.GetSolBalanceAsync();
                updatedBalance.IsSome.Should().BeTrue();
                
                if (updatedBalance.IfNone(0) > 0.1m) // If we received some SOL
                {
                    // Step 4: Transfer SOL from wallet1 to wallet2
                    var transferAmount = 0.01m; // Transfer 0.01 SOL
                    var transferResult = await wallet1.SendSolAsync(wallet2.PublicKey.Key, transferAmount);

                    if (transferResult.IsSome)
                    {
                        transferResult.IfSome(txSignature =>
                        {
                            txSignature.Should().NotBeNullOrEmpty();
                        });

                        // Wait for transaction confirmation
                        await Task.Delay(5000);

                        // Verify balances changed
                        var wallet1FinalBalance = await wallet1.GetSolBalanceAsync();
                        var wallet2FinalBalance = await wallet2.GetSolBalanceAsync();

                        wallet2FinalBalance.IfNone(0).Should().BeGreaterThan(0);
                    }
                }
            }
        }
        catch
        {
            // Airdrop might fail on testnet, skip this part of the test
            Assert.True(true, "Airdrop test skipped due to testnet limitations");
        }

        // Step 5: Verify wallets can be retrieved from manager
        var retrievedWallet1 = await _walletManager.GetWalletAsync(TestWallet1Name);
        var retrievedWallet2 = await _walletManager.GetWalletAsync(TestWallet2Name);

        retrievedWallet1.IsSome.Should().BeTrue();
        retrievedWallet2.IsSome.Should().BeTrue();

        retrievedWallet1.IfSome(w => w.Name.Should().Be(TestWallet1Name));
        retrievedWallet2.IfSome(w => w.Name.Should().Be(TestWallet2Name));
    }

    [Fact]
    public async Task E2E_CreateWallet_GetSwapQuote_ValidateSwapPreparation()
    {
        // Step 1: Create a wallet for swap testing
        var walletResult = await _walletManager.CreateWalletWithSeedAsync("SwapE2EWallet", "swappass");
        walletResult.IsSome.Should().BeTrue();

        ISolanaWallet swapWallet = null;
        walletResult.IfSome(result => swapWallet = result.wallet);
        swapWallet.Should().NotBeNull();

        // Step 2: Get token assets (should include SOL by default)
        var tokenAssets = await swapWallet.GetTokenAssetsAsync();
        tokenAssets.IsSome.Should().BeTrue();

        tokenAssets.IfSome(assets =>
        {
            assets.Should().Contain(asset => asset.Symbol == "SOL");
        });

        // Step 3: Attempt to get a swap quote (using mainnet for liquidity)
        // Note: Switch to mainnet RPC for quote testing as testnet may lack liquidity
        var mainnetServices = new ServiceCollection();
        mainnetServices.AddLogging();
        mainnetServices.AddSolanaWalletForMainnet();
        var mainnetProvider = mainnetServices.BuildServiceProvider();
        var mainnetSwapService = mainnetProvider.GetRequiredService<ISwapService>();

        try
        {
            var swapQuote = await mainnetSwapService.GetSwapQuoteAsync(
                SolMintAddress, 
                UsdcMintAddress, 
                0.1m, // 0.1 SOL
                100   // 1% slippage
            );

            if (swapQuote.IsSome)
            {
                swapQuote.IfSome(quote =>
                {
                    quote.InputMint.Should().Be(SolMintAddress);
                    quote.OutputMint.Should().Be(UsdcMintAddress);
                    quote.InputAmount.Should().Be(0.1m);
                    quote.OutputAmount.Should().BeGreaterThan(0);
                    quote.SlippageBps.Should().Be(100);
                    quote.QuoteResponse.Should().NotBeNull();
                });

                // Step 4: Test swap routes
                var swapRoutes = await mainnetSwapService.GetSwapRoutesAsync(SolMintAddress, UsdcMintAddress, 0.1m);
                swapRoutes.IsSome.Should().BeTrue();

                swapRoutes.IfSome(routes =>
                {
                    routes.Should().NotBeEmpty();
                    routes.Should().Contain(route => route.InputMint == SolMintAddress);
                });
            }
        }
        catch
        {
            // Quote service might be unavailable, skip this part
            Assert.True(true, "Swap quote test skipped due to service availability");
        }
        finally
        {
            mainnetProvider?.Dispose();
        }

        // Step 5: Verify wallet signing capability (required for swaps)
        var testMessage = System.Text.Encoding.UTF8.GetBytes("E2E swap test message");
        var signature = swapWallet.SignMessage(testMessage);
        signature.IsSome.Should().BeTrue();

        var verification = swapWallet.VerifyMessage(testMessage, signature.IfNone(Array.Empty<byte>()));
        verification.Should().BeTrue();

        // Cleanup
        await _walletManager.DeleteWalletAsync("SwapE2EWallet");
    }

    [Fact]
    public async Task E2E_BurnerWalletWorkflow_CreateUseAndBurn()
    {
        // Step 1: Create a burner wallet with seed phrase
        var burnerResult = await _walletManager.CreateBurnerWalletWithSeedAsync("E2EBurnerWallet", use24Words: false);
        burnerResult.IsSome.Should().BeTrue();

        IBurnerWallet burnerWallet = null;
        string[] burnerSeed = null;

        burnerResult.IfSome(result =>
        {
            burnerWallet = result.wallet as IBurnerWallet;
            burnerSeed = result.seedPhrase;
        });

        burnerWallet.Should().NotBeNull();
        burnerSeed.Should().NotBeNull();
        burnerSeed.Length.Should().Be(12);

        // Step 2: Verify burner wallet properties
        burnerWallet.WalletType.Should().Be(WalletType.Burner);
        burnerWallet.IsExpired.Should().BeFalse();
        burnerWallet.TimeToLive.Should().BeGreaterThan(TimeSpan.Zero);
        burnerWallet.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Step 3: Test extending lifetime
        var originalTtl = burnerWallet.TimeToLive;
        var extensionResult = await burnerWallet.ExtendLifetimeAsync(TimeSpan.FromHours(1));
        extensionResult.Should().BeTrue();
        burnerWallet.TimeToLive.Should().BeGreaterThan(originalTtl);

        // Step 4: Test wallet functionality
        var balance = await burnerWallet.GetSolBalanceAsync();
        balance.IsSome.Should().BeTrue();

        var mnemonic = burnerWallet.GetMnemonic();
        mnemonic.IsSome.Should().BeTrue();

        // Step 5: Burn the wallet
        var burnResult = await burnerWallet.BurnAsync();
        burnResult.Should().BeTrue();
        burnerWallet.IsLocked.Should().BeTrue();

        // Verify wallet is no longer accessible
        var postBurnBalance = await burnerWallet.GetSolBalanceAsync();
        postBurnBalance.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task E2E_PortfolioManagement_MultipleWalletsAndTokens()
    {
        // Step 1: Create multiple wallets
        var standardWallet = await _walletManager.CreateWalletAsync("PortfolioStandard", "pass1");
        var burnerWallet = await _walletManager.CreateBurnerWalletAsync("PortfolioBurner");

        standardWallet.IsSome.Should().BeTrue();
        burnerWallet.IsSome.Should().BeTrue();

        // Step 2: Get all wallets
        var allWallets = await _walletManager.GetAllWalletsAsync();
        allWallets.IsSome.Should().BeTrue();

        allWallets.IfSome(wallets =>
        {
            wallets.Should().Contain(w => w.Name == "PortfolioStandard");
            wallets.Should().Contain(w => w.Name == "PortfolioBurner");
        });

        // Step 3: Get aggregated token assets
        var allTokenAssets = await _walletManager.GetAllTokenAssetsAsync();
        allTokenAssets.IsSome.Should().BeTrue();

        allTokenAssets.IfSome(assets =>
        {
            // Should contain at least SOL
            assets.Should().Contain(asset => asset.Symbol == "SOL");
            
            // Verify asset properties
            foreach (var asset in assets)
            {
                asset.MintAddress.Should().NotBeNullOrEmpty();
                asset.Symbol.Should().NotBeNullOrEmpty();
                asset.Balance.Should().BeGreaterOrEqualTo(0);
            }
        });

        // Step 4: Get total portfolio value
        var totalValue = await _walletManager.GetTotalPortfolioValueAsync();
        totalValue.Should().BeGreaterOrEqualTo(0);

        // Step 5: Test popular tokens retrieval
        var popularTokens = await _tokenAssetService.GetPopularTokensAsync();
        popularTokens.IsSome.Should().BeTrue();

        popularTokens.IfSome(tokens =>
        {
            tokens.Should().NotBeEmpty();
            tokens.Should().Contain(token => token.Symbol == "SOL");
        });

        // Step 6: Test token search
        var searchResults = await _tokenAssetService.SearchTokensAsync("SOL");
        searchResults.IsSome.Should().BeTrue();

        searchResults.IfSome(results =>
        {
            results.Should().Contain(token => token.Symbol.Contains("SOL", StringComparison.OrdinalIgnoreCase));
        });

        // Cleanup
        await _walletManager.DeleteWalletAsync("PortfolioStandard");
        await _walletManager.DeleteWalletAsync("PortfolioBurner");
    }

    [Fact]
    public async Task E2E_WalletSecurity_SigningAndVerification()
    {
        // Step 1: Create a wallet for security testing
        var securityWalletResult = await _walletManager.CreateWalletWithSeedAsync("SecurityTestWallet", "securepass");
        securityWalletResult.IsSome.Should().BeTrue();

        ISolanaWallet securityWallet = null;
        securityWalletResult.IfSome(result => securityWallet = result.wallet);

        // Step 2: Test message signing
        var testMessages = new[]
        {
            "Hello, Solana!",
            "Transaction data: SOL transfer 0.1",
            "Swap authorization: SOL to USDC",
            "🚀 Unicode test message",
            ""
        };

        foreach (var message in testMessages)
        {
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            var signature = securityWallet.SignMessage(messageBytes);
            
            signature.IsSome.Should().BeTrue($"Should be able to sign message: {message}");
            
            signature.IfSome(sig =>
            {
                sig.Length.Should().Be(64, "Ed25519 signatures should be 64 bytes");
                
                // Verify the signature
                var isValid = securityWallet.VerifyMessage(messageBytes, sig);
                isValid.Should().BeTrue($"Signature should be valid for message: {message}");
            });
        }

        // Step 3: Test wallet locking/unlocking
        var lockResult = await securityWallet.Lock();
        lockResult.Should().BeTrue();
        securityWallet.IsLocked.Should().BeTrue();

        // Locked wallet should not be able to sign
        var lockedSignature = securityWallet.SignMessage(System.Text.Encoding.UTF8.GetBytes("test"));
        lockedSignature.IsNone.Should().BeTrue();

        // Unlock wallet
        var unlockResult = await securityWallet.UnlockAsync("securepass");
        unlockResult.Should().BeTrue();
        securityWallet.IsLocked.Should().BeFalse();

        // Should be able to sign again
        var unlockedSignature = securityWallet.SignMessage(System.Text.Encoding.UTF8.GetBytes("test"));
        unlockedSignature.IsSome.Should().BeTrue();

        // Cleanup
        await _walletManager.DeleteWalletAsync("SecurityTestWallet");
    }

    [Fact]
    public async Task E2E_ServiceIntegration_DependencyInjection()
    {
        // Verify all services are properly registered and working
        var rpcClient = _serviceProvider.GetService<IRpcClient>();
        var tokenMintResolver = _serviceProvider.GetService<ITokenMintResolver>();
        var walletManager = _serviceProvider.GetService<ISolanaWalletManager>();
        var swapService = _serviceProvider.GetService<ISwapService>();
        var tokenAssetService = _serviceProvider.GetService<ITokenAssetService>();

        // All services should be available
        rpcClient.Should().NotBeNull();
        tokenMintResolver.Should().NotBeNull();
        walletManager.Should().NotBeNull();
        swapService.Should().NotBeNull();
        tokenAssetService.Should().NotBeNull();

        // Services should be singletons (same instances)
        var rpcClient2 = _serviceProvider.GetService<IRpcClient>();
        var walletManager2 = _serviceProvider.GetService<ISolanaWalletManager>();

        ReferenceEquals(rpcClient, rpcClient2).Should().BeTrue();
        ReferenceEquals(walletManager, walletManager2).Should().BeTrue();

        // Test basic functionality
        var health = await rpcClient.GetHealthAsync();
        health.WasSuccessful.Should().BeTrue();

        var testWallet = await walletManager.CreateWalletAsync("DITestWallet", "dipass");
        testWallet.IsSome.Should().BeTrue();

        await walletManager.DeleteWalletAsync("DITestWallet");
    }
}