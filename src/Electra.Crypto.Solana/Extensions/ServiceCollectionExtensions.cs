using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Pyth;
using Solnet.Rpc;
using System;
using Microsoft.Extensions.Logging;

namespace Electra.Crypto.Solana.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSolanaWallet(this IServiceCollection services, 
        Action<SolanaWalletOptions>? configureOptions = null)
    {
        var options = new SolanaWalletOptions();
        configureOptions?.Invoke(options);

        // Register core Solnet services
        services.TryAddSingleton<IRpcClient>(provider => 
            Solnet.Rpc.ClientFactory.GetClient(options.RpcUrl));

        services.TryAddSingleton<ITokenMintResolver>(provider =>
            new TokenMintResolver());

        // Register Pyth client if enabled
        if (options.EnablePythPricing)
        {
            services.TryAddSingleton<IPythClient>(provider =>
            {
                var logger = provider.GetService<ILogger<PythClient>>();
                var client = Solnet.Pyth.ClientFactory.GetClient(logger: logger);
                return client;
            });
            return services;
        }

        // Register our wallet services
        services.TryAddSingleton<ITokenAssetService, TokenAssetService>();
        services.TryAddSingleton<ISolanaWalletManager, SolanaWalletManager>();
        services.TryAddSingleton<ISwapService, SwapService>();

        return services;
    }

    public static IServiceCollection AddSolanaWalletForMainnet(this IServiceCollection services)
    {
        return services.AddSolanaWallet(options =>
        {
            options.RpcUrl = Cluster.MainNet;
            options.WebSocketUrl = "wss://api.mainnet-beta.solana.com/";
            options.EnablePythPricing = true;
        });
    }

    public static IServiceCollection AddSolanaWalletForDevnet(this IServiceCollection services)
    {
        return services.AddSolanaWallet(options =>
        {
            options.RpcUrl = Cluster.DevNet;
            options.WebSocketUrl = "wss://api.devnet.solana.com/";
            options.EnablePythPricing = false;
        });
    }

    public static IServiceCollection AddSolanaWalletForTestnet(this IServiceCollection services)
    {
        return services.AddSolanaWallet(options =>
        {
            options.RpcUrl = Cluster.TestNet;
            options.WebSocketUrl = "wss://api.testnet.solana.com/";
            options.EnablePythPricing = false;
        });
    }
}

public class SolanaWalletOptions
{
    public Cluster RpcUrl { get; set; } = Cluster.MainNet;
    public string WebSocketUrl { get; set; } = "wss://api.mainnet-beta.solana.com/";
    public bool EnablePythPricing { get; set; } = true;
    public TimeSpan DefaultBurnerWalletTtl { get; set; } = TimeSpan.FromHours(24);
    public int MaxSubWalletsPerParent { get; set; } = 10;
    public bool EnableWalletPersistence { get; set; } = true;
    public string? WalletStoragePath { get; set; }
}