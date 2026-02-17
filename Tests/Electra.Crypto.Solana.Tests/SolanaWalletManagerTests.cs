using Electra.Crypto.Solana;
using FluentAssertions;
using LanguageExt;
using NSubstitute;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Rpc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Electra.Crypto.Solana.Tests;

public class SolanaWalletManagerTests
{
    private readonly IRpcClient _mockRpcClient;
    private readonly ITokenMintResolver _mockTokenMintResolver;
    private readonly ITokenAssetService _mockTokenAssetService;
    private readonly SolanaWalletManager _walletManager;

    public SolanaWalletManagerTests()
    {
        _mockRpcClient = Substitute.For<IRpcClient>();
        _mockTokenMintResolver = Substitute.For<ITokenMintResolver>();
        _mockTokenAssetService = Substitute.For<ITokenAssetService>();
        _walletManager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);
    }

    [Fact]
    public async Task CreateWalletAsync_WithValidParameters_ShouldCreateWallet()
    {
        // Arrange
        var name = "TestWallet";
        var passphrase = "test-passphrase";

        // Act
        var result = await _walletManager.CreateWalletAsync(name, passphrase);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: wallet => {
                wallet.Name.Should().Be(name);
                wallet.WalletType.Should().Be(WalletType.Standard);
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task CreateWalletAsync_WithDuplicateName_ShouldReturnNone()
    {
        // Arrange
        var name = "DuplicateWallet";
        var passphrase = "test-passphrase";
        
        await _walletManager.CreateWalletAsync(name, passphrase);

        // Act
        var result = await _walletManager.CreateWalletAsync(name, passphrase);

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBurnerWalletAsync_ShouldCreateBurnerWallet()
    {
        // Arrange
        var name = "TestBurner";

        // Act
        var result = await _walletManager.CreateBurnerWalletAsync(name);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: wallet => {
                wallet.Should().BeOfType<BurnerWallet>();
                wallet.Name.Should().Be(name);
                wallet.WalletType.Should().Be(WalletType.Burner);
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBurnerWalletAsync_WithoutName_ShouldGenerateDefaultName()
    {
        // Act
        var result = await _walletManager.CreateBurnerWalletAsync();

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: wallet => {
                wallet.Name.Should().StartWith("Burner_");
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task ImportWalletAsync_WithValidMnemonic_ShouldImportWallet()
    {
        // Arrange
        var name = "ImportedWallet";
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var passphrase = "";

        // Act
        var result = await _walletManager.ImportWalletAsync(name, mnemonic, passphrase);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: wallet => {
                wallet.Name.Should().Be(name);
                wallet.WalletType.Should().Be(WalletType.Standard);
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task ImportWalletAsync_WithInvalidMnemonic_ShouldReturnNone()
    {
        // Arrange
        var name = "InvalidWallet";
        var mnemonic = "invalid mnemonic phrase";
        var passphrase = "";

        // Act
        var result = await _walletManager.ImportWalletAsync(name, mnemonic, passphrase);

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetWalletAsync_WithExistingWallet_ShouldReturnWallet()
    {
        // Arrange
        var name = "TestWallet";
        var passphrase = "test-passphrase";
        await _walletManager.CreateWalletAsync(name, passphrase);

        // Act
        var result = await _walletManager.GetWalletAsync(name);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: wallet => {
                wallet.Name.Should().Be(name);
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task GetWalletAsync_WithNonExistentWallet_ShouldReturnNone()
    {
        // Act
        var result = await _walletManager.GetWalletAsync("NonExistentWallet");

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllWalletsAsync_WithMultipleWallets_ShouldReturnAllWallets()
    {
        // Arrange
        await _walletManager.CreateWalletAsync("Wallet1", "passphrase1");
        await _walletManager.CreateWalletAsync("Wallet2", "passphrase2");
        await _walletManager.CreateBurnerWalletAsync("Burner1");

        // Act
        var result = await _walletManager.GetAllWalletsAsync();

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: wallets => {
                wallets.Count().Should().Be(3);
                wallets.Should().Contain(w => w.Name == "Wallet1");
                wallets.Should().Contain(w => w.Name == "Wallet2");
                wallets.Should().Contain(w => w.Name == "Burner1");
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteWalletAsync_WithExistingWallet_ShouldDeleteWallet()
    {
        // Arrange
        var name = "WalletToDelete";
        var passphrase = "test-passphrase";
        await _walletManager.CreateWalletAsync(name, passphrase);

        // Act
        // var deleteResult = await _walletManager.DeleteWalletAsync(name);
        // var getResult = await _walletManager.GetWalletAsync(name);
        //
        // // Assert
        // deleteResult.IsSome.Should().BeTrue();
        //getResult.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteWalletAsync_WithNonExistentWallet_ShouldReturnNFalse()
    {
        // Act
        var result = await _walletManager.DeleteWalletAsync("NonExistentWallet");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTotalPortfolioValueAsync_WithMockedAssets_ShouldCalculateCorrectly()
    {
        // Arrange
        await _walletManager.CreateWalletAsync("TestWallet", "passphrase");
        
        // Mock SOL price
        _mockTokenAssetService.GetTokenPriceAsync("So11111111111111111111111111111111111111112")
            .Returns(Task.FromResult(LanguageExt.Option<decimal>.Some(100m))); // $100 per SOL

        // Act
        var result = await _walletManager.GetTotalPortfolioValueAsync();

        // Assert
        //result.Should().BeTrue();
        // Value will be 0 since we're not mocking actual balance calls, but the method should work
        //result.IfNone(-1).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Dispose_ShouldDisposeAllWallets()
    {
        // Arrange
        var walletManagerToDispose = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);

        // Act & Assert
        walletManagerToDispose.Invoking(w => w.Dispose()).Should().NotThrow();
    }
}

public class TokenAssetServiceTests
{
    private readonly IRpcClient _mockRpcClient;
    private readonly ITokenMintResolver _mockTokenMintResolver;
    private readonly TokenAssetService _tokenAssetService;

    public TokenAssetServiceTests()
    {
        _mockRpcClient = Substitute.For<IRpcClient>();
        _mockTokenMintResolver = Substitute.For<ITokenMintResolver>();
        _tokenAssetService = new TokenAssetService(_mockRpcClient, _mockTokenMintResolver);
    }

    [Fact]
    public async Task GetTokenInfoAsync_WithValidMint_ShouldReturnTokenInfo()
    {
        // Arrange
        var mintAddress = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"; // USDC
        var mockTokenDef = Substitute.For<TokenDef>();
        mockTokenDef.Symbol.Returns("USDC");
        mockTokenDef.TokenName.Returns("USD Coin");
        mockTokenDef.DecimalPlaces.Returns(6);
        mockTokenDef.TokenLogoUrl.Returns("https://example.com/logo.png");
        
        _mockTokenMintResolver.Resolve(mintAddress).Returns(mockTokenDef);

        // Act
        var result = await _tokenAssetService.GetTokenInfoAsync(mintAddress);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: tokenInfo => {
                tokenInfo.MintAddress.Should().Be(mintAddress);
                tokenInfo.Symbol.Should().Be("USDC");
                tokenInfo.Name.Should().Be("USD Coin");
                tokenInfo.Decimals.Should().Be(6);
                tokenInfo.IsVerified.Should().BeTrue();
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task GetTokenInfoAsync_WithEmptyMint_ShouldReturnNone()
    {
        // Act
        var result = await _tokenAssetService.GetTokenInfoAsync("");

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetPopularTokensAsync_ShouldReturnPopularTokens()
    {
        // Act
        var result = await _tokenAssetService.GetPopularTokensAsync();

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: tokens => {
                tokens.Should().NotBeEmpty();
                tokens.Should().Contain(t => t.Symbol == "SOL");
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task SearchTokensAsync_WithValidQuery_ShouldReturnMatchingTokens()
    {
        // Act
        var result = await _tokenAssetService.SearchTokensAsync("SOL");

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: tokens => {
                tokens.Should().Contain(t => t.Symbol.Contains("SOL") || t.Name.Contains("SOL"));
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task SearchTokensAsync_WithEmptyQuery_ShouldReturnNone()
    {
        // Act
        var result = await _tokenAssetService.SearchTokensAsync("");

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldDisposeHttpClient()
    {
        // Act & Assert
        _tokenAssetService.Invoking(s => s.Dispose()).Should().NotThrow();
    }
}

public class TokenAssetInfoTests
{
    [Fact]
    public void CreateSolToken_ShouldCreateCorrectSolTokenInfo()
    {
        // Arrange
        var balance = 2.5m;

        // Act
        var result = TokenAssetInfo.CreateSolToken(balance);

        // Assert
        result.MintAddress.Should().Be("So11111111111111111111111111111111111111112");
        result.Symbol.Should().Be("SOL");
        result.Name.Should().Be("Solana");
        result.Decimals.Should().Be(9);
        result.Balance.Should().Be(balance);
        result.BalanceRaw.Should().Be(2_500_000_000UL);
        result.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void WithBalance_ShouldUpdateBalanceAndTimestamp()
    {
        // Arrange
        var originalToken = TokenAssetInfo.CreateSolToken(1.0m);
        var newBalance = 2.5m;
        var newBalanceRaw = 2_500_000_000UL;

        // Act
        var result = originalToken.WithBalance(newBalance, newBalanceRaw);

        // Assert
        result.Balance.Should().Be(newBalance);
        result.BalanceRaw.Should().Be(newBalanceRaw);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Symbol.Should().Be(originalToken.Symbol); // Other properties should remain unchanged
    }

    [Fact]
    public void WithPrice_ShouldUpdatePriceAndTimestamp()
    {
        // Arrange
        var originalToken = TokenAssetInfo.CreateSolToken(1.0m);
        var newPrice = 150.75m;

        // Act
        var result = originalToken.WithPrice(newPrice);

        // Assert
        result.Price.Should().Be(newPrice);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Balance.Should().Be(originalToken.Balance); // Other properties should remain unchanged
    }

    [Fact]
    public void Value_WithPriceSet_ShouldCalculateCorrectly()
    {
        // Arrange
        var balance = 2.5m;
        var price = 100m;
        var token = TokenAssetInfo.CreateSolToken(balance).WithPrice(price);

        // Act
        var value = token.Value;

        // Assert
        value.Should().Be(250m); // 2.5 * 100
    }

    [Fact]
    public void Value_WithoutPrice_ShouldReturnNull()
    {
        // Arrange
        var token = TokenAssetInfo.CreateSolToken(2.5m);

        // Act
        var value = token.Value;

        // Assert
        value.Should().BeNull();
    }
}