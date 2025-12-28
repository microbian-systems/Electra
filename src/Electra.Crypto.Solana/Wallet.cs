using LanguageExt;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Solnet.Programs.Utilities;
using static LanguageExt.Prelude;

namespace Electra.Crypto.Solana;

public class SolanaWallet : ISolanaWallet
{
    private readonly IRpcClient _rpcClient;
    private readonly ITokenMintResolver _tokenMintResolver;
    private readonly Solnet.Wallet.Wallet _solnetWallet;
    private readonly string _encryptedMnemonic;
    private bool _isLocked;
    private bool _disposed;

    public string Name { get; }
    public PublicKey PublicKey => _solnetWallet?.Account?.PublicKey ?? throw new InvalidOperationException("Wallet not initialized");
    public bool IsLocked => _isLocked;
    public WalletType WalletType { get; }

    protected SolanaWallet(string name, Solnet.Wallet.Wallet solnetWallet, IRpcClient rpcClient, 
                          ITokenMintResolver tokenMintResolver, WalletType walletType = WalletType.Standard)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _solnetWallet = solnetWallet ?? throw new ArgumentNullException(nameof(solnetWallet));
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        _tokenMintResolver = tokenMintResolver ?? throw new ArgumentNullException(nameof(tokenMintResolver));
        WalletType = walletType;
        _isLocked = false;
    }

    public static Option<SolanaWallet> Create(string name, string passphrase, IRpcClient rpcClient, 
                                            ITokenMintResolver tokenMintResolver, WalletType walletType = WalletType.Standard)
    {
        try
        {
            var mnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            var wallet = new Solnet.Wallet.Wallet(mnemonic, passphrase);
            return new SolanaWallet(name, wallet, rpcClient, tokenMintResolver, walletType);
        }
        catch (Exception)
        {
            return None;
        }
    }

    public static Option<(SolanaWallet wallet, string[] seedPhrase)> CreateWithSeed(string name, string passphrase, IRpcClient rpcClient, 
                                                                                  ITokenMintResolver tokenMintResolver, bool use24Words = false, WalletType walletType = WalletType.Standard)
    {
        try
        {
            var wordCount = use24Words ? WordCount.TwentyFour : WordCount.Twelve;
            var mnemonic = new Mnemonic(WordList.English, wordCount);
            var wallet = new Solnet.Wallet.Wallet(mnemonic, passphrase);
            var solanaWallet = new SolanaWallet(name, wallet, rpcClient, tokenMintResolver, walletType);
            var seedWords = mnemonic.ToString().Split(' ');
            return (solanaWallet, seedWords);
        }
        catch (Exception)
        {
            return None;
        }
    }

    public static Option<SolanaWallet> ImportFromMnemonic(string name, string mnemonicPhrase, string passphrase, 
                                                        IRpcClient rpcClient, ITokenMintResolver tokenMintResolver)
    {
        try
        {
            var wallet = new Solnet.Wallet.Wallet(mnemonicPhrase, WordList.English, passphrase);
            return new SolanaWallet(name, wallet, rpcClient, tokenMintResolver);
        }
        catch (Exception)
        {
            return None;
        }
    }

    public Option<string> GetMnemonic()
    {
        if (_isLocked || _solnetWallet?.Mnemonic == null)
            return None;
        
        try
        {
            return _solnetWallet.Mnemonic.ToString();
        }
        catch
        {
            return None;
        }
    }

    public Option<Account> GetAccount(int index = 0)
    {
        if (_isLocked || _solnetWallet == null)
            return None;
        
        try
        {
            return index == 0 ? _solnetWallet.Account : _solnetWallet.GetAccount(index);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<decimal>> GetSolBalanceAsync()
    {
        if (_isLocked)
            return None;
        
        try
        {
            var result = await _rpcClient.GetBalanceAsync(PublicKey);
            return result.WasSuccessful ? 
                SolHelper.ConvertToSol(result.Result.Value) : 
                None;
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<IEnumerable<TokenAssetInfo>>> GetTokenAssetsAsync()
    {
        if (_isLocked)
            return None;
        
        try
        {
            var tokenWallet = await TokenWallet.LoadAsync(_rpcClient, _tokenMintResolver, PublicKey);
            var balances = tokenWallet.Balances();
            
            var assets = new List<TokenAssetInfo>();
            
            // Add SOL balance
            var solBalance = await GetSolBalanceAsync();
            if (solBalance.IsSome)
            {
                assets.Add(TokenAssetInfo.CreateSolToken(solBalance.IfNone(0)));
            }
            
            // Add token balances
            foreach (var balance in balances)
            {
                var asset = new TokenAssetInfo
                {
                    MintAddress = balance.TokenMint,
                    Symbol = balance.Symbol,
                    Name = balance.TokenName,
                    Decimals = balance.DecimalPlaces,
                    Balance = balance.QuantityDecimal,
                    BalanceRaw = balance.QuantityRaw,
                    IsVerified = !string.IsNullOrEmpty(balance.TokenDef?.TokenLogoUrl) // Consider verified if it has a logo URL
                };
                assets.Add(asset);
            }
            
            return Option<IEnumerable<TokenAssetInfo>>.Some(assets.AsEnumerable());
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<TokenAssetInfo>> GetTokenAssetInfoAsync(string mintAddress)
    {
        if (_isLocked || string.IsNullOrEmpty(mintAddress))
            return None;
        
        try
        {
            var assets = await GetTokenAssetsAsync();
            return assets.Match(
                Some: list => {
                    var found = list.FirstOrDefault(a => a.MintAddress == mintAddress);
                    return found != null ? Some(found) : None;
                },
                None: () => None
            );
        }
        catch
        {
            return None;
        }
    }

    public Task<bool> Lock()
    {
            _isLocked = true;
            return Task.FromResult(true);
    }

    public Task<bool> UnlockAsync(string passphrase)
    {

            // In a real implementation, you would verify the passphrase
            // against encrypted wallet data
            _isLocked = false;
            return Task.FromResult(_isLocked);

    }

    public async Task<Option<string>> SendSolAsync(string destinationAddress, decimal amount)
    {
        if (_isLocked || string.IsNullOrEmpty(destinationAddress) || amount <= 0)
            return None;
        
        try
        {
            var destination = new PublicKey(destinationAddress);
            var lamports = SolHelper.ConvertToLamports(amount);
            
            var blockHash = await _rpcClient.GetRecentBlockHashAsync();
            if (!blockHash.WasSuccessful)
                return None;
            
            var transactionBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(PublicKey)
                .AddInstruction(SystemProgram.Transfer(PublicKey, destination, lamports));
            
            var transaction = transactionBuilder.Build(_solnetWallet.Account);
            var result = await _rpcClient.SendTransactionAsync(transaction);
            
            return result.WasSuccessful ? result.Result : None;
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<string>> SendTokenAsync(string tokenMintAddress, string destinationAddress, decimal amount)
    {
        if (_isLocked || string.IsNullOrEmpty(tokenMintAddress) || string.IsNullOrEmpty(destinationAddress) || amount <= 0)
            return None;
        
        try
        {
            var tokenWallet = await TokenWallet.LoadAsync(_rpcClient, _tokenMintResolver, PublicKey);
            var sourceAccount = tokenWallet.TokenAccounts().WithMint(tokenMintAddress).AssociatedTokenAccount();
            
            if (sourceAccount == null)
                return None;
            
            var source = sourceAccount;
            var destination = new PublicKey(destinationAddress);
            
            var result = await tokenWallet.SendAsync(source, amount, destination, PublicKey, 
                builder => builder.Build(_solnetWallet.Account));
            
            return result.WasSuccessful ? result.Result : None;
        }
        catch
        {
            return None;
        }
    }

    public Option<byte[]> SignMessage(byte[] message)
    {
        if (_isLocked || message == null || _solnetWallet?.Account == null)
            return None;
        
        try
        {
            return _solnetWallet.Sign(message);
        }
        catch
        {
            return None;
        }
    }

    public bool VerifyMessage(byte[] message, byte[] signature)
    {
        if (message == null || signature == null || _solnetWallet?.Account == null)
            return false;
        
        return _solnetWallet.Verify(message, signature);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _isLocked = true;
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}