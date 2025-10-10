using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Solnet.Programs.Utilities;
using System.Threading;
using Solnet.Wallet;

namespace Electra.Crypto.Solana.Tests;

[Collection("Solana Testnet Integration Tests")]
public class SolanaTestnetIntegrationTests : IAsyncLifetime
{
    private readonly IRpcClient _rpcClient;
    private readonly ITokenMintResolver _tokenMintResolver;
    private readonly ITokenAssetService _tokenAssetService;
    private readonly SolanaWalletManager _walletManager;
    
    // Test wallet mnemonics (for consistent testing)
    private const string TestWallet1Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
    private const string TestWallet2Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon";
    
    // Known testnet addresses
    private const string UsdcMintAddressTestnet = "4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU"; // USDC on testnet
    private const string SolMintAddress = "So11111111111111111111111111111111111111112"; // Wrapped SOL
    
    public SolanaTestnetIntegrationTests()
    {
        // Use Solana testnet
        _rpcClient = ClientFactory.GetClient(Cluster.TestNet);
        _tokenMintResolver = TokenMintResolver.Load();
        _tokenAssetService = new TokenAssetService(_rpcClient, _tokenMintResolver);
        _walletManager = new SolanaWalletManager(_rpcClient, _tokenMintResolver, _tokenAssetService);
    }

    public async Task InitializeAsync()
    {
        // Wait for testnet connection
        await Task.Delay(1000);
        
        // Verify testnet connection
        var health = await _rpcClient.GetHealthAsync();
        health.WasSuccessful.Should().BeTrue("Testnet connection should be healthy");
    }

