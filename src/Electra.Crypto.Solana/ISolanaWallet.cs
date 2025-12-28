using LanguageExt;
using Solnet.Wallet;
using Solnet.Rpc.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Electra.Crypto.Solana;

public interface ISolanaWallet : IDisposable
{
    string Name { get; }
    PublicKey PublicKey { get; }
    bool IsLocked { get; }
    WalletType WalletType { get; }
    
    Option<string> GetMnemonic();
    Option<Account> GetAccount(int index = 0);
    Task<Option<decimal>> GetSolBalanceAsync();
    Task<Option<IEnumerable<TokenAssetInfo>>> GetTokenAssetsAsync();
    Task<Option<TokenAssetInfo>> GetTokenAssetInfoAsync(string mintAddress);
    
    Task<bool> Lock();
    Task<bool> UnlockAsync(string passphrase);
    
    Task<Option<string>> SendSolAsync(string destinationAddress, decimal amount);
    Task<Option<string>> SendTokenAsync(string tokenMintAddress, string destinationAddress, decimal amount);
    
    Option<byte[]> SignMessage(byte[] message);
    bool VerifyMessage(byte[] message, byte[] signature);
}

public interface ISolanaWalletManager
{
    Task<Option<ISolanaWallet>> CreateWalletAsync(string name, string passphrase, WalletType walletType = WalletType.Standard);
    Task<Option<(ISolanaWallet wallet, string[] seedPhrase)>> CreateWalletWithSeedAsync(string name, string passphrase, bool use24Words = false, WalletType walletType = WalletType.Standard);
    Task<Option<ISolanaWallet>> CreateBurnerWalletAsync(string name = null);
    Task<Option<(ISolanaWallet wallet, string[] seedPhrase)>> CreateBurnerWalletWithSeedAsync(string name = null, bool use24Words = false);
    Task<Option<ISolanaWallet>> ImportWalletAsync(string name, string mnemonic, string passphrase);
    Task<Option<ISolanaWallet>> CreateSubWalletAsync(string parentWalletName, string subWalletName, int accountIndex);
    
    Task<bool> DeleteWalletAsync(string name);
    Task<Option<IEnumerable<ISolanaWallet>>> GetAllWalletsAsync();
    Task<Option<ISolanaWallet>> GetWalletAsync(string name);
    
    Task<decimal> GetTotalPortfolioValueAsync();
    Task<Option<IEnumerable<TokenAssetInfo>>> GetAllTokenAssetsAsync();
}

public interface IBurnerWallet : ISolanaWallet
{
    DateTime CreatedAt { get; }
    TimeSpan TimeToLive { get; }
    bool IsExpired { get; }
    Task<bool> ExtendLifetimeAsync(TimeSpan additionalTime);
    Task<bool> BurnAsync();
}

public interface ISubWallet : ISolanaWallet
{
    string ParentWalletName { get; }
    int AccountIndex { get; }
    Option<ISolanaWallet> GetParentWallet();
}

public interface ITokenAssetService
{
    Task<Option<TokenAssetInfo>> GetTokenInfoAsync(string mintAddress);
    Task<Option<decimal>> GetTokenPriceAsync(string mintAddress);
    Task<Option<IEnumerable<TokenAssetInfo>>> GetPopularTokensAsync();
    Task<Option<IEnumerable<TokenAssetInfo>>> SearchTokensAsync(string query);
}


public enum WalletType
{
    Standard,
    Burner,
    SubWallet,
    Hardware,
    ReadOnly
}