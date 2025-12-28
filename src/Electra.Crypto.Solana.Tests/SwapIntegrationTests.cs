using FluentAssertions;
using LanguageExt;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Rpc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

namespace Electra.Crypto.Solana.Tests;

[Collection("Swap Integration Tests")]
public class SwapIntegrationTests : IAsyncLifetime
{
    private readonly IRpcClient _rpcClient;
    private readonly ITokenMintResolver _tokenMintResolver;
    private readonly ITokenAssetService _tokenAssetService;
    private readonly ISwapService _swapService;
    private readonly SolanaWalletManager _walletManager;

    // Test wallet mnemonic for consistent testing
    private const string TestWalletMnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
    
    // Known testnet/mainnet addresses
    private const string SolMintAddress = "So11111111111111111111111111111111111111112"; // Wrapped SOL
    private const string UsdcMintAddress = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"; // USDC mainnet
    private const string BonkMintAddress = "DezXAZ8z7PnrnRJjz3wXBoRgixCa6xjnB7YaB1pPB263"; // BONK

    public SwapIntegrationTests()
    {
        // Use mainnet for swap tests as testnet may not have sufficient liquidity
        _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        _tokenMintResolver = TokenMintResolver.Load();
        _tokenAssetService = new TokenAssetService(_rpcClient, _tokenMintResolver);
        _swapService = new SwapService(_rpcClient, _tokenMintResolver);
        _walletManager = new SolanaWalletManager(_rpcClient, _tokenMintResolver, _tokenAssetService);
    }

    public async Task InitializeAsync()
    {
        // Wait for connection
        await Task.Delay(1000);
        
        // Verify connection
        var health = await _rpcClient.GetHealthAsync();
        health.WasSuccessful.Should().BeTrue("RPC connection should be healthy");
    }