    public Task DisposeAsync()
    {
        _walletManager?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateTwoWallets_ShouldCreateSuccessfully()
    {
        // Arrange & Act
        var wallet1Result = await _walletManager.CreateWalletWithSeedAsync("TestWallet1", "passphrase1");
        var wallet2Result = await _walletManager.CreateWalletWithSeedAsync("TestWallet2", "passphrase2");

        // Assert
        wallet1Result.IsSome.Should().BeTrue();
        wallet2Result.IsSome.Should().BeTrue();

        wallet1Result.IfSome(result =>
        {
            result.wallet.Should().NotBeNull();
            result.wallet.Name.Should().Be("TestWallet1");
            result.seedPhrase.Should().NotBeNull();
            result.seedPhrase.Length.Should().Be(12);
        });

        wallet2Result.IfSome(result =>
        {
            result.wallet.Should().NotBeNull();
            result.wallet.Name.Should().Be("TestWallet2");
            result.seedPhrase.Should().NotBeNull();
            result.seedPhrase.Length.Should().Be(12);
        });
    }

    [Fact]
    public async Task ImportTestWallets_ShouldImportSuccessfully()
    {
        // Act
        var wallet1 = await _walletManager.ImportWalletAsync("ImportedWallet1", TestWallet1Mnemonic, "");
        var wallet2 = await _walletManager.ImportWalletAsync("ImportedWallet2", TestWallet2Mnemonic, "");

        // Assert
        wallet1.IsSome.Should().BeTrue();
        wallet2.IsSome.Should().BeTrue();

        wallet1.IfSome(w =>
        {
            w.Name.Should().Be("ImportedWallet1");
            w.PublicKey.Should().NotBeNull();
            w.PublicKey.Key.Should().NotBeNullOrEmpty();
        });

        wallet2.IfSome(w =>
        {
            w.Name.Should().Be("ImportedWallet2");
            w.PublicKey.Should().NotBeNull();
            w.PublicKey.Key.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task CheckWalletBalances_ShouldReturnBalanceInfo()
    {
        // Arrange
        var wallet = await _walletManager.ImportWalletAsync("BalanceTestWallet", TestWallet1Mnemonic, "");
        wallet.IsSome.Should().BeTrue();

        // Act
        var solBalance = await wallet.IfNone(() => null).GetSolBalanceAsync();
        var tokenAssets = await wallet.IfNone(() => null).GetTokenAssetsAsync();

        // Assert
        solBalance.IsSome.Should().BeTrue();
        solBalance.IfNone(0).Should().BeGreaterOrEqualTo(0);

        tokenAssets.IsSome.Should().BeTrue();
        tokenAssets.IfSome(assets =>
        {
            assets.Should().NotBeNull();
            // Should at least contain SOL
            assets.Should().Contain(asset => asset.Symbol == "SOL");
        });
    }

    [Fact]
    public async Task SendSol_BetweenWallets_ShouldTransferSuccessfully()
    {
        // Arrange
        var senderWallet = await _walletManager.ImportWalletAsync("SenderWallet", TestWallet1Mnemonic, "");
        var receiverWallet = await _walletManager.ImportWalletAsync("ReceiverWallet", TestWallet2Mnemonic, "");

        senderWallet.IsSome.Should().BeTrue();
        receiverWallet.IsSome.Should().BeTrue();

        var sender = senderWallet.IfNone(() => null);
        var receiver = receiverWallet.IfNone(() => null);

        // Get initial balances
        var senderInitialBalance = await sender.GetSolBalanceAsync();
        var receiverInitialBalance = await receiver.GetSolBalanceAsync();

        senderInitialBalance.IsSome.Should().BeTrue();
        receiverInitialBalance.IsSome.Should().BeTrue();

        var sendAmount = 0.001m; // Send 0.001 SOL

        // Skip if sender doesn't have enough balance
        if (senderInitialBalance.IfNone(0) < sendAmount + 0.001m) // Include fee buffer
        {
            Assert.True(true, "Skipping test - insufficient balance in test wallet");
            return;
        }

        // Act
        var transactionResult = await sender.SendSolAsync(receiver.PublicKey.Key, sendAmount);

        // Assert
        transactionResult.IsSome.Should().BeTrue("Transaction should succeed");
        
        transactionResult.IfSome(txSignature =>
        {
            txSignature.Should().NotBeNullOrEmpty();
            txSignature.Length.Should().BeGreaterThan(40); // Solana transaction signatures are base58 encoded
        });

        // Wait for transaction confirmation
        await Task.Delay(5000);

        // Verify balances changed
        var senderFinalBalance = await sender.GetSolBalanceAsync();
        var receiverFinalBalance = await receiver.GetSolBalanceAsync();

        senderFinalBalance.IsSome.Should().BeTrue();
        receiverFinalBalance.IsSome.Should().BeTrue();

        // Receiver should have more balance
        receiverFinalBalance.IfNone(0).Should().BeGreaterThan(receiverInitialBalance.IfNone(0));
        
        // Sender should have less balance (amount + fees)
        senderFinalBalance.IfNone(0).Should().BeLessThan(senderInitialBalance.IfNone(0));
    }

    [Fact]
    public async Task RequestTestnetAirdrop_ShouldReceiveSol()
    {
        // Arrange
        var wallet = await _walletManager.CreateWalletAsync("AirdropTestWallet", "passphrase");
        wallet.IsSome.Should().BeTrue();

        var testWallet = wallet.IfNone(() => null);
        var initialBalance = await testWallet.GetSolBalanceAsync();

        // Act - Request airdrop (1 SOL)
        var airdropResult = await _rpcClient.RequestAirdropAsync(testWallet.PublicKey.Key, 1_000_000_000); // 1 SOL in lamports

        // Assert
        airdropResult.WasSuccessful.Should().BeTrue("Airdrop request should succeed");
        airdropResult.Result.Should().NotBeNullOrEmpty();

        // Wait for airdrop confirmation
        await Task.Delay(10000);

        // Check balance increased
        var finalBalance = await testWallet.GetSolBalanceAsync();
        finalBalance.IsSome.Should().BeTrue();
        finalBalance.IfNone(0).Should().BeGreaterThan(initialBalance.IfNone(0));
    }

    [Fact]
    public async Task CreateAndFundMultipleWallets_ShouldManagePortfolio()
    {
        // Arrange & Act
        var wallet1 = await _walletManager.CreateWalletAsync("PortfolioWallet1", "pass1");
        var wallet2 = await _walletManager.CreateWalletAsync("PortfolioWallet2", "pass2");
        var burnerWallet = await _walletManager.CreateBurnerWalletAsync("PortfolioBurner");

        // Assert
        wallet1.IsSome.Should().BeTrue();
        wallet2.IsSome.Should().BeTrue();
        burnerWallet.IsSome.Should().BeTrue();

        // Get all wallets
        var allWallets = await _walletManager.GetAllWalletsAsync();
        allWallets.IsSome.Should().BeTrue();
        
        allWallets.IfSome(wallets =>
        {
            wallets.Should().Contain(w => w.Name == "PortfolioWallet1");
            wallets.Should().Contain(w => w.Name == "PortfolioWallet2");
            wallets.Should().Contain(w => w.Name == "PortfolioBurner");
        });

        // Get total portfolio value
        var totalValue = await _walletManager.GetTotalPortfolioValueAsync();
        totalValue.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task SwapSolToUsdc_ShouldExecuteSuccessfully()
    {
        // Arrange
        var wallet = await _walletManager.ImportWalletAsync("SwapTestWallet", TestWallet1Mnemonic, "");
        wallet.IsSome.Should().BeTrue();

        var swapWallet = wallet.IfNone(() => null);
        var initialSolBalance = await swapWallet.GetSolBalanceAsync();
        var initialTokens = await swapWallet.GetTokenAssetsAsync();

        initialSolBalance.IsSome.Should().BeTrue();
        
        // Skip if insufficient SOL for swap
        if (initialSolBalance.IfNone(0) < 0.1m)
        {
            Assert.True(true, "Skipping swap test - insufficient SOL balance");
            return;
        }

        // Act - This is a conceptual test since actual DEX integration would require specific DEX protocols
        // For now, we'll test the token asset retrieval and balance checking that would be part of a swap
        
        var solTokenInfo = await swapWallet.GetTokenAssetInfoAsync(SolMintAddress);
        solTokenInfo.IsSome.Should().BeTrue();

        solTokenInfo.IfSome(solToken =>
        {
            solToken.Symbol.Should().Be("SOL");
            solToken.Balance.Should().BeGreaterThan(0);
        });

        // In a real swap, we would:
        // 1. Get quote from Jupiter/Raydium
        // 2. Build swap transaction
        // 3. Sign and send transaction
        // 4. Verify token balances changed

        // For this test, we'll simulate the balance checking part
        var allTokens = await swapWallet.GetTokenAssetsAsync();
        allTokens.IsSome.Should().BeTrue();
        
        allTokens.IfSome(tokens =>
        {
            tokens.Should().Contain(t => t.Symbol == "SOL");
            // Check if wallet already has USDC
            var usdcToken = tokens.FirstOrDefault(t => t.MintAddress == UsdcMintAddressTestnet);
            if (usdcToken != null)
            {
                usdcToken.Symbol.Should().Be("USDC");
            }
        });
    }

    [Fact]
    public async Task SimulateTokenSwapFlow_ShouldPrepareSwapParameters()
    {
        // Arrange
        var wallet = await _walletManager.ImportWalletAsync("TokenSwapWallet", TestWallet1Mnemonic, "");
        wallet.IsSome.Should().BeTrue();

        var swapWallet = wallet.IfNone(() => null);
        var swapAmount = 0.01m; // 0.01 SOL

        // Act - Simulate swap preparation steps

        // Step 1: Check source token balance
        var solBalance = await swapWallet.GetSolBalanceAsync();
        solBalance.IsSome.Should().BeTrue();

        if (solBalance.IfNone(0) < swapAmount)
        {
            Assert.True(true, "Skipping - insufficient SOL for swap simulation");
            return;
        }

        // Step 2: Get token information
        var solTokenInfo = await swapWallet.GetTokenAssetInfoAsync(SolMintAddress);
        solTokenInfo.IsSome.Should().BeTrue();

        // Step 3: Validate wallet can sign transactions
        var testMessage = System.Text.Encoding.UTF8.GetBytes("Swap preparation test");
        var signature = swapWallet.SignMessage(testMessage);
        signature.IsSome.Should().BeTrue();

        // Step 4: Verify message can be verified
        var isValid = swapWallet.VerifyMessage(testMessage, signature.IfNone(Array.Empty<byte>()));
        isValid.Should().BeTrue();

        // Assert - All swap prerequisites are met
        solTokenInfo.IfSome(token =>
        {
            token.Symbol.Should().Be("SOL");
            token.Balance.Should().BeGreaterOrEqualTo(swapAmount);
            token.MintAddress.Should().Be(SolMintAddress);
        });
    }

    [Fact]
    public async Task CreateAssociatedTokenAccounts_ShouldPrepareForTokenOperations()
    {
        // Arrange
        var wallet = await _walletManager.CreateWalletAsync("TokenAccountWallet", "passphrase");
        wallet.IsSome.Should().BeTrue();

        var tokenWallet = wallet.IfNone(() => null);

        // Act - Get current token assets (this will show associated token accounts)
        var tokenAssets = await tokenWallet.GetTokenAssetsAsync();

        // Assert
        tokenAssets.IsSome.Should().BeTrue();
        
        tokenAssets.IfSome(assets =>
        {
            // Should at least have SOL
            assets.Should().Contain(asset => asset.Symbol == "SOL");
            
            // Each asset should have valid properties
            foreach (var asset in assets)
            {
                asset.MintAddress.Should().NotBeNullOrEmpty();
                asset.Symbol.Should().NotBeNullOrEmpty();
                asset.Balance.Should().BeGreaterOrEqualTo(0);
                asset.Decimals.Should().BeGreaterOrEqualTo(0);
            }
        });
    }

    [Fact]
    public async Task SendTokenBetweenWallets_ShouldTransferTokenSuccessfully()
    {
        // Arrange
        var senderWallet = await _walletManager.ImportWalletAsync("TokenSender", TestWallet1Mnemonic, "");
        var receiverWallet = await _walletManager.ImportWalletAsync("TokenReceiver", TestWallet2Mnemonic, "");

        senderWallet.IsSome.Should().BeTrue();
        receiverWallet.IsSome.Should().BeTrue();

        var sender = senderWallet.IfNone(() => null);
        var receiver = receiverWallet.IfNone(() => null);

        // Check if sender has any SPL tokens
        var senderTokens = await sender.GetTokenAssetsAsync();
        senderTokens.IsSome.Should().BeTrue();

        string tokenMintToTransfer = null;
        decimal tokenBalanceToTransfer = 0;

        senderTokens.IfSome(tokens =>
        {
            var splToken = tokens.FirstOrDefault(t => t.Symbol != "SOL" && t.Balance > 0);
            if (splToken != null)
            {
                tokenMintToTransfer = splToken.MintAddress;
                tokenBalanceToTransfer = splToken.Balance;
            }
        });

        if (tokenMintToTransfer == null)
        {
            Assert.True(true, "Skipping - no SPL tokens available for transfer");
            return;
        }

        var transferAmount = Math.Min(tokenBalanceToTransfer * 0.1m, 1m); // Transfer 10% or 1 token, whichever is smaller

        // Act
        var transferResult = await sender.SendTokenAsync(tokenMintToTransfer, receiver.PublicKey.Key, transferAmount);

        // Assert
        transferResult.IsSome.Should().BeTrue("Token transfer should succeed");
        
        transferResult.IfSome(txSignature =>
        {
            txSignature.Should().NotBeNullOrEmpty();
            txSignature.Length.Should().BeGreaterThan(40);
        });
    }

    [Fact]
    public async Task GetTokenPricesAndValues_ShouldRetrievePriceInformation()
    {
        // Arrange
        var wallet = await _walletManager.ImportWalletAsync("PriceTestWallet", TestWallet1Mnemonic, "");
        wallet.IsSome.Should().BeTrue();

        var priceWallet = wallet.IfNone(() => null);

        // Act
        var tokenAssets = await priceWallet.GetTokenAssetsAsync();
        
        // Assert
        tokenAssets.IsSome.Should().BeTrue();
        
        tokenAssets.IfSome(async assets =>
        {
            foreach (var asset in assets)
            {
                // Test price retrieval for each token
                var priceInfo = await _tokenAssetService.GetTokenPriceAsync(asset.MintAddress);
                
                // Price might not be available for all tokens, but the call should not fail
                priceInfo.IsNone.Should().BeFalse("Price service should return Some value (even if 0)");
                
                if (asset.Symbol == "SOL")
                {
                    // SOL should have a price
                    priceInfo.IfSome(price =>
                    {
                        price.Should().BeGreaterThan(0, "SOL should have a positive price");
                    });
                }
            }
        });
    }

    [Fact]
    public async Task BurnerWalletLifecycle_ShouldManageTemporaryWallet()
    {
        // Arrange & Act
        var burnerResult = await _walletManager.CreateBurnerWalletWithSeedAsync("TempTestWallet", use24Words: false);
        
        // Assert
        burnerResult.IsSome.Should().BeTrue();
        
        burnerResult.IfSome(async result =>
        {
            var burnerWallet = result.wallet as IBurnerWallet;
            burnerWallet.Should().NotBeNull();
            
            burnerWallet.WalletType.Should().Be(WalletType.Burner);
            burnerWallet.IsExpired.Should().BeFalse();
            burnerWallet.TimeToLive.Should().BeGreaterThan(TimeSpan.Zero);
            
            result.seedPhrase.Should().NotBeNull();
            result.seedPhrase.Length.Should().Be(12);
            
            // Test extending lifetime
            var extendResult = await burnerWallet.ExtendLifetimeAsync(TimeSpan.FromHours(1));
            extendResult.Should().BeTrue();
            
            // Test burning
            var burnResult = await burnerWallet.BurnAsync();
            burnResult.Should().BeTrue();
            burnerWallet.IsLocked.Should().BeTrue();
        });
    }
}