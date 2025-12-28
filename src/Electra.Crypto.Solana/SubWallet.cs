using LanguageExt;
using Solnet.Extensions;
using Solnet.Rpc;
using Solnet.Wallet;
using System;

namespace Electra.Crypto.Solana;

public class SubWallet : SolanaWallet, ISubWallet
{
    public string ParentWalletName { get; }
    public int AccountIndex { get; }

    private readonly Func<Option<ISolanaWallet>> _getParentWallet;

    private SubWallet(string name, string parentWalletName, int accountIndex, 
        Solnet.Wallet.Wallet solnetWallet, IRpcClient rpcClient, 
        ITokenMintResolver tokenMintResolver, Func<Option<ISolanaWallet>> getParentWallet)
        : base(name, solnetWallet, rpcClient, tokenMintResolver, WalletType.SubWallet)
    {
        ParentWalletName = parentWalletName ?? throw new ArgumentNullException(nameof(parentWalletName));
        AccountIndex = accountIndex;
        _getParentWallet = getParentWallet ?? throw new ArgumentNullException(nameof(getParentWallet));
    }

    public static Option<SubWallet> Create(string name, string parentWalletName, int accountIndex,
        Solnet.Wallet.Wallet parentSolnetWallet, IRpcClient rpcClient,
        ITokenMintResolver tokenMintResolver, Func<Option<ISolanaWallet>> getParentWallet)
    {
        try
        {
            // For now, create a simplified sub-wallet using the parent's mnemonic
            // In a full implementation, this would derive a new account from the parent
            var subWallet = new Solnet.Wallet.Wallet(parentSolnetWallet.Mnemonic, "");
            
            return new SubWallet(name, parentWalletName, accountIndex, subWallet, 
                rpcClient, tokenMintResolver, getParentWallet);
        }
        catch
        {
            return Prelude.None;
        }
    }

    public Option<ISolanaWallet> GetParentWallet()
    {
        return _getParentWallet();
    }
}