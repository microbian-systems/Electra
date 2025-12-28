using LanguageExt;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Rpc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Electra.Crypto.Solana;

public class SolanaWalletManager : ISolanaWalletManager, IDisposable
{
    private readonly IRpcClient _rpcClient;
    private readonly ITokenMintResolver _tokenMintResolver;
    private readonly ITokenAssetService _tokenAssetService;
    private readonly ConcurrentDictionary<string, ISolanaWallet> _wallets;
    private readonly ConcurrentDictionary<string, List<string>> _subWalletIndex; // parentName -> List of subWallet names
    private bool _disposed;

    public SolanaWalletManager(IRpcClient rpcClient, ITokenMintResolver tokenMintResolver, ITokenAssetService tokenAssetService)
    {
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        _tokenMintResolver = tokenMintResolver ?? throw new ArgumentNullException(nameof(tokenMintResolver));
        _tokenAssetService = tokenAssetService ?? throw new ArgumentNullException(nameof(tokenAssetService));
        _wallets = new ConcurrentDictionary<string, ISolanaWallet>();
        _subWalletIndex = new ConcurrentDictionary<string, List<string>>();
    }

    public async Task<Option<ISolanaWallet>> CreateWalletAsync(string name, string passphrase, WalletType walletType = WalletType.Standard)
    {
        if (string.IsNullOrEmpty(name) || _wallets.ContainsKey(name))
            return None;

        try
        {
            var wallet = walletType switch
            {
                WalletType.Burner => await CreateBurnerWalletAsync(name),
                WalletType.Standard => SolanaWallet.Create(name, passphrase, _rpcClient, _tokenMintResolver).ToOption<ISolanaWallet>(),
                _ => None
            };

            return wallet.Match(
                Some: w => {
                    _wallets.TryAdd(name, w);
                    return Some(w);
                },
                None: () => None
            );
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<(ISolanaWallet wallet, string[] seedPhrase)>> CreateWalletWithSeedAsync(string name, string passphrase, bool use24Words = false, WalletType walletType = WalletType.Standard)
    {
        if (string.IsNullOrEmpty(name) || _wallets.ContainsKey(name))
            return None;

        try
        {
            Option<(ISolanaWallet wallet, string[] seedPhrase)> walletResult;
            
            if (walletType == WalletType.Burner)
            {
                walletResult = await CreateBurnerWalletWithSeedAsync(name, use24Words);
            }
            else
            {
                var standardResult = SolanaWallet.CreateWithSeed(name, passphrase, _rpcClient, _tokenMintResolver, use24Words);
                walletResult = standardResult.Map(result => (wallet: (ISolanaWallet)result.wallet, result.seedPhrase));
            }

            return walletResult.Match(
                Some: result => {
                    _wallets.TryAdd(name, result.wallet);
                    return Some(result);
                },
                None: () => None
            );
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<ISolanaWallet>> CreateBurnerWalletAsync(string name = null)
    {
        try
        {
            var finalName = name ?? $"Burner_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            
            if (_wallets.ContainsKey(finalName))
                return None;

            var burnerWallet = BurnerWallet.Create(finalName, _rpcClient, _tokenMintResolver);
            
            return burnerWallet.Match(
                Some: w => {
                    _wallets.TryAdd(finalName, w);
                    return Some<ISolanaWallet>(w);
                },
                None: () => None
            );
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<(ISolanaWallet wallet, string[] seedPhrase)>> CreateBurnerWalletWithSeedAsync(string name = null, bool use24Words = false)
    {
        try
        {
            var finalName = name ?? $"Burner_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            
            if (_wallets.ContainsKey(finalName))
                return None;

            var burnerWalletResult = BurnerWallet.CreateWithSeed(finalName, _rpcClient, _tokenMintResolver, use24Words);
            
            return burnerWalletResult.Match(
                Some: result => {
                    _wallets.TryAdd(finalName, result.wallet);
                    return Some((wallet: (ISolanaWallet)result.wallet, result.seedPhrase));
                },
                None: () => None
            );
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<ISolanaWallet>> ImportWalletAsync(string name, string mnemonic, string passphrase)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(mnemonic) || _wallets.ContainsKey(name))
            return None;

        try
        {
            var wallet = SolanaWallet.ImportFromMnemonic(name, mnemonic, passphrase, _rpcClient, _tokenMintResolver);
            
            return wallet.Match(
                Some: w => {
                    _wallets.TryAdd(name, w);
                    return Some<ISolanaWallet>(w);
                },
                None: () => None
            );
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<ISolanaWallet>> CreateSubWalletAsync(string parentWalletName, string subWalletName, int accountIndex)
    {
        if (string.IsNullOrEmpty(parentWalletName) || string.IsNullOrEmpty(subWalletName) || 
            _wallets.ContainsKey(subWalletName) || !_wallets.ContainsKey(parentWalletName))
            return None;

        try
        {
            var parentWallet = _wallets[parentWalletName];
            if (parentWallet is not SolanaWallet solanaParent)
                return None;

            // Get the underlying Solnet wallet (this would need to be exposed in a real implementation)
            // For now, we'll create a new sub-wallet with the same approach
            var subWallet = SubWallet.Create(
                subWalletName, 
                parentWalletName, 
                accountIndex,
                null, // This would need the parent's Solnet.Wallet instance
                _rpcClient,
                _tokenMintResolver,
                () => GetWalletAsync(parentWalletName).Result
            );

            return subWallet.Match(
                Some: w => {
                    _wallets.TryAdd(subWalletName, w);
                    
                    // Add to sub-wallet index
                    _subWalletIndex.AddOrUpdate(
                        parentWalletName,
                        new List<string> { subWalletName },
                        (key, existing) => { existing.Add(subWalletName); return existing; }
                    );
                    
                    return Some<ISolanaWallet>(w);
                },
                None: () => None
            );
        }
        catch
        {
            return None;
        }
    }

    public async Task<bool> DeleteWalletAsync(string name)
    {
        if (string.IsNullOrEmpty(name) || !_wallets.ContainsKey(name))
            return false;

        try
        {
            var wallet = _wallets[name];
            
            // If this is a parent wallet, delete all sub-wallets first
            if (_subWalletIndex.ContainsKey(name))
            {
                var subWallets = _subWalletIndex[name].ToList();
                foreach (var subWalletName in subWallets)
                {
                    await DeleteWalletAsync(subWalletName);
                }
                _subWalletIndex.TryRemove(name, out _);
            }
            
            // If this is a sub-wallet, remove from parent's index
            if (wallet is ISubWallet subWallet)
            {
                if (_subWalletIndex.ContainsKey(subWallet.ParentWalletName))
                {
                    _subWalletIndex[subWallet.ParentWalletName].Remove(name);
                }
            }

            // Dispose and remove the wallet
            wallet.Dispose();
            _wallets.TryRemove(name, out _);
            return true;
        }
        catch
        {
            // todo - log error
            return false;
        }
    }

    public Task<Option<IEnumerable<ISolanaWallet>>> GetAllWalletsAsync()
    {
        try
        {
            return Task.FromResult(Some<IEnumerable<ISolanaWallet>>(_wallets.Values.AsEnumerable()));
        }
        catch
        {
            return Task.FromResult(Option<IEnumerable<ISolanaWallet>>.None);
        }
    }

    public Task<Option<ISolanaWallet>> GetWalletAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Task.FromResult(Option<ISolanaWallet>.None);

        return Task.FromResult(_wallets.TryGetValue(name, out var wallet) ? Some(wallet) : Option<ISolanaWallet>.None);
    }

    public async Task<decimal> GetTotalPortfolioValueAsync()
    {
        try
        {
            decimal totalValue = 0;

            foreach (var wallet in _wallets.Values)
            {
                var solBalance = await wallet.GetSolBalanceAsync();
                if (solBalance.IsSome)
                {
                    // Get SOL price and add to total
                    var solPrice = await _tokenAssetService.GetTokenPriceAsync("So11111111111111111111111111111111111111112");
                    if (solPrice.IsSome)
                    {
                        totalValue += solBalance.IfNone(0) * solPrice.IfNone(0);
                    }
                }

                var tokens = await wallet.GetTokenAssetsAsync();
                if (tokens.IsSome)
                {
                    foreach (var token in tokens.IfNone(Enumerable.Empty<TokenAssetInfo>()))
                    {
                        if (token.Value.HasValue)
                        {
                            totalValue += token.Value.Value;
                        }
                    }
                }
            }

            return totalValue;
        }
        catch
        {
            // todo - log error 
            return 0L;
        }
    }

    public async Task<Option<IEnumerable<TokenAssetInfo>>> GetAllTokenAssetsAsync()
    {
        try
        {
            var allAssets = new List<TokenAssetInfo>();
            var assetsByMint = new Dictionary<string, TokenAssetInfo>();

            foreach (var wallet in _wallets.Values)
            {
                var assets = await wallet.GetTokenAssetsAsync();
                if (assets.IsSome)
                {
                    foreach (var asset in assets.IfNone(Enumerable.Empty<TokenAssetInfo>()))
                    {
                        if (assetsByMint.ContainsKey(asset.MintAddress))
                        {
                            // Aggregate balances for same token across wallets
                            var existing = assetsByMint[asset.MintAddress];
                            assetsByMint[asset.MintAddress] = existing.WithBalance(
                                existing.Balance + asset.Balance,
                                existing.BalanceRaw + asset.BalanceRaw
                            );
                        }
                        else
                        {
                            assetsByMint[asset.MintAddress] = asset;
                        }
                    }
                }
            }

            return Option<IEnumerable<TokenAssetInfo>>.Some(assetsByMint.Values.AsEnumerable());
        }
        catch
        {
            return None;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            foreach (var wallet in _wallets.Values)
            {
                wallet?.Dispose();
            }
            _wallets.Clear();
            _subWalletIndex.Clear();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}