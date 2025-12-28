using LanguageExt;

namespace Electra.Crypto.Core;

public abstract record  TransactionResult(
    Option<string> TransactionId,
    Option<string> TransactionSignature,
    Option<string> TransactionError,
    Option<string> TransactionType
);


public abstract record WalletTransactionResult(
    Option<string> TransactionId,
    Option<string> TransactionSignature,
    Option<string> TransactionError,
    Option<string> TransactionType,
    Option<string> WalletAddress
);