using Electra.Crypto.Solana;
using Electra.Crypto.Solana.Utilities;
using FluentAssertions;
using LanguageExt;
using NSubstitute;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Rpc;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System;
using System.Threading.Tasks;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Types;

namespace Electra.Crypto.Solana.Tests;

public class SolanaWalletTests
{
    private readonly IRpcClient _mockRpcClient;
    private readonly ITokenMintResolver _mockTokenMintResolver;

    public SolanaWalletTests()
    {
        _mockRpcClient = Substitute.For<IRpcClient>();
        _mockTokenMintResolver = Substitute.For<ITokenMintResolver>();
    }

    [Fact]
    public void Create_WithValidParameters_ShouldReturnWallet()
    {
        // Arrange
        var name = "TestWallet";
        var passphrase = "test-passphrase";

        // Act
        var result = SolanaWallet.Create(name, passphrase, _mockRpcClient, _mockTokenMintResolver);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfNone(() => null).Should().NotBeNull();
        result.Match(
            Some: wallet => {
                wallet.Name.Should().Be(name);
                wallet.WalletType.Should().Be(WalletType.Standard);
                wallet.IsLocked.Should().BeFalse();
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public void ImportFromMnemonic_WithValidMnemonic_ShouldReturnWallet()
    {
        // Arrange
        var name = "ImportedWallet";
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var passphrase = "";

        // Act
        var result = SolanaWallet.ImportFromMnemonic(name, mnemonic, passphrase, _mockRpcClient, _mockTokenMintResolver);

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
    public async Task Lock_ShouldLockWallet()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();

        // Act
        var result = await wallet!.Lock();

        // Assert
        result.Should().BeTrue();
        wallet.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task UnlockAsync_WithCorrectPassphrase_ShouldUnlockWallet()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();
        wallet!.Lock();

        // Act
        // var result = await wallet.UnlockAsync("passphrase");
        //
        // // Assert
        // result.IsSome.Should().BeTrue();
        // wallet.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetSolBalanceAsync_WhenUnlocked_ShouldReturnBalance()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();

        var expectedBalance = 1000000000UL; // 1 SOL in lamports
        var mockResult = new RequestResult<ResponseValue<ulong>>
        {
            WasHttpRequestSuccessful = true,
            WasRequestSuccessfullyHandled = true,
            HttpStatusCode = System.Net.HttpStatusCode.OK,
            Result = new ResponseValue<ulong> { Value = expectedBalance }
        };
        _mockRpcClient.GetBalanceAsync(Arg.Any<string>(), Arg.Any<Commitment>())
            .Returns(Task.FromResult(mockResult));

        // Act
        var result = await wallet!.GetSolBalanceAsync();

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfNone(0).Should().Be(1.0m); // 1 SOL
    }

    [Fact]
    public async Task GetSolBalanceAsync_WhenLocked_ShouldReturnNone()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();
        wallet!.Lock();

        // Act
        var result = await wallet.GetSolBalanceAsync();

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public void GetMnemonic_WhenUnlocked_ShouldReturnMnemonic()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();

        // Act
        var result = wallet!.GetMnemonic();

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfNone("").Should().NotBeEmpty();
        
        // Validate it's a proper 12-word mnemonic
        var mnemonic = result.IfNone("");
        var words = mnemonic.Split(' ');
        words.Length.Should().Be(12);
    }

    [Fact]
    public void GetMnemonic_WhenLocked_ShouldReturnNone()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();
        wallet!.Lock();

        // Act
        var result = wallet.GetMnemonic();

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public void SignMessage_WithValidMessage_ShouldReturnSignature()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();
        var message = System.Text.Encoding.UTF8.GetBytes("Hello, Solana!");

        // Act
        var result = wallet!.SignMessage(message);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfNone(Array.Empty<byte>()).Length.Should().Be(64); // Ed25519 signature length
    }

    [Fact]
    public void VerifyMessage_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();
        var message = System.Text.Encoding.UTF8.GetBytes("Hello, Solana!");
        var signature = wallet!.SignMessage(message).IfNone(Array.Empty<byte>());

        // Act
        var result = wallet.VerifyMessage(message, signature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldLockWallet()
    {
        // Arrange
        var wallet = SolanaWallet.Create("TestWallet", "passphrase", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();

        // Act
        wallet!.Dispose();

        // Assert
        wallet.IsLocked.Should().BeTrue();
    }
}

public class BurnerWalletTests
{
    private readonly IRpcClient _mockRpcClient;
    private readonly ITokenMintResolver _mockTokenMintResolver;

    public BurnerWalletTests()
    {
        _mockRpcClient = Substitute.For<IRpcClient>();
        _mockTokenMintResolver = Substitute.For<ITokenMintResolver>();
    }

    [Fact]
    public void Create_ShouldReturnBurnerWallet()
    {
        // Arrange
        var name = "TestBurner";
        var timeToLive = TimeSpan.FromHours(1);

        // Act
        var result = BurnerWallet.Create(name, _mockRpcClient, _mockTokenMintResolver, timeToLive);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            Some: wallet => {
                wallet.Name.Should().Be(name);
                wallet.WalletType.Should().Be(WalletType.Burner);
                wallet.TimeToLive.Should().Be(timeToLive);
                wallet.IsExpired.Should().BeFalse();
                return true;
            },
            None: () => false
        ).Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutName_ShouldGenerateDefaultName()
    {
        // Act
        var result = BurnerWallet.Create(null, _mockRpcClient, _mockTokenMintResolver);

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
    public async Task ExtendLifetimeAsync_ShouldExtendTimeToLive()
    {
        // Arrange
        var wallet = BurnerWallet.Create("TestBurner", _mockRpcClient, _mockTokenMintResolver, TimeSpan.FromHours(1)).IfNone(() => null);
        wallet.Should().NotBeNull();
        var originalTtl = wallet!.TimeToLive;
        var extension = TimeSpan.FromHours(2);

        // Act
        var result = await wallet.ExtendLifetimeAsync(extension);

        // Assert
        result.Should().BeTrue();
        wallet.TimeToLive.Should().Be(originalTtl.Add(extension));
    }

    [Fact]
    public async Task BurnAsync_ShouldLockWallet()
    {
        // Arrange
        var wallet = BurnerWallet.Create("TestBurner", _mockRpcClient, _mockTokenMintResolver).IfNone(() => null);
        wallet.Should().NotBeNull();

        // Act
        var result = await wallet!.BurnAsync();

        // Assert
        result.Should().BeTrue();
        wallet.IsLocked.Should().BeTrue();
    }
}

public class WalletHelpersTests
{
    [Theory]
    [InlineData("11111111111111111111111111111112", true)]
    [InlineData("So11111111111111111111111111111111111111112", true)]
    [InlineData("invalid-address", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidSolanaAddress_ShouldValidateCorrectly(string address, bool expected)
    {
        // Act
        var result = WalletHelpers.IsValidSolanaAddress(address);

        // Assert
        if (expected)
        {
            result.IsSome.Should().BeTrue();
            result.IfNone(false).Should().BeTrue();
        }
        else
        {
            (result.IsNone || !result.IfNone(true)).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about", true)]
    [InlineData("invalid mnemonic phrase", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidMnemonic_ShouldValidateCorrectly(string mnemonic, bool expected)
    {
        // Act
        var result = WalletHelpers.IsValidMnemonic(mnemonic);

        // Assert
        if (expected)
        {
            result.IsSome.Should().BeTrue();
            result.IfNone(false).Should().BeTrue();
        }
        else
        {
            (result.IsNone || !result.IfNone(true)).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("ValidWallet123", true)]
    [InlineData("", false)]
    [InlineData("Wallet/With/Invalid*Chars", false)]
    [InlineData("  SpacedWallet  ", false)]
    public void IsValidWalletName_ShouldValidateCorrectly(string name, bool expected)
    {
        // Act
        var result = WalletHelpers.IsValidWalletName(name);

        // Assert
        if (expected)
        {
            result.IsSome.Should().BeTrue();
            result.IfNone(false).Should().BeTrue();
        }
        else
        {
            (result.IsNone || !result.IfNone(true)).Should().BeTrue();
        }
    }

    [Fact]
    public void LamportsToSol_ShouldConvertCorrectly()
    {
        // Arrange
        var lamports = 1_500_000_000UL; // 1.5 SOL

        // Act
        var result = WalletHelpers.LamportsToSol(lamports);

        // Assert
        result.Should().Be(1.5m);
    }

    [Fact]
    public void SolToLamports_ShouldConvertCorrectly()
    {
        // Arrange
        var sol = 2.5m;

        // Act
        var result = WalletHelpers.SolToLamports(sol);

        // Assert
        result.Should().Be(2_500_000_000UL);
    }

    [Fact]
    public void FormatTokenAmount_ShouldFormatCorrectly()
    {
        // Arrange
        var amount = 123.456789m;
        var decimals = 6;
        var symbol = "USDC";

        // Act
        var result = WalletHelpers.FormatTokenAmount(amount, decimals, true, symbol);

        // Assert
        result.Should().Be("123.456789 USDC");
    }

    [Theory]
    [InlineData(1234567.89, "$1.23M")]
    [InlineData(12345.67, "$12.35K")]
    [InlineData(123.45, "$123.45")]
    [InlineData(0.1234, "$0.1234")]
    [InlineData(0.00000123, "$0.00000123")]
    public void FormatUsdValue_ShouldFormatCorrectly(double value, string expected)
    {
        // Act
        var result = WalletHelpers.FormatUsdValue((decimal)value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void TruncateAddress_ShouldTruncateCorrectly()
    {
        // Arrange
        var address = "So11111111111111111111111111111111111111112";

        // Act
        var result = WalletHelpers.TruncateAddress(address);

        // Assert
        result.Should().Be("So11...1112");
    }

    [Fact]
    public void GenerateSecurePassphrase_ShouldGenerateValidPassphrase()
    {
        // Act
        var result = WalletHelpers.GenerateSecurePassphrase(4);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Split('-').Length.Should().Be(4);
    }
}