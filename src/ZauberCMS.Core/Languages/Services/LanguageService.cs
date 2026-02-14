
using Electra.Models.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Audit.Interfaces;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Languages.Interfaces;
using ZauberCMS.Core.Languages.Models;
using ZauberCMS.Core.Languages.Parameters;
using ZauberCMS.Core.Languages.Mapping;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Plugins;
using ZauberCMS.Core.Shared.Models;
using ZauberCMS.Core.Shared.Services;

namespace ZauberCMS.Core.Languages.Services;

public class LanguageService(
    IServiceScopeFactory serviceScopeFactory,
    ICacheService cacheService,
    AuthenticationStateProvider authenticationStateProvider,
    ExtensionManager extensionManager)
    : ILanguageService
{
    /// <summary>
    /// Retrieves a language by id or ISO code.
    /// </summary>
    /// <param name="parameters">Id or ISO code, and tracking flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Language or null.</returns>
    public async Task<Language?> GetLanguageAsync(GetLanguageParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var query = dbContext.Query<Language>();

        if (parameters.LanguageIsoCode != null)
        {
            return await query.FirstOrDefaultAsync(x => x.LanguageIsoCode == parameters.LanguageIsoCode, cancellationToken);
        }

        if (parameters.Id != null)
        {
            return await query.FirstOrDefaultAsync(x => x.Id == parameters.Id, cancellationToken);
        }

        // Should never get here
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Creates or updates a language from a CultureInfo. Prevents duplicates and logs audit.
    /// </summary>
    /// <param name="parameters">CultureInfo and optional id to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result including success and messages.</returns>
    public async Task<HandlerResult<Language>> SaveLanguageAsync(SaveLanguageParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        var user = await userManager.GetUserAsync(authState.User);
        var handlerResult = new HandlerResult<Language>();

        if (parameters.CultureInfo != null)
        {
            var isUpdate = false;

            var language = new Language();
            if (parameters.Id != null)
            {
                var lang = dbContext.Query<Language>().FirstOrDefault(x => x.Id == parameters.Id);
                if (lang != null)
                {
                    if (parameters.CultureInfo.Name == lang.LanguageIsoCode)
                    {
                        // Just return if they are trying to save the same culture
                        handlerResult.Success = true;
                        return handlerResult;
                    }

                    isUpdate = true;
                    language = lang;
                }
            }

            // Does this already exist
            var existing = dbContext.Query<Language>().FirstOrDefault(x => x.LanguageIsoCode == parameters.CultureInfo.Name);
            if (existing != null)
            {
                handlerResult.AddMessage("Language already exists", ResultMessageType.Error);
                return handlerResult;
            }

            language.LanguageCultureName = parameters.CultureInfo.EnglishName;
            language.LanguageIsoCode = parameters.CultureInfo.Name;

            if (!isUpdate)
            {
                await dbContext.StoreAsync(language, cancellationToken);
            }
            else
            {
                language.DateUpdated = DateTime.UtcNow;
            }

            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            await user.AddAudit(language, $"Language ({language.LanguageCultureName})",
                isUpdate ? AuditExtensions.AuditAction.Update : AuditExtensions.AuditAction.Create, auditService,
                cancellationToken);
            return await dbContext.SaveChangesAndLog(language, handlerResult, cacheService, extensionManager, cancellationToken);
        }

        handlerResult.AddMessage("CultureInfo is null", ResultMessageType.Error);
        return handlerResult;
    }

    /// <summary>
    /// Synchronous call to get query language 
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public PaginatedList<Language> QueryLanguage(QueryLanguageParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var query = dbContext.Query<Language>().AsQueryable();

        if (parameters.Query != null)
        {
            query = parameters.Query.Invoke();
        }
        else
        {
            var idCount = parameters.Ids.Count;
            if (parameters.Ids.Count != 0)
            {
                query = query.Where(x => parameters.Ids.Contains(x.Id));
                parameters.AmountPerPage = idCount;
            }

            if (parameters.LanguageIsoCodes.Count != 0)
            {
                query = query.Where(x =>
                    x.LanguageIsoCode != null && parameters.LanguageIsoCodes.Contains(x.LanguageIsoCode));
            }
        }

        if (parameters.WhereClause != null)
        {
            query = query.Where(parameters.WhereClause);
        }

        query = parameters.OrderBy switch
        {
            GetLanguageOrderBy.DateCreated => query.OrderBy(p => p.DateCreated),
            GetLanguageOrderBy.DateCreatedDescending => query.OrderByDescending(p => p.DateCreated),
            GetLanguageOrderBy.LanguageIsoCode => query.OrderBy(p => p.LanguageIsoCode),
            GetLanguageOrderBy.LanguageCultureName => query.OrderBy(p => p.LanguageCultureName),
            _ => query.OrderByDescending(p => p.DateCreated)
        };

        return query.ToPaginatedList(parameters.PageIndex, parameters.AmountPerPage);
    }
    
    /// <summary>
    /// Queries languages with filtering, ordering and paging.
    /// </summary>
    /// <param name="parameters">Query options including ids and ISO codes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of languages.</returns>
    public Task<PaginatedList<Language>> QueryLanguageAsync(QueryLanguageParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var query = dbContext.Query<Language>().AsQueryable();

        if (parameters.Query != null)
        {
            query = parameters.Query.Invoke();
        }
        else
        {
            var idCount = parameters.Ids.Count;
            if (parameters.Ids.Count != 0)
            {
                query = query.Where(x => parameters.Ids.Contains(x.Id));
                parameters.AmountPerPage = idCount;
            }

            if (parameters.LanguageIsoCodes.Count != 0)
            {
                query = query.Where(x =>
                    x.LanguageIsoCode != null && parameters.LanguageIsoCodes.Contains(x.LanguageIsoCode));
            }
        }

        if (parameters.WhereClause != null)
        {
            query = query.Where(parameters.WhereClause);
        }

        query = parameters.OrderBy switch
        {
            GetLanguageOrderBy.DateCreated => query.OrderBy(p => p.DateCreated),
            GetLanguageOrderBy.DateCreatedDescending => query.OrderByDescending(p => p.DateCreated),
            GetLanguageOrderBy.LanguageIsoCode => query.OrderBy(p => p.LanguageIsoCode),
            GetLanguageOrderBy.LanguageCultureName => query.OrderBy(p => p.LanguageCultureName),
            _ => query.OrderByDescending(p => p.DateCreated)
        };

        return Task.FromResult(query.ToPaginatedList(parameters.PageIndex, parameters.AmountPerPage));
    }

    /// <summary>
    /// Deletes a language by id or ISO code. Logs audit.
    /// </summary>
    /// <param name="parameters">Id or ISO code to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result including success and messages.</returns>
    public async Task<HandlerResult<Language?>> DeleteLanguageAsync(DeleteLanguageParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        var user = await userManager.GetUserAsync(authState.User);
        var handlerResult = new HandlerResult<Language>();

        Language? language = null;
        if (parameters.Id != null)
        {
            language =
                await dbContext.Query<Language>().FirstOrDefaultAsync(l => l.Id == parameters.Id, token: cancellationToken);
            if (language != null)
            {
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                await user.AddAudit(language, $"Language ({language.LanguageCultureName})",
                    AuditExtensions.AuditAction.Delete, auditService,
                    cancellationToken);
                dbContext.Delete(language);
            }
        }
        else
        {
            language = await dbContext.Query<Language>().FirstOrDefaultAsync(
                l => l.LanguageIsoCode == parameters.LanguageIsoCode, token: cancellationToken);
            if (language != null)
            {
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                await user.AddAudit(language, $"Language ({language.LanguageCultureName})",
                    AuditExtensions.AuditAction.Delete, auditService,
                    cancellationToken);
                dbContext.Delete(language);
            }
        }

        return (await dbContext.SaveChangesAndLog(language, handlerResult, cacheService, extensionManager, cancellationToken))!;
    }

    /// <summary>
    /// Creates or updates a language dictionary and its texts. Clears relevant cache.
    /// </summary>
    /// <param name="parameters">Dictionary with texts to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result including success and messages.</returns>
    public async Task<HandlerResult<LanguageDictionary>> SaveLanguageDictionaryAsync(SaveLanguageDictionaryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = await userManager.GetUserAsync(authState.User);
        var handlerResult = new HandlerResult<LanguageDictionary>();

        if (parameters.LanguageDictionary != null)
        {
            var langTexts = parameters.LanguageDictionary.Texts;

            parameters.LanguageDictionary.Texts = [];

            // Save the language dictionary first
            var langDictionary =
                dbContext.Query<LanguageDictionary>().FirstOrDefault(x => x.Id == parameters.LanguageDictionary.Id);
            if (langDictionary == null)
            {
                langDictionary = parameters.LanguageDictionary;
                await dbContext.StoreAsync(langDictionary, cancellationToken);
            }
            else
            {
                parameters.LanguageDictionary.MapTo(langDictionary);
            }

            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            await user.AddAudit(langDictionary, $"Language Dictionary ({langDictionary.Key})",
                AuditExtensions.AuditAction.Update, auditService,
                cancellationToken);
            handlerResult = await dbContext.SaveChangesAndLog(langDictionary, handlerResult, cacheService, extensionManager, cancellationToken);
            if (handlerResult.Success)
            {
                var langTextResult = new HandlerResult<LanguageText>();
                foreach (var languageText in langTexts)
                {
                    var lt = dbContext.Query<LanguageText>().FirstOrDefault(x => x.Id == languageText.Id);
                    if (lt == null)
                    {
                        lt = languageText;
                        await dbContext.StoreAsync(lt, cancellationToken);
                    }
                    else
                    {
                        languageText.MapTo(lt);
                    }

                    var saveResult = await dbContext.SaveChangesAndLog(lt, langTextResult, cacheService, extensionManager, cancellationToken);
                    if (!saveResult.Success)
                    {
                        handlerResult.Success = false;
                        handlerResult.Messages = saveResult.Messages;
                        return handlerResult;
                    }
                }
            }
            else
            {
                return handlerResult;
            }

            // Clear Cache
            cacheService.ClearCachedItemsWithPrefix(nameof(LanguageDictionary));
            return handlerResult;
        }

        handlerResult.Success = false;
        return handlerResult;
    }

    /// <summary>
    /// Deletes a language dictionary by id and logs audit.
    /// </summary>
    /// <param name="parameters">Dictionary id to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result including success and messages.</returns>
    public async Task<HandlerResult<LanguageDictionary?>> DeleteLanguageDictionaryAsync(DeleteLanguageDictionaryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = await userManager.GetUserAsync(authState.User);
        var handlerResult = new HandlerResult<LanguageDictionary>();

        LanguageDictionary? langDict = null;
        if (parameters.Id != null)
        {
            langDict = await dbContext.Query<LanguageDictionary>()
                .FirstOrDefaultAsync(l => l.Id == parameters.Id, token: cancellationToken);
            if (langDict != null)
            {
                await user.AddAudit(langDict, $"Language Dictionary ({langDict.Key})",
                    AuditExtensions.AuditAction.Delete, auditService,
                    cancellationToken);
                dbContext.Delete(langDict);
            }
        }

        return (await dbContext.SaveChangesAndLog(langDict, handlerResult, cacheService, extensionManager, cancellationToken))!;
    }

    /// <summary>
    /// Returns a nested dictionary of all language keys and values per language from cache.
    /// </summary>
    /// <param name="parameters">Unused. Reserved for future options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary ISO -> (Key -> Value).</returns>
    public async Task<Dictionary<string, Dictionary<string, string>>> GetCachedAllLanguageDictionariesAsync(GetCachedAllLanguageDictionariesParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var cacheKey = typeof(LanguageDictionary).ToCacheKey("GetCachedAllLanguageDictionaries");
        
        return (await cacheService.GetSetCachedItemAsync(cacheKey, () =>
        {
            var allLanguages = dbContext.Query<Language>().Include(x=>x.LanguageTexts)
                ;
            var allLanguageDictionaries = dbContext.Query<LanguageDictionary>();
            var returnDict = new Dictionary<string, Dictionary<string, string>>();
            foreach (var language in allLanguages)
            {
                var langTextDict = new Dictionary<string, string>();
                foreach (var languageDictionary in allLanguageDictionaries)
                {
                    langTextDict.Add(languageDictionary.Key, language.LanguageTexts
                        .FirstOrDefault(x => x.LanguageDictionaryId == languageDictionary.Id)?.Value ?? string.Empty);
                }

                if (language.LanguageIsoCode != null) returnDict.Add(language.LanguageIsoCode, langTextDict);
            }
            return Task.FromResult(returnDict);
        }))!;
    }

    /// <summary>
    /// Returns language dictionaries for a data grid with filtering, sort and paging.
    /// </summary>
    /// <param name="parameters">Grid options including filter and order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Data grid result including total count and items.</returns>
    public async Task<DataGridResult<LanguageDictionary>> GetDataGridLanguageDictionaryAsync(DataGridLanguageDictionaryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        
        var result = new DataGridResult<LanguageDictionary>();

        // Now you have the DbSet<T> and can query it
        var query = dbContext.Query<LanguageDictionary>()
            .Include(x => x.Texts)
            //.AsSplitQuery()
            //.AsTracking()
            .AsQueryable();
        
        // Note: Dynamic LINQ string filtering is not supported with RavenDB
        // TODO: Implement proper filtering using expression trees
        // if (!string.IsNullOrEmpty(parameters.Filter))
        // {
        //     query = query.Where(parameters.Filter);
        // }

        // if (!string.IsNullOrEmpty(parameters.Order))
        // {
        //     query = query.OrderBy(parameters.Order);
        // }
        
        query = query.OrderBy(x => x.Key);

        // Important!!! Make sure the Count property of RadzenDataGrid is set.
        result.Count = await query.CountAsync(cancellationToken);

        // Perform paging via Skip and Take.
        result.Items = await query.Skip(parameters.Skip).Take(parameters.Take)
            .ToListAsync(cancellationToken);

        return result;
    }
}