using Microsoft.Extensions.Logging;

namespace Electra.Events;

public interface IEventHandlerBase { }

public abstract class EventHandlerBase(ILogger<EventHandlerBase> log) : IEventHandlerBase
{
    /// <summary>
    /// Cancelation token support for event handlers
    /// </summary>
    /// <param name="timeout">the timeout in minutes</param>
    /// <returns><see cref="CancellationToken"/></returns>
    protected CancellationToken GetToken(int timeout = 10) 
        => new CancellationTokenSource(TimeSpan.FromMinutes(timeout)).Token;
};