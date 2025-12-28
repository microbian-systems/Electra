using LanguageExt;

namespace Electra.Crypto.Core;

public record TokenAssetInfo(
    Option<string> Name, 
    Option<string> Symbol, 
    Option<string> LogoUri, 
    decimal Price, 
    Option<string> PublicKey);


public record WalletInfo(
    Option<string> Name, 
    decimal Balance,
    Option<string[]> Assets,
    Option<string> PublicKey);

