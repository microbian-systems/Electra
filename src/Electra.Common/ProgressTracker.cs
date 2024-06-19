using System.Threading;

namespace Electra.Common;

public class ProgressTracker(Action<double> updater, ILogger<ProgressTracker> log)
{
    public event Action<double> progressUpdated = updater;

    public void Process<T>(IList<T> items, Action<T> processItem)
    {
        if(items is null || items.Count == 0)
        {
            log.LogInformation("no items to process in progress tracker");
            //yield break;
        }

        var count = items.Count;
        var percentage = 0D;

        for(var i=0; i<items.Count; i++)
        {
            processItem(items[i]);
            var progress = (Math.Round(1.0 * (i+1) / count, 1) * 100);

            if (progress != percentage)
            {
                percentage = progress;
                log.LogInformation("{0}% complete", percentage);
                progressUpdated?.Invoke(percentage);
            }
        }
    }

    public async IAsyncEnumerable<T> Process<T>(IReadOnlyList<T> items, Func<T, T> processItem)
    {
        if(items is null || items.Count == 0)
        {
            log.LogInformation("no items to process in progress tracker");
            yield break;
        }

        var count = items.Count;
        var percentage = 0;

        for(var i=0; i<items.Count; i++)
        {
            var result = processItem(items[i]);
            var progress = (int)(Math.Round(1.0 * (i+1) / count, 1) * 100);

            if (progress != percentage)
            {
                percentage = progress;
                log.LogInformation("{pct}% complete", percentage);
                progressUpdated?.Invoke(percentage);
            }
            yield return result;
        }
    }

    public async IAsyncEnumerable<TRes> Process<T, TRes>(IReadOnlyList<T> items, Func<T, TRes> processItem)
    {
        if(items is null || items.Count == 0)
        {
            log.LogInformation("no items to process in progress tracker");
            yield break;
        }

        var count = items.Count;
        var percentage = 0;

        for(var i=0; i<items.Count; i++)
        {
            var result = processItem(items[i]);
            var progress = (int)(Math.Round(1.0 * (i+1) / count, 1) * 100);

            if (progress != percentage)
            {
                percentage = progress;
                log.LogInformation("{pct}% complete", percentage);
                progressUpdated?.Invoke(percentage);
            }
            yield return result;
        }
    }
}