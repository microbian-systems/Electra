using Microbians.Validators.Extensions;
using FluentValidation;
using Microbians.Common;

namespace Microbians.Validators
{
    public class AppSettingsValidator : AbstractValidator<AppSettings>
    {
        public AppSettingsValidator()
        {
            RuleFor(x => x.Secret).NotNullOrEmpty()
                .WithMessage($"jwt secret (Secret) must not be empty");
            RuleFor(x => x.KeyVaultEndPoint).NotNullOrEmpty();
            RuleFor(x => x.AzureStorage).NotNullOrEmpty();
            RuleFor(x => x.ValidIssuers.Count).GreaterThanOrEqualTo(0);
            RuleFor(x => x.AppInsightsKey).NotNullOrEmpty();
            RuleFor(x => x.UseAzureStorage).NotNullOrEmpty();
            RuleFor(x => x.EnableMiniProfiler).NotNullOrEmpty();
            RuleFor(x => x.UseLocalDB).NotNull();
            RuleFor(x => x.AzureStorage.StorageKey).NotNullOrEmpty();
            RuleFor(x => x.AzureStorage.StorageName).NotNullOrEmpty();
            RuleFor(x => x.AzureStorage.ContainerName).NotNullOrEmpty();
            RuleFor(x => x.ConnStrings.Default).NotNullOrEmpty();
        }
    }
}