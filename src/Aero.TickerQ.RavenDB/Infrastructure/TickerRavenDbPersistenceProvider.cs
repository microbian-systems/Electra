using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Models;

namespace Aero.TickerQ.RavenDB.Infrastructure;

internal sealed class TickerRavenDbPersistenceProvider<TTimeTicker, TCronTicker> : 
    ITickerPersistenceProvider<TTimeTicker, TCronTicker>
    where TTimeTicker : TimeTickerEntity<TTimeTicker>, new()
    where TCronTicker : CronTickerEntity, new()
{
    private readonly IDocumentStore _documentStore;
    private readonly ITickerClock _clock;
    private readonly ITickerQRedisContext _redisContext;
    private readonly string _lockHolder;

    public TickerRavenDbPersistenceProvider(
        IDocumentStore documentStore, 
        ITickerClock clock, 
        SchedulerOptionsBuilder optionsBuilder,
        ITickerQRedisContext redisContext)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _redisContext = redisContext ?? throw new ArgumentNullException(nameof(redisContext));
        _lockHolder = optionsBuilder?.NodeIdentifier ?? Environment.MachineName;
    }

    //#region Time Ticker Core Methods

    public async IAsyncEnumerable<TimeTickerEntity> QueueTimeTickers(
        TimeTickerEntity[] timeTickers, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var timeTicker in timeTickers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var existing = await session.LoadAsync<TTimeTicker>(timeTicker.Id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (existing == null || existing.UpdatedAt != timeTicker.UpdatedAt)
                continue;
            
            existing.Status = TickerStatus.Queued;
            existing.LockHolder = _lockHolder;
            existing.LockedAt = now;
            existing.UpdatedAt = now;
            
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            timeTicker.Status = TickerStatus.Queued;
            timeTicker.LockHolder = _lockHolder;
            timeTicker.LockedAt = now;
            timeTicker.UpdatedAt = now;
            
            yield return timeTicker;
        }
    }

    public async IAsyncEnumerable<TimeTickerEntity> QueueTimedOutTimeTickers(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var fallbackThreshold = now.AddSeconds(-1);
        
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<TTimeTicker>()
            .Where(x => x.ExecutionTime != null)
            .Where(x => x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued)
            .Where(x => x.ExecutionTime <= fallbackThreshold)
            .Where(x => x.LockedAt == null || x.LockHolder == _lockHolder);
        
        var timeTickersToUpdate = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        
        foreach (var ticker in timeTickersToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var existing = await session.LoadAsync<TTimeTicker>(ticker.Id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (existing == null || existing.UpdatedAt > ticker.UpdatedAt)
                continue;
            
            existing.Status = TickerStatus.InProgress;
            existing.LockHolder = _lockHolder;
            existing.LockedAt = now;
            existing.UpdatedAt = now;
            
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            yield return BuildTimeTickerHierarchy(ticker);
        }
    }

    public async Task ReleaseAcquiredTimeTickers(Guid[] timeTickerIds, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        using var session = _documentStore.OpenAsyncSession();
        
        var idsToRelease = timeTickerIds.Length == 0 
            ? await session.Query<TTimeTicker>()
                .Where(CanAcquireExpression())
                .Select(x => x.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : timeTickerIds.ToList();
        
        foreach (var id in idsToRelease)
        {
            var ticker = await session.LoadAsync<TTimeTicker>(id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (ticker == null || !CanAcquire(ticker))
                continue;
            
            ticker.Status = TickerStatus.Idle;
            ticker.LockHolder = null;
            ticker.LockedAt = null;
            ticker.UpdatedAt = now;
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TimeTickerEntity[]> GetEarliestTimeTickers(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var oneSecondAgo = now.AddSeconds(-1);
        
        using var session = _documentStore.OpenAsyncSession();
        
        var baseQuery = session.Query<TTimeTicker>()
            .Where(x => x.ExecutionTime != null)
            .Where(x => x.ExecutionTime >= oneSecondAgo)
            .Where(CanAcquireExpression())
            .OrderBy(x => x.ExecutionTime);
        
        var earliest = await baseQuery.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        
        if (earliest == null)
            return Array.Empty<TimeTickerEntity>();
        
        var minSecond = new DateTime(
            earliest.ExecutionTime!.Value.Year,
            earliest.ExecutionTime.Value.Month,
            earliest.ExecutionTime.Value.Day,
            earliest.ExecutionTime.Value.Hour,
            earliest.ExecutionTime.Value.Minute,
            earliest.ExecutionTime.Value.Second,
            DateTimeKind.Utc);
        
        var maxExecutionTime = minSecond.AddSeconds(1);
        
        var result = await session.Query<TTimeTicker>()
            .Where(x => x.ExecutionTime != null)
            .Where(x => x.ExecutionTime >= minSecond && x.ExecutionTime < maxExecutionTime)
            .Where(CanAcquireExpression())
            .OrderBy(x => x.ExecutionTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return result.Select(BuildTimeTickerHierarchy).ToArray();
    }

    public async Task<int> UpdateTimeTicker(InternalFunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var ticker = await session.LoadAsync<TTimeTicker>(functionContext.TickerId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        
        if (ticker == null)
            return 0;
        
        ApplyFunctionContextToTicker(ticker, functionContext);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        return 1;
    }

    public async Task<byte[]> GetTimeTickerRequest(Guid id, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var ticker = await session.LoadAsync<TTimeTicker>(id.ToString(), cancellationToken)
            .ConfigureAwait(false);
        
        return ticker?.Request;
    }

    public async Task UpdateTimeTickersWithUnifiedContext(
        Guid[] timeTickerIds, 
        InternalFunctionContext functionContext, 
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var id in timeTickerIds)
        {
            var ticker = await session.LoadAsync<TTimeTicker>(id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (ticker != null)
                ApplyFunctionContextToTicker(ticker, functionContext);
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TimeTickerEntity[]> AcquireImmediateTimeTickersAsync(
        Guid[] ids, 
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            return Array.Empty<TimeTickerEntity>();
        
        var now = _clock.UtcNow;
        using var session = _documentStore.OpenAsyncSession();
        
        var acquired = new List<TimeTickerEntity>();
        
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var ticker = await session.LoadAsync<TTimeTicker>(id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (ticker == null || !CanAcquire(ticker))
                continue;
            
            ticker.Status = TickerStatus.InProgress;
            ticker.LockHolder = _lockHolder;
            ticker.LockedAt = now;
            ticker.UpdatedAt = now;
            
            acquired.Add(BuildTimeTickerHierarchy(ticker));
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return acquired.ToArray();
    }

    //#endregion

    //#region Time Ticker Shared Methods

    public async Task<TTimeTicker> GetTimeTickerById(Guid id, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var ticker = await session.LoadAsync<TTimeTicker>(id.ToString(), cancellationToken)
            .ConfigureAwait(false);
        
        return ticker == null ? null : BuildTickerHierarchy(ticker);
    }

    public async Task<TTimeTicker[]> GetTimeTickers(
        Expression<Func<TTimeTicker, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<TTimeTicker>()
            .Where(x => x.ParentId == null)
            .OrderByDescending(x => x.ExecutionTime);
        
        if (predicate != null)
            query = query.Where(predicate);
        
        var results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.Select(BuildTickerHierarchy).ToArray();
    }

    public async Task<PaginationResult<TTimeTicker>> GetTimeTickersPaginated(
        Expression<Func<TTimeTicker, bool>> predicate, 
        int pageNumber, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<TTimeTicker>()
            .Where(x => x.ParentId == null)
            .OrderByDescending(x => x.ExecutionTime);
        
        if (predicate != null)
            query = query.Where(predicate);
        
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return new PaginationResult<TTimeTicker>
        {
            Items = items.Select(BuildTickerHierarchy).ToArray(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<int> AddTimeTickers(TTimeTicker[] tickers, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var ticker in tickers)
        {
            await session.StoreAsync(ticker, cancellationToken).ConfigureAwait(false);
            
            if (ticker.Children != null)
            {
                foreach (var child in ticker.Children)
                {
                    child.ParentId = ticker.Id;
                    await session.StoreAsync(child, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tickers.Length;
    }

    public async Task<int> UpdateTimeTickers(TTimeTicker[] tickers, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var ticker in tickers)
        {
            var existing = await session.LoadAsync<TTimeTicker>(ticker.Id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (existing != null)
            {
                var metadata = session.Advanced.GetMetadataFor(existing);
                var changeVector = metadata["@change-vector"].ToString();
                await session.StoreAsync(ticker, changeVector, ticker.Id.ToString(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tickers.Length;
    }

    public async Task<int> RemoveTimeTickers(Guid[] tickerIds, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var count = 0;
        foreach (var id in tickerIds)
        {
            var ticker = await session.LoadAsync<TTimeTicker>(id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (ticker != null)
            {
                session.Delete(ticker);
                count++;
                
                var children = await session.Query<TTimeTicker>()
                    .Where(x => x.ParentId == id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                
                foreach (var child in children)
                {
                    session.Delete(child);
                    count++;
                }
            }
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    public async Task ReleaseDeadNodeTimeTickerResources(string instanceIdentifier, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        using var session = _documentStore.OpenAsyncSession();
        
        var releasable = await session.Query<TTimeTicker>()
            .Where(x => (x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued))
            .Where(x => x.LockHolder == instanceIdentifier || x.LockedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        foreach (var ticker in releasable)
        {
            ticker.LockHolder = null;
            ticker.LockedAt = null;
            ticker.Status = TickerStatus.Idle;
            ticker.UpdatedAt = now;
        }
        
        var inProgress = await session.Query<TTimeTicker>()
            .Where(x => x.LockHolder == instanceIdentifier && x.Status == TickerStatus.InProgress)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        foreach (var ticker in inProgress)
        {
            ticker.Status = TickerStatus.Skipped;
            ticker.SkippedReason = "Node is not alive!";
            ticker.ExecutedAt = now;
            ticker.UpdatedAt = now;
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    //#endregion

    //#region Cron Ticker Core Methods

    public async Task MigrateDefinedCronTickers(
        (string Function, string Expression)[] cronTickers, 
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        using var session = _documentStore.OpenAsyncSession();
        
        var allRegisteredFunctions = TickerFunctionProvider.TickerFunctions.Keys
            .ToHashSet(StringComparer.Ordinal);
        
        var orphanedCron = await session.Query<TCronTicker>()
            .Where(c => !allRegisteredFunctions.Contains(c.Function))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        foreach (var orphaned in orphanedCron)
        {
            var occurrences = await session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
                .Where(o => o.CronTickerId == orphaned.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            
            foreach (var occ in occurrences)
                session.Delete(occ);
            
            session.Delete(orphaned);
        }
        
        var functions = cronTickers.Select(x => x.Function).ToList();
        var existing = await session.Query<TCronTicker>()
            .Where(c => functions.Contains(c.Function))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        var existingByFunction = existing.ToDictionary(c => c.Function);
        
        foreach (var (function, expression) in cronTickers)
        {
            if (existingByFunction.TryGetValue(function, out var cron))
            {
                if (!string.Equals(cron.Expression, expression, StringComparison.Ordinal))
                {
                    cron.Expression = expression;
                    cron.UpdatedAt = now;
                }
            }
            else
            {
                var entity = new TCronTicker
                {
                    Id = Guid.NewGuid(),
                    Function = function,
                    Expression = expression,
                    InitIdentifier = $"RavenDb_Seeded_{function}",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Request = Array.Empty<byte>()
                };
                await session.StoreAsync(entity, cancellationToken).ConfigureAwait(false);
            }
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        if (_redisContext.HasRedisConnection)
            await _redisContext.DistributedCache.RemoveAsync("cron:expressions", cancellationToken)
                .ConfigureAwait(false);
    }

    public async Task<CronTickerEntity[]> GetAllCronTickerExpressions(CancellationToken cancellationToken)
    {
        return await _redisContext.GetOrSetArrayAsync(
            cacheKey: "cron:expressions",
            factory: async (ct) =>
            {
                using var session = _documentStore.OpenAsyncSession();
                return await session.Query<TCronTicker>()
                    .Select(x => new CronTickerEntity
                    {
                        Id = x.Id,
                        Function = x.Function,
                        Expression = x.Expression,
                        Retries = x.Retries,
                        RetryIntervals = x.RetryIntervals
                    })
                    .ToArrayAsync(ct)
                    .ConfigureAwait(false);
            },
            expiration: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken);
    }

    //#endregion

    //#region Cron Ticker Shared Methods

    public async Task<TCronTicker> GetCronTickerById(Guid id, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.LoadAsync<TCronTicker>(id.ToString(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TCronTicker[]> GetCronTickers(
        Expression<Func<TCronTicker, bool>> predicate, 
        CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<TCronTicker>()
            .OrderByDescending(x => x.CreatedAt);
        
        if (predicate != null)
            query = query.Where(predicate);
        
        return await query.ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PaginationResult<TCronTicker>> GetCronTickersPaginated(
        Expression<Func<TCronTicker, bool>> predicate, 
        int pageNumber, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<TCronTicker>()
            .OrderByDescending(x => x.CreatedAt);
        
        if (predicate != null)
            query = query.Where(predicate);
        
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return new PaginationResult<TCronTicker>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<int> InsertCronTickers(TCronTicker[] tickers, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var ticker in tickers)
            await session.StoreAsync(ticker, cancellationToken).ConfigureAwait(false);
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        if (_redisContext.HasRedisConnection)
            await _redisContext.DistributedCache.RemoveAsync("cron:expressions", cancellationToken)
                .ConfigureAwait(false);
        
        return tickers.Length;
    }

    public async Task<int> UpdateCronTickers(TCronTicker[] cronTickers, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var ticker in cronTickers)
        {
            var existing = await session.LoadAsync<TCronTicker>(ticker.Id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (existing != null)
            {
                var metadata = session.Advanced.GetMetadataFor(existing);
                var changeVector = metadata["@change-vector"].ToString();
                await session.StoreAsync(ticker, changeVector, ticker.Id.ToString(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        if (_redisContext.HasRedisConnection)
            await _redisContext.DistributedCache.RemoveAsync("cron:expressions", cancellationToken)
                .ConfigureAwait(false);
        
        return cronTickers.Length;
    }

    public async Task<int> RemoveCronTickers(Guid[] cronTickerIds, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var id in cronTickerIds)
        {
            var ticker = await session.LoadAsync<TCronTicker>(id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (ticker != null)
                session.Delete(ticker);
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        if (_redisContext.HasRedisConnection)
            await _redisContext.DistributedCache.RemoveAsync("cron:expressions", cancellationToken)
                .ConfigureAwait(false);
        
        return cronTickerIds.Length;
    }

    //#endregion

    //#region Cron Occurrence Core Methods

    public async Task<CronTickerOccurrenceEntity<TCronTicker>> GetEarliestAvailableCronOccurrence(
        Guid[] ids, 
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var mainSchedulerThreshold = now.AddSeconds(-1);
        
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
            .Where(x => ids.Contains(x.CronTickerId))
            .Where(x => x.ExecutionTime >= mainSchedulerThreshold)
            .Where(CanAcquireCronOccurrenceExpression())
            .OrderBy(x => x.ExecutionTime);
        
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<CronTickerOccurrenceEntity<TCronTicker>> QueueCronTickerOccurrences(
        (DateTime Key, InternalManagerContext[] Items) cronTickerOccurrences, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var executionTime = cronTickerOccurrences.Key;
        
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var item in cronTickerOccurrences.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (item.NextCronOccurrence == null)
            {
                var newOccurrence = new CronTickerOccurrenceEntity<TCronTicker>
                {
                    Id = Guid.NewGuid(),
                    CronTickerId = item.Id,
                    ExecutionTime = executionTime,
                    Status = TickerStatus.Queued,
                    LockHolder = _lockHolder,
                    LockedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                
                await session.StoreAsync(newOccurrence, cancellationToken).ConfigureAwait(false);
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                
                newOccurrence.CronTicker = new TCronTicker
                {
                    Id = item.Id,
                    Function = item.FunctionName,
                    InitIdentifier = _lockHolder,
                    Expression = item.Expression,
                    Retries = item.Retries,
                    RetryIntervals = item.RetryIntervals
                };
                
                yield return newOccurrence;
            }
            else
            {
                var existing = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
                    item.NextCronOccurrence.Id.ToString(), cancellationToken)
                    .ConfigureAwait(false);
                
                if (existing == null || !CanAcquireCronOccurrence(existing))
                    continue;
                
                existing.Status = TickerStatus.Queued;
                existing.LockHolder = _lockHolder;
                existing.LockedAt = now;
                existing.UpdatedAt = now;
                
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                
                yield return existing;
            }
        }
    }

    public async IAsyncEnumerable<CronTickerOccurrenceEntity<TCronTicker>> QueueTimedOutCronTickerOccurrences(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var fallbackThreshold = now.AddSeconds(-1);
        
        using var session = _documentStore.OpenAsyncSession();
        
        var occurrencesToUpdate = await session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
            .Where(x => x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued)
            .Where(x => x.ExecutionTime <= fallbackThreshold)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        foreach (var occurrence in occurrencesToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var existing = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
                occurrence.Id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (existing == null || existing.UpdatedAt > occurrence.UpdatedAt)
                continue;
            
            existing.Status = TickerStatus.InProgress;
            existing.LockHolder = _lockHolder;
            existing.LockedAt = now;
            existing.UpdatedAt = now;
            
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            yield return existing;
        }
    }

    public async Task UpdateCronTickerOccurrence(
        InternalFunctionContext functionContext, 
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var occurrence = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
            functionContext.TickerId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        
        if (occurrence != null)
        {
            ApplyFunctionContextToCronOccurrence(occurrence, functionContext);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ReleaseAcquiredCronTickerOccurrences(
        Guid[] occurrenceIds, 
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        using var session = _documentStore.OpenAsyncSession();
        
        var idsToRelease = occurrenceIds.Length == 0 
            ? await session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
                .Where(CanAcquireCronOccurrenceExpression())
                .Select(x => x.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : occurrenceIds.ToList();
        
        foreach (var id in idsToRelease)
        {
            var occurrence = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
                id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (occurrence == null || !CanAcquireCronOccurrence(occurrence))
                continue;
            
            occurrence.Status = TickerStatus.Idle;
            occurrence.LockHolder = null;
            occurrence.LockedAt = null;
            occurrence.UpdatedAt = now;
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> GetCronTickerOccurrenceRequest(
        Guid tickerId, 
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var occurrence = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
            tickerId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        
        if (occurrence?.CronTicker != null)
            return occurrence.CronTicker.Request;
        
        if (occurrence != null)
        {
            var cronTicker = await session.LoadAsync<TCronTicker>(occurrence.CronTickerId.ToString(), cancellationToken)
                .ConfigureAwait(false);
            return cronTicker?.Request;
        }
        
        return null;
    }

    public async Task UpdateCronTickerOccurrencesWithUnifiedContext(
        Guid[] cronOccurrenceIds, 
        InternalFunctionContext functionContext, 
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var id in cronOccurrenceIds)
        {
            var occurrence = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
                id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (occurrence != null)
                ApplyFunctionContextToCronOccurrence(occurrence, functionContext);
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseDeadNodeOccurrenceResources(
        string instanceIdentifier, 
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        using var session = _documentStore.OpenAsyncSession();
        
        var releasable = await session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
            .Where(x => (x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued))
            .Where(x => x.LockHolder == instanceIdentifier || x.LockedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        foreach (var occurrence in releasable)
        {
            occurrence.LockHolder = null;
            occurrence.LockedAt = null;
            occurrence.Status = TickerStatus.Idle;
            occurrence.UpdatedAt = now;
        }
        
        var inProgress = await session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
            .Where(x => x.LockHolder == instanceIdentifier && x.Status == TickerStatus.InProgress)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        foreach (var occurrence in inProgress)
        {
            occurrence.Status = TickerStatus.Skipped;
            occurrence.SkippedReason = "Node is not alive!";
            occurrence.ExecutedAt = now;
            occurrence.UpdatedAt = now;
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    //#endregion

    //#region Cron Occurrence Shared Methods

    public async Task<CronTickerOccurrenceEntity<TCronTicker>[]> GetAllCronTickerOccurrences(
        Expression<Func<CronTickerOccurrenceEntity<TCronTicker>, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
            .OrderByDescending(x => x.CreatedAt);
        
        if (predicate != null)
            query = query.Where(predicate);
        
        return await query.ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PaginationResult<CronTickerOccurrenceEntity<TCronTicker>>> GetAllCronTickerOccurrencesPaginated(
        Expression<Func<CronTickerOccurrenceEntity<TCronTicker>, bool>> predicate, 
        int pageNumber, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        var query = session.Query<CronTickerOccurrenceEntity<TCronTicker>>()
            .OrderByDescending(x => x.CreatedAt);
        
        if (predicate != null)
            query = query.Where(predicate);
        
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return new PaginationResult<CronTickerOccurrenceEntity<TCronTicker>>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<int> InsertCronTickerOccurrences(
        CronTickerOccurrenceEntity<TCronTicker>[] cronTickerOccurrences, 
        CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var occurrence in cronTickerOccurrences)
            await session.StoreAsync(occurrence, cancellationToken).ConfigureAwait(false);
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return cronTickerOccurrences.Length;
    }

    public async Task<int> RemoveCronTickerOccurrences(
        Guid[] cronTickerOccurrences, 
        CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        
        foreach (var id in cronTickerOccurrences)
        {
            var occurrence = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
                id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (occurrence != null)
                session.Delete(occurrence);
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return cronTickerOccurrences.Length;
    }

    public async Task<CronTickerOccurrenceEntity<TCronTicker>[]> AcquireImmediateCronOccurrencesAsync(
        Guid[] occurrenceIds, 
        CancellationToken cancellationToken = default)
    {
        if (occurrenceIds == null || occurrenceIds.Length == 0)
            return Array.Empty<CronTickerOccurrenceEntity<TCronTicker>>();
        
        var now = _clock.UtcNow;
        using var session = _documentStore.OpenAsyncSession();
        
        var acquired = new List<CronTickerOccurrenceEntity<TCronTicker>>();
        
        foreach (var id in occurrenceIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var occurrence = await session.LoadAsync<CronTickerOccurrenceEntity<TCronTicker>>(
                id.ToString(), cancellationToken)
                .ConfigureAwait(false);
            
            if (occurrence == null || !CanAcquireCronOccurrence(occurrence))
                continue;
            
            occurrence.Status = TickerStatus.InProgress;
            occurrence.LockHolder = _lockHolder;
            occurrence.LockedAt = now;
            occurrence.UpdatedAt = now;
            
            acquired.Add(occurrence);
        }
        
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return acquired.ToArray();
    }

    //#endregion

    //#region Helper Methods

    private bool CanAcquire(TTimeTicker ticker)
    {
        return ((ticker.Status == TickerStatus.Idle || ticker.Status == TickerStatus.Queued) && 
                ticker.LockHolder == _lockHolder) ||
               ((ticker.Status == TickerStatus.Idle || ticker.Status == TickerStatus.Queued) && 
                ticker.LockedAt == null);
    }

    private bool CanAcquireCronOccurrence(CronTickerOccurrenceEntity<TCronTicker> occurrence)
    {
        return ((occurrence.Status == TickerStatus.Idle || occurrence.Status == TickerStatus.Queued) && 
                occurrence.LockHolder == _lockHolder) ||
               ((occurrence.Status == TickerStatus.Idle || occurrence.Status == TickerStatus.Queued) && 
                occurrence.LockedAt == null);
    }

    private Expression<Func<TTimeTicker, bool>> CanAcquireExpression()
    {
        return x => ((x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued) && 
                     x.LockHolder == _lockHolder) ||
                    ((x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued) && 
                     x.LockedAt == null);
    }

    private Expression<Func<CronTickerOccurrenceEntity<TCronTicker>, bool>> CanAcquireCronOccurrenceExpression()
    {
        return x => ((x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued) && 
                     x.LockHolder == _lockHolder) ||
                    ((x.Status == TickerStatus.Idle || x.Status == TickerStatus.Queued) && 
                     x.LockedAt == null);
    }

    private TTimeTicker BuildTickerHierarchy(TTimeTicker ticker)
    {
        if (ticker == null) return null;
        
        using var session = _documentStore.OpenAsyncSession();
        
        var children = session.Query<TTimeTicker>()
            .Where(x => x.ParentId == ticker.Id)
            .ToListAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
        
        ticker.Children = children;
        
        foreach (var child in children)
        {
            var grandchildren = session.Query<TTimeTicker>()
                .Where(x => x.ParentId == child.Id)
                .ToListAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            
            child.Children = grandchildren;
        }
        
        return ticker;
    }

    private TimeTickerEntity BuildTimeTickerHierarchy(TTimeTicker ticker)
    {
        if (ticker == null) return null;
        
        using var session = _documentStore.OpenAsyncSession();
        
        var children = session.Query<TTimeTicker>()
            .Where(x => x.ParentId == ticker.Id && x.ExecutionTime == null)
            .ToListAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
        
        var result = new TimeTickerEntity
        {
            Id = ticker.Id,
            Function = ticker.Function,
            Status = ticker.Status,
            Retries = ticker.Retries,
            RetryCount = ticker.RetryCount,
            ExecutionTime = ticker.ExecutionTime,
            InitIdentifier = ticker.InitIdentifier,
            LockHolder = ticker.LockHolder,
            LockedAt = ticker.LockedAt,
            ParentId = ticker.ParentId,
            Request = ticker.Request,
            ExceptionMessage = ticker.ExceptionMessage,
            SkippedReason = ticker.SkippedReason,
            ElapsedTime = ticker.ElapsedTime,
            RetryIntervals = ticker.RetryIntervals,
            RunCondition = ticker.RunCondition,
            ExecutedAt = ticker.ExecutedAt,
            CreatedAt = ticker.CreatedAt,
            UpdatedAt = ticker.UpdatedAt,
            Description = ticker.Description,
            Children = children.Select(c => new TimeTickerEntity
            {
                Id = c.Id,
                Function = c.Function,
                Retries = c.Retries,
                RetryIntervals = c.RetryIntervals,
                RunCondition = c.RunCondition,
                Children = session.Query<TTimeTicker>()
                    .Where(x => x.ParentId == c.Id)
                    .ToListAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult()
                    .Select(gc => new TimeTickerEntity
                    {
                        Id = gc.Id,
                        Function = gc.Function,
                        Retries = gc.Retries,
                        RetryIntervals = gc.RetryIntervals,
                        RunCondition = gc.RunCondition
                    })
                    .ToList()
            }).ToList()
        };
        
        return result;
    }

    private void ApplyFunctionContextToTicker(TTimeTicker ticker, InternalFunctionContext context)
    {
        var propsToUpdate = context.GetPropsToUpdate();
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.Status)) &&
            context.Status != TickerStatus.Skipped)
        {
            ticker.Status = context.Status;
        }
        else if (propsToUpdate.Contains(nameof(InternalFunctionContext.Status)))
        {
            ticker.Status = context.Status;
            ticker.SkippedReason = context.ExceptionDetails;
        }
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.ExecutedAt)))
            ticker.ExecutedAt = context.ExecutedAt;
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.ExceptionDetails)) &&
            context.Status != TickerStatus.Skipped)
        {
            ticker.ExceptionMessage = context.ExceptionDetails;
        }
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.ElapsedTime)))
            ticker.ElapsedTime = context.ElapsedTime;
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.RetryCount)))
            ticker.RetryCount = context.RetryCount;
        
        ticker.LockHolder = null;
        ticker.LockedAt = null;
        ticker.UpdatedAt = _clock.UtcNow;
    }

    private void ApplyFunctionContextToCronOccurrence(
        CronTickerOccurrenceEntity<TCronTicker> occurrence, 
        InternalFunctionContext context)
    {
        var propsToUpdate = context.GetPropsToUpdate();
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.Status)) &&
            context.Status != TickerStatus.Skipped)
        {
            occurrence.Status = context.Status;
        }
        else if (propsToUpdate.Contains(nameof(InternalFunctionContext.Status)))
        {
            occurrence.Status = context.Status;
            occurrence.SkippedReason = context.ExceptionDetails;
        }
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.ExecutedAt)))
            occurrence.ExecutedAt = context.ExecutedAt;
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.ExceptionDetails)) &&
            context.Status != TickerStatus.Skipped)
        {
            occurrence.ExceptionMessage = context.ExceptionDetails;
        }
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.ElapsedTime)))
            occurrence.ElapsedTime = context.ElapsedTime;
        
        if (propsToUpdate.Contains(nameof(InternalFunctionContext.RetryCount)))
            occurrence.RetryCount = context.RetryCount;
        
        occurrence.LockHolder = null;
        occurrence.LockedAt = null;
        occurrence.UpdatedAt = _clock.UtcNow;
    }

    //#endregion
}
