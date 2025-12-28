using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;

namespace Electra.Crypto.Core;

public interface IElectraWallet
{
    Task<Option<CreateWalletResult>> CreateWallet();
    Task<Option<CreateWalletResult>> CreateWallet(string mnemonic);
    Task<Option<CreateWalletResult>> CreateWallet(byte[] privateKey);
    Task<Option<WalletInfo>> GetWallet(string publicKey);
    Task<bool> DeleteWallet(string publicKey);
    Task<Option<CreateWalletResult>> CreateSubWallet(string parentPublicKey);
    Task<Option<IEnumerable<WalletInfo>>> GetSubWallets(string parentPublicKey);
    Task<bool> DeleteSubWallet(string publicKey);

    Task<Option<TransactionResult>> SendTokenAsync(string fromPublicKey, string toPublicKey, string tokenMint, ulong amount);
    Task<Option<TokenBalanceResult>> GetTokenBalanceAsync(string publicKey, string tokenMint);
    Task<Option<CryptoTokenInfo>> GetTokenAssetInfoAsync(string tokenMint);
    Task<Option<TransactionResult>> GetTransactionAsync(string signature);
    Task<Option<CryptoAccountInfo>> GetAccountInfoAsync(string publicKey);
    Task<Option<TransactionResult>> SwapAsync(string fromPublicKey, string toPublicKey, string tokenMint, ulong amount);
}

public record CreateWalletResult(Option<string> PublicKey, Option<string> PrivateKey, Option<string[]> Mnemonic);

public record CryptoAccountInfo
{
}

public record CryptoTokenInfo
{
}

public record TokenBalanceResult
{
}
