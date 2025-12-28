using LanguageExt;
using Solnet.Wallet;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using static LanguageExt.Prelude;

namespace Electra.Crypto.Solana.Utilities;

public static class WalletHelpers
{
    private static readonly Regex SolanaAddressRegex = new(@"^[1-9A-HJ-NP-Za-km-z]{32,44}$", RegexOptions.Compiled);
    private static readonly Regex MnemonicRegex = new(@"^(\w+\s){11}\w+$|^(\w+\s){23}\w+$", RegexOptions.Compiled);

    /// <summary>
    /// Validates if a string is a valid Solana public key address
    /// </summary>
    public static Option<bool> IsValidSolanaAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return None;

        try
        {
            // Check format first
            if (!SolanaAddressRegex.IsMatch(address))
                return false;

            // Try to create PublicKey to validate
            var publicKey = new PublicKey(address);
            return publicKey.IsOnCurve();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a mnemonic phrase is valid
    /// </summary>
    public static Option<bool> IsValidMnemonic(string mnemonic)
    {
        if (string.IsNullOrEmpty(mnemonic))
            return None;

        try
        {
            var words = mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Check word count (12 or 24 words)
            if (words.Length != 12 && words.Length != 24)
                return Some(false);

            // Check if all words are in BIP39 wordlist
            var wordList = Solnet.Wallet.Bip39.WordList.English;
            var validWords = words.All(word => wordList.WordExists(word, out _));

            if (!validWords)
                return Some(false);

            // Try to create a mnemonic to validate checksum
            var mnemonicObj = new Solnet.Wallet.Bip39.Mnemonic(mnemonic, wordList);
            return Some(true);
        }
        catch
        {
            return Some(false);
        }
    }

    /// <summary>
    /// Generates a random wallet name if none provided
    /// </summary>
    public static string GenerateWalletName(WalletType walletType = WalletType.Standard)
    {
        var prefix = walletType switch
        {
            WalletType.Burner => "Burner",
            WalletType.SubWallet => "SubWallet",
            WalletType.Hardware => "Hardware",
            WalletType.ReadOnly => "ReadOnly",
            _ => "Wallet"
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var random = new Random().Next(1000, 9999);
        
        return $"{prefix}_{timestamp}_{random}";
    }

    /// <summary>
    /// Validates wallet name format and uniqueness requirements
    /// </summary>
    public static Option<bool> IsValidWalletName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Check length
        if (name.Length < 1 || name.Length > 50)
            return false;

        // Check for invalid characters
        var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        if (name.Any(c => invalidChars.Contains(c)))
            return false;

        // Check if it starts/ends with whitespace
        if (name != name.Trim())
            return false;

        return true;
    }

    /// <summary>
    /// Converts lamports to SOL with proper decimal formatting
    /// </summary>
    public static decimal LamportsToSol(ulong lamports)
    {
        return lamports / 1_000_000_000m;
    }

    /// <summary>
    /// Converts SOL to lamports
    /// </summary>
    public static ulong SolToLamports(decimal sol)
    {
        return (ulong)(sol * 1_000_000_000m);
    }

    /// <summary>
    /// Formats token amount with appropriate decimal places
    /// </summary>
    public static string FormatTokenAmount(decimal amount, int decimals, bool showSymbol = false, string symbol = "")
    {
        var formatted = Math.Round(amount, Math.Min(decimals, 8)).ToString($"F{Math.Min(decimals, 8)}");
        
        // Remove trailing zeros
        formatted = formatted.TrimEnd('0').TrimEnd('.');
        
        if (showSymbol && !string.IsNullOrEmpty(symbol))
        {
            formatted += $" {symbol}";
        }

        return formatted;
    }

    /// <summary>
    /// Formats USD value with appropriate decimal places
    /// </summary>
    public static string FormatUsdValue(decimal value)
    {
        return value switch
        {
            >= 1000000 => $"${value / 1000000:F2}M",
            >= 1000 => $"${value / 1000:F2}K",
            >= 1 => $"${value:F2}",
            >= 0.01m => $"${value:F4}",
            _ => $"${value:F8}"
        };
    }

    /// <summary>
    /// Validates transaction amount
    /// </summary>
    public static Option<bool> IsValidTransactionAmount(decimal amount, decimal balance, decimal minAmount = 0.000001m)
    {
        if (amount <= 0)
            return false;

        if (amount < minAmount)
            return false;

        if (amount > balance)
            return false;

        return true;
    }

    /// <summary>
    /// Calculates estimated transaction fee for SOL transfer
    /// </summary>
    public static decimal EstimateSolTransferFee()
    {
        // Standard SOL transfer fee is 5000 lamports
        return LamportsToSol(5000);
    }

    /// <summary>
    /// Calculates estimated transaction fee for token transfer
    /// </summary>
    public static decimal EstimateTokenTransferFee(bool needsAtaCreation = false)
    {
        // Base transfer fee + potential ATA creation fee
        var baseFee = LamportsToSol(5000);
        var ataCreationFee = needsAtaCreation ? LamportsToSol(2039280) : 0; // ~0.002 SOL for ATA creation
        
        return baseFee + ataCreationFee;
    }

    /// <summary>
    /// Truncates address for display purposes
    /// </summary>
    public static string TruncateAddress(string address, int startChars = 4, int endChars = 4)
    {
        if (string.IsNullOrEmpty(address) || address.Length <= startChars + endChars + 3)
            return address;

        return $"{address[..startChars]}...{address[^endChars..]}";
    }

    /// <summary>
    /// Generates a secure random passphrase
    /// </summary>
    public static string GenerateSecurePassphrase(int wordCount = 6)
    {
        var words = new[]
        {
            "apple", "brave", "chair", "dance", "eagle", "flame", "grape", "house",
            "ivory", "jolly", "knight", "lemon", "magic", "noble", "ocean", "peace",
            "queen", "robot", "storm", "tiger", "ultra", "voice", "water", "xenon",
            "yellow", "zebra", "action", "bright", "cloud", "dream", "energy", "frost"
        };

        var random = new Random();
        var selectedWords = new string[wordCount];
        
        for (int i = 0; i < wordCount; i++)
        {
            selectedWords[i] = words[random.Next(words.Length)];
        }

        return string.Join("-", selectedWords);
    }
}