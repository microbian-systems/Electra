using FluentAssertions;
using LanguageExt;
using NSubstitute;
using Solnet.Extensions;
using Solnet.Rpc;
using Xunit;

namespace Electra.Crypto.Solana.Tests;

public class SeedPhraseTests
{
    private readonly IRpcClient _mockRpcClient;
    private readonly ITokenMintResolver _mockTokenMintResolver;
    private readonly ITokenAssetService _mockTokenAssetService;

    public SeedPhraseTests()
    {
        _mockRpcClient = Substitute.For<IRpcClient>();
        _mockTokenMintResolver = Substitute.For<ITokenMintResolver>();
        _mockTokenAssetService = Substitute.For<ITokenAssetService>();
    }

    [Fact]
    public void SolanaWallet_CreateWithSeed_Should_Return_12_Word_Seed_Phrase_By_Default()
    {
        // Arrange
        var walletName = "TestWallet";
        var passphrase = "testpass";

        // Act
        var result = SolanaWallet.CreateWithSeed(walletName, passphrase, _mockRpcClient, _mockTokenMintResolver);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(12);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });
    }

    [Fact]
    public void SolanaWallet_CreateWithSeed_Should_Return_24_Word_Seed_Phrase_When_Requested()
    {
        // Arrange
        var walletName = "TestWallet24";
        var passphrase = "testpass";

        // Act
        var result = SolanaWallet.CreateWithSeed(walletName, passphrase, _mockRpcClient, _mockTokenMintResolver, use24Words: true);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(24);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });
    }

    [Fact]
    public void BurnerWallet_CreateWithSeed_Should_Return_12_Word_Seed_Phrase_By_Default()
    {
        // Arrange
        var walletName = "TestBurnerWallet";

        // Act
        var result = BurnerWallet.CreateWithSeed(walletName, _mockRpcClient, _mockTokenMintResolver);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.wallet.WalletType.Should().Be(WalletType.Burner);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(12);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });
    }

    [Fact]
    public void BurnerWallet_CreateWithSeed_Should_Return_24_Word_Seed_Phrase_When_Requested()
    {
        // Arrange
        var walletName = "TestBurnerWallet24";

        // Act
        var result = BurnerWallet.CreateWithSeed(walletName, _mockRpcClient, _mockTokenMintResolver, use24Words: true);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.wallet.WalletType.Should().Be(WalletType.Burner);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(24);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });
    }

    [Fact]
    public async Task SolanaWalletManager_CreateWalletWithSeedAsync_Should_Return_12_Word_Seed_Phrase_By_Default()
    {
        // Arrange
        var manager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);
        var walletName = "TestManagerWallet";
        var passphrase = "testpass";

        // Act
        var result = await manager.CreateWalletWithSeedAsync(walletName, passphrase);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(12);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });

        // Verify wallet was added to manager
        var retrievedWallet = await manager.GetWalletAsync(walletName);
        retrievedWallet.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task SolanaWalletManager_CreateWalletWithSeedAsync_Should_Return_24_Word_Seed_Phrase_When_Requested()
    {
        // Arrange
        var manager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);
        var walletName = "TestManagerWallet24";
        var passphrase = "testpass";

        // Act
        var result = await manager.CreateWalletWithSeedAsync(walletName, passphrase, use24Words: true);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(24);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });

        // Verify wallet was added to manager
        var retrievedWallet = await manager.GetWalletAsync(walletName);
        retrievedWallet.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task SolanaWalletManager_CreateBurnerWalletWithSeedAsync_Should_Return_12_Word_Seed_Phrase_By_Default()
    {
        // Arrange
        var manager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);
        var walletName = "TestBurnerManagerWallet";

        // Act
        var result = await manager.CreateBurnerWalletWithSeedAsync(walletName);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.wallet.WalletType.Should().Be(WalletType.Burner);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(12);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });

        // Verify wallet was added to manager
        var retrievedWallet = await manager.GetWalletAsync(walletName);
        retrievedWallet.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task SolanaWalletManager_CreateBurnerWalletWithSeedAsync_Should_Return_24_Word_Seed_Phrase_When_Requested()
    {
        // Arrange
        var manager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);
        var walletName = "TestBurnerManagerWallet24";

        // Act
        var result = await manager.CreateBurnerWalletWithSeedAsync(walletName, use24Words: true);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().Be(walletName);
            tuple.wallet.WalletType.Should().Be(WalletType.Burner);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(24);
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
        });

        // Verify wallet was added to manager
        var retrievedWallet = await manager.GetWalletAsync(walletName);
        retrievedWallet.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task SolanaWalletManager_CreateWalletWithSeedAsync_Should_Return_None_For_Duplicate_Name()
    {
        // Arrange
        var manager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);
        var walletName = "DuplicateWallet";
        var passphrase = "testpass";

        // Act - Create first wallet
        var firstResult = await manager.CreateWalletWithSeedAsync(walletName, passphrase);
        
        // Act - Try to create second wallet with same name
        var secondResult = await manager.CreateWalletWithSeedAsync(walletName, passphrase);

        // Assert
        firstResult.IsSome.Should().BeTrue();
        secondResult.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task SolanaWalletManager_CreateBurnerWalletWithSeedAsync_Should_Generate_Unique_Name_When_None_Provided()
    {
        // Arrange
        var manager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);

        // Act
        var result = await manager.CreateBurnerWalletWithSeedAsync();

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            tuple.wallet.Should().NotBeNull();
            tuple.wallet.Name.Should().StartWith("Burner_");
            tuple.wallet.WalletType.Should().Be(WalletType.Burner);
            tuple.seedPhrase.Should().NotBeNull();
            tuple.seedPhrase.Length.Should().Be(12);
        });
    }

    [Fact]
    public void SeedPhrase_Words_Should_Be_Valid_BIP39_Words()
    {
        // Arrange
        var walletName = "BIP39TestWallet";
        var passphrase = "testpass";

        // Act
        var result = SolanaWallet.CreateWithSeed(walletName, passphrase, _mockRpcClient, _mockTokenMintResolver);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(tuple =>
        {
            // All words should be non-empty and trimmed
            tuple.seedPhrase.Should().OnlyContain(word => !string.IsNullOrWhiteSpace(word));
            tuple.seedPhrase.Should().OnlyContain(word => word.Trim() == word);
            
            // All words should be lowercase (BIP39 standard)
            tuple.seedPhrase.Should().OnlyContain(word => word == word.ToLower());
            
            // Each word should be at least 3 characters (shortest BIP39 words)
            tuple.seedPhrase.Should().OnlyContain(word => word.Length >= 3);
        });
    }

    [Theory]
    [InlineData(true, 24)]
    [InlineData(false, 12)]
    public async Task CreateWalletWithSeed_Methods_Should_Respect_Word_Count_Parameter(bool use24Words, int expectedCount)
    {
        // Arrange
        var manager = new SolanaWalletManager(_mockRpcClient, _mockTokenMintResolver, _mockTokenAssetService);
        var walletName = $"TestWallet_{expectedCount}";
        var passphrase = "testpass";

        // Act
        var standardResult = await manager.CreateWalletWithSeedAsync(walletName, passphrase, use24Words);
        var burnerResult = await manager.CreateBurnerWalletWithSeedAsync($"{walletName}_Burner", use24Words);

        // Assert
        standardResult.IsSome.Should().BeTrue();
        standardResult.IfSome(tuple => tuple.seedPhrase.Length.Should().Be(expectedCount));

        burnerResult.IsSome.Should().BeTrue();
        burnerResult.IfSome(tuple => tuple.seedPhrase.Length.Should().Be(expectedCount));
    }
}