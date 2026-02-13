using System;
using System.Linq.Expressions;
using Raven.Client.Documents.Linq;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Enums;

namespace Aero.TickerQ.RavenDB;

public static class TickerQueryExtensions
{
    public static IRavenQueryable<TTimeTicker> WhereCanAcquire<TTimeTicker>(this IRavenQueryable<TTimeTicker> q, string lockHolder) where TTimeTicker : TimeTickerEntity<TTimeTicker>
    {
        Expression<Func<TTimeTicker, bool>> pred = e =>
            ((e.Status == TickerStatus.Idle || e.Status == TickerStatus.Queued) && e.LockHolder == lockHolder) || 
            ((e.Status == TickerStatus.Idle || e.Status == TickerStatus.Queued) && e.LockedAt == null);
           
        return q.Where(pred);
    }
    
    public static IRavenQueryable<CronTickerOccurrenceEntity<TCronTicker>> WhereCanAcquire<TCronTicker>(this IRavenQueryable<CronTickerOccurrenceEntity<TCronTicker>> q, string lockHolder) where TCronTicker : CronTickerEntity
    {
        return q.Where(e =>
            ((e.Status == TickerStatus.Idle || e.Status == TickerStatus.Queued) && e.LockHolder == lockHolder) || 
            ((e.Status == TickerStatus.Idle || e.Status == TickerStatus.Queued) && e.LockedAt == null)
        );
    }
}