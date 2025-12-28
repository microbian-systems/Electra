using LanguageExt;
using Solnet.Extensions;
using Solnet.Rpc;
using Solnet.Wallet.Bip39;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Electra.Crypto.Solana;

public class BurnerWallet : SolanaWallet, IBurnerWallet
{
    private readonly Timer _expirationTimer;

    public DateTime CreatedAt { get; }
    public TimeSpan TimeToLive { get; private set; }
    public bool IsExpired => DateTime.UtcNow > CreatedAt.Add(TimeToLive);

    private BurnerWallet(string name, Solnet.Wallet.Wallet solnetWallet, IRpcClient rpcClient, 
        ITokenMintResolver tokenMintResolver, TimeSpan timeToLive)
        : base(name, solnetWallet, rpcClient, tokenMintResolver, WalletType.Burner)
    {
        CreatedAt = DateTime.UtcNow;
        TimeToLive = timeToLive;
        
        _expirationTimer = new Timer(OnExpired, null, timeToLive, Timeout.InfiniteTimeSpan);
    }

    public static Option<BurnerWallet> Create(string name, IRpcClient rpcClient, 
        ITokenMintResolver tokenMintResolver, TimeSpan? timeToLive = null)
    {
        try
        {
            var ttl = timeToLive ?? TimeSpan.FromHours(24); // Default 24 hours
            var mnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            var wallet = new Solnet.Wallet.Wallet(mnemonic, "");
            var finalName = name ?? $"Burner_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            
            return new BurnerWallet(finalName, wallet, rpcClient, tokenMintResolver, ttl);
        }
        catch
        {
            return Prelude.None;
        }
    }

    public static Option<(BurnerWallet wallet, string[] seedPhrase)> CreateWithSeed(string name, IRpcClient rpcClient, 
        ITokenMintResolver tokenMintResolver, bool use24Words = false, TimeSpan? timeToLive = null)
    {
        try
        {
            var ttl = timeToLive ?? TimeSpan.FromHours(24); // Default 24 hours
            var wordCount = use24Words ? WordCount.TwentyFour : WordCount.Twelve;
            var mnemonic = new Mnemonic(WordList.English, wordCount);
            var wallet = new Solnet.Wallet.Wallet(mnemonic, "");
            var finalName = name ?? $"Burner_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            
            var burnerWallet = new BurnerWallet(finalName, wallet, rpcClient, tokenMintResolver, ttl);
            var seedWords = mnemonic.ToString().Split(' ');
            return (burnerWallet, seedWords);
        }
        catch
        {
            return Prelude.None;
        }
    }

    public Task<bool> ExtendLifetimeAsync(TimeSpan additionalTime)
    {
        try
        {
            if (IsExpired)
                return Task.FromResult(false);
            
            TimeToLive = TimeToLive.Add(additionalTime);
            
            // Reset timer with new duration
            var newDuration = CreatedAt.Add(TimeToLive) - DateTime.UtcNow;
            if (newDuration > TimeSpan.Zero)
            {
                _expirationTimer?.Change(newDuration, Timeout.InfiniteTimeSpan);
            }
            
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<bool> BurnAsync()
    {
        try
        {
            // Send all remaining SOL to a burn address or back to main wallet
            var balance = await GetSolBalanceAsync();
            if (balance.IsSome && balance.IfNone(0) > 0.001m) // Keep some for transaction fees
            {
                // In a real implementation, you might want to send to a specific address
                // For now, we'll just mark as burned
            }
            
            var res = await Lock();
            _expirationTimer?.Dispose();
            return res;
        }
        catch
        {
            // todo - add logging
            return false;
        }
    }

    private void OnExpired(object state)
    {
        _ = Task.Run(async () => await BurnAsync());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _expirationTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}