using System.Threading.Channels;
using ThrowGuard;

namespace Electra.Core.Events;

public sealed class EventListener(ILogger<EventListener> log)
    : EventListener<object>(log){}

public interface IEventListener : IEventListener<object> { }
public interface IEventListener<T> where T : class
{
    ChannelReader<T> Reader { get; }
    ChannelWriter<T> Writer { get; }
    Task Handle(T @event, CancellationToken ct);
    Task WaitForProcessing(T @event, CancellationToken ct);
}

public class EventListener<T>(ILogger<EventListener<T>> log)
    : IEventListener<T> where T : class
{
    protected readonly Channel<T> events = Channel.CreateUnbounded<T>();

    public ChannelReader<T> Reader => events.Reader;
    public ChannelWriter<T> Writer => events.Writer;

    public async Task Handle(T @event, CancellationToken ct)
    {
        log.LogInformation("Event received: {Event}", @event);
        await events.Writer.WriteAsync(@event, ct).ConfigureAwait(false);
    }

    public async Task WaitForProcessing(T @event, CancellationToken ct)
    {
        log.LogInformation("Waiting for event: {Event}", @event);
        await foreach (var item in events.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            if (item.Equals(@event))
                return;
        }

        log.LogError("No events were found");
        Throw.AppException("No events were found");
    }
}

public interface IEventBus {Task Publish(object @event, CancellationToken ct);}
public class EventCatcher: IEventBus
{
    private readonly EventListener listener;
    private readonly IEventBus eventBus;

    public EventCatcher(EventListener listener, IEventBus eventBus)
    {
        this.listener = listener;
        this.eventBus = eventBus;
    }

    public async Task Publish(object @event, CancellationToken ct)
    {
        await eventBus.Publish(@event, ct).ConfigureAwait(false);

        await listener.Handle(@event, ct).ConfigureAwait(false);
    }
}

// https://event-driven.io/en/testing_asynchronous_processes_with_a_little_help_from_dotnet_channels/

// var eventListener = new EventListener();
//
// services
//     .AddScoped<IEventBus>(sp =>
//         new EventCatcher(
//             eventListener,
//             sp.GetRequiredService<EventBus>()
//         )
//     )

// [Fact]
// public async Task AddUser_ShouldEventuallyUpdateUserDetailsReadModel()
// {
//     // Given
//     var userId = Guid.NewGuid();
//     var userName = "Oscar the Grouch";
//     var command = new AddUser(userId, userName);
//     var userAdded = new UserAdded(userId, userName);
//
//     // When
//     await commandHandler.Handle(command, ct);
//
//     // Then
//     await eventListener.WaitForProcessing(userAdded , ct);
//
//     var userDetails = awai GetUserDetails(userId, ct);
//     userDetails.Should().NotBeNull();
//     userDetails.Id.Should().Be(userId);
//     userDetails.Name.Should().Be(userName);
// }