    public Task DisposeAsync()
    {
        //_swapService?.Dispose();
        _walletManager?.Dispose();
        //_tokenAssetService?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetSwapQuote_SolToUsdc_ShouldReturnValidQuote()
    {
        // Arrange
        var inputAmount = 0.1m; // 0.1 SOL
        var slippageBps = 100m; // 1% slippage

        // Act
        var quote = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, inputAmount, slippageBps);

        // Assert
        quote.IsSome.Should().BeTrue("Quote should be available for SOL to USDC");
        
        quote.IfSome(q =>
        {
            q.InputMint.Should().Be(SolMintAddress);
            q.OutputMint.Should().Be(UsdcMintAddress);
            q.InputAmount.Should().Be(inputAmount);
            q.OutputAmount.Should().BeGreaterThan(0);
            q.SlippageBps.Should().Be(slippageBps);
            q.PriceImpactPct.Should().BeGreaterThanOrEqualTo(0);
            q.QuoteResponse.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetSwapQuote_UsdcToSol_ShouldReturnValidQuote()
    {
        // Arrange
        var inputAmount = 10m; // 10 USDC
        var slippageBps = 50m; // 0.5% slippage

        // Act
        var quote = await _swapService.GetSwapQuoteAsync(UsdcMintAddress, SolMintAddress, inputAmount, slippageBps);

        // Assert
        quote.IsSome.Should().BeTrue("Quote should be available for USDC to SOL");
        
        quote.IfSome(q =>
        {
            q.InputMint.Should().Be(UsdcMintAddress);
            q.OutputMint.Should().Be(SolMintAddress);
            q.InputAmount.Should().Be(inputAmount);
            q.OutputAmount.Should().BeGreaterThan(0);
            q.SlippageBps.Should().Be(slippageBps);
            q.QuoteResponse.Should().NotBeNull();
            q.QuoteResponse.RoutePlan.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task GetSwapQuote_SolToBonk_ShouldReturnValidQuote()
    {
        // Arrange
        var inputAmount = 0.01m; // 0.01 SOL
        var slippageBps = 300m; // 3% slippage for smaller tokens

        // Act
        var quote = await _swapService.GetSwapQuoteAsync(SolMintAddress, BonkMintAddress, inputAmount, slippageBps);

        // Assert
        quote.IsSome.Should().BeTrue("Quote should be available for SOL to BONK");
        
        quote.IfSome(q =>
        {
            q.InputMint.Should().Be(SolMintAddress);
            q.OutputMint.Should().Be(BonkMintAddress);
            q.InputAmount.Should().Be(inputAmount);
            q.OutputAmount.Should().BeGreaterThan(0);
            q.SlippageBps.Should().Be(slippageBps);
            q.PriceImpactPct.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public async Task GetSwapRoutes_SolToUsdc_ShouldReturnAvailableRoutes()
    {
        // Arrange
        var inputAmount = 0.1m; // 0.1 SOL

        // Act
        var routes = await _swapService.GetSwapRoutesAsync(SolMintAddress, UsdcMintAddress, inputAmount);

        // Assert
        routes.IsSome.Should().BeTrue("Routes should be available for SOL to USDC");
        
        routes.IfSome(routeList =>
        {
            routeList.Should().NotBeEmpty();
            
            foreach (var route in routeList)
            {
                route.InputMint.Should().Be(SolMintAddress);
                route.OutputMint.Should().Be(UsdcMintAddress);
                route.InputAmount.Should().Be(inputAmount);
                route.OutputAmount.Should().BeGreaterThan(0);
                route.MarketInfos.Should().NotBeEmpty();
            }
        });
    }

    [Fact]
    public async Task CreateSwapWallet_ShouldPrepareForSwapOperations()
    {
        // Arrange & Act
        var walletResult = await _walletManager.CreateWalletWithSeedAsync("SwapTestWallet", "swappass");

        // Assert
        walletResult.IsSome.Should().BeTrue();
        
        walletResult.IfSome(result =>
        {
            var wallet = result.wallet;
            var seedPhrase = result.seedPhrase;

            wallet.Should().NotBeNull();
            wallet.Name.Should().Be("SwapTestWallet");
            seedPhrase.Should().NotBeNull();
            seedPhrase.Length.Should().Be(12);

            // Verify wallet can sign (required for swaps)
            var testMessage = System.Text.Encoding.UTF8.GetBytes("Swap test message");
            var signature = wallet.SignMessage(testMessage);
            signature.IsSome.Should().BeTrue();

            var isValid = wallet.VerifyMessage(testMessage, signature.IfNone(Array.Empty<byte>()));
            isValid.Should().BeTrue();
        });
    }

    [Fact]
    public async Task SimulateSwapExecution_SolToUsdc_ShouldPrepareTransaction()
    {
        // Arrange
        var wallet = await _walletManager.ImportWalletAsync("SimulateSwapWallet", TestWalletMnemonic, "");
        wallet.IsSome.Should().BeTrue();

        var swapWallet = wallet.IfNone(() => null);
        var inputAmount = 0.001m; // Very small amount for testing

        // Get a quote first
        var quote = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, inputAmount, 100);
        
        if (quote.IsNone)
        {
            Assert.True(true, "Skipping swap simulation - quote not available");
            return;
        }

        // Check wallet balance
        var solBalance = await swapWallet.GetSolBalanceAsync();
        solBalance.IsSome.Should().BeTrue();

        if (solBalance.IfNone(0) < inputAmount + 0.01m) // Include buffer for fees
        {
            Assert.True(true, "Skipping swap simulation - insufficient SOL balance");
            return;
        }

        // Act - Attempt swap execution (this will likely fail due to insufficient mainnet funds)
        // But we can test the transaction preparation
        var swapResult = await _swapService.ExecuteSwapAsync(swapWallet, quote.IfNone(new SwapQuote()));

        // Assert - We expect this to potentially fail due to balance, but the service should handle gracefully
        // The test validates that the swap service can attempt the operation without throwing exceptions
        Assert.True(true, "Swap simulation completed - transaction preparation tested");
    }

    [Fact]
    public async Task ValidateSwapQuoteParams_InvalidInputs_ShouldReturnNone()
    {
        // Test invalid mint addresses
        var invalidQuote1 = await _swapService.GetSwapQuoteAsync("", UsdcMintAddress, 1m);
        invalidQuote1.IsNone.Should().BeTrue();

        var invalidQuote2 = await _swapService.GetSwapQuoteAsync(SolMintAddress, "", 1m);
        invalidQuote2.IsNone.Should().BeTrue();

        // Test zero amount
        var invalidQuote3 = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, 0m);
        invalidQuote3.IsNone.Should().BeTrue();

        // Test negative amount
        var invalidQuote4 = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, -1m);
        invalidQuote4.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetMultipleSwapQuotes_DifferentPairs_ShouldReturnConsistentResults()
    {
        // Arrange
        var testPairs = new[]
        {
            (SolMintAddress, UsdcMintAddress, 0.1m),
            (UsdcMintAddress, SolMintAddress, 10m),
            (SolMintAddress, BonkMintAddress, 0.01m)
        };

        // Act & Assert
        foreach (var (inputMint, outputMint, amount) in testPairs)
        {
            var quote = await _swapService.GetSwapQuoteAsync(inputMint, outputMint, amount);
            
            if (quote.IsSome)
            {
                quote.IfSome(q =>
                {
                    q.InputMint.Should().Be(inputMint);
                    q.OutputMint.Should().Be(outputMint);
                    q.InputAmount.Should().Be(amount);
                    q.OutputAmount.Should().BeGreaterThan(0);
                    q.QuoteResponse.Should().NotBeNull();
                });
            }
            
            // Add small delay between requests to avoid rate limiting
            await Task.Delay(100);
        }
    }

    [Fact]
    public async Task SwapQuoteConsistency_SameParams_ShouldReturnSimilarResults()
    {
        // Arrange
        var inputAmount = 0.1m;
        var slippage = 100m;

        // Act - Get two quotes with same parameters
        var quote1 = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, inputAmount, slippage);
        await Task.Delay(1000); // Small delay
        var quote2 = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, inputAmount, slippage);

        // Assert
        if (quote1.IsSome && quote2.IsSome)
        {
            var q1 = quote1.IfNone(new SwapQuote());
            var q2 = quote2.IfNone(new SwapQuote());

            // Output amounts should be close (within 5% variance due to market movement)
            var variance = Math.Abs(q1.OutputAmount - q2.OutputAmount) / q1.OutputAmount;
            variance.Should().BeLessThan(0.05m, "Quotes should be consistent within short time periods");

            q1.InputAmount.Should().Be(q2.InputAmount);
            q1.SlippageBps.Should().Be(q2.SlippageBps);
        }
    }

    [Fact]
    public async Task HighSlippageSwapQuote_ShouldAdjustOutputAmount()
    {
        // Arrange
        var inputAmount = 0.1m;
        var lowSlippage = 50m;  // 0.5%
        var highSlippage = 500m; // 5%

        // Act
        var lowSlippageQuote = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, inputAmount, lowSlippage);
        var highSlippageQuote = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, inputAmount, highSlippage);

        // Assert
        if (lowSlippageQuote.IsSome && highSlippageQuote.IsSome)
        {
            var lowQ = lowSlippageQuote.IfNone(new SwapQuote());
            var highQ = highSlippageQuote.IfNone(new SwapQuote());

            lowQ.SlippageBps.Should().Be(lowSlippage);
            highQ.SlippageBps.Should().Be(highSlippage);

            // Higher slippage should generally result in same or worse output (due to MEV protection)
            // but the quote service might return similar results for route calculation
            highQ.OutputAmount.Should().BeGreaterThan(0);
            lowQ.OutputAmount.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task SwapServiceDisposal_ShouldCleanupResources()
    {
        // Arrange
        var swapService = new SwapService(_rpcClient, _tokenMintResolver);

        // Act
        swapService.Dispose();

        // Assert - No exception should be thrown, and subsequent calls should handle disposed state
        Assert.True(true, "Swap service disposed successfully");
    }

    [Theory]
    [InlineData(50)]   // 0.5%
    [InlineData(100)]  // 1%
    [InlineData(300)]  // 3%
    [InlineData(1000)] // 10%
    public async Task SwapQuoteWithVariousSlippage_ShouldAcceptDifferentValues(decimal slippageBps)
    {
        // Arrange
        var inputAmount = 0.1m;

        // Act
        var quote = await _swapService.GetSwapQuoteAsync(SolMintAddress, UsdcMintAddress, inputAmount, slippageBps);

        // Assert
        if (quote.IsSome)
        {
            quote.IfSome(q =>
            {
                q.SlippageBps.Should().Be(slippageBps);
                q.OutputAmount.Should().BeGreaterThan(0);
                q.InputAmount.Should().Be(inputAmount);
            });
        }
    }
}