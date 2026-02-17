# Aero.Events

Event sourcing and domain events for the Aero framework.

## Overview

`Aero.Events` provides infrastructure for domain events, event sourcing, and event-driven architecture patterns.

## Key Components

### Domain Events

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }
}

public abstract class DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public abstract string EventType { get; }
}

public class OrderCreatedEvent : DomainEvent
{
    public override string EventType => nameof(OrderCreatedEvent);
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}

public class OrderStatusChangedEvent : DomainEvent
{
    public override string EventType => nameof(OrderStatusChangedEvent);
    public string OrderId { get; set; }
    public string OldStatus { get; set; }
    public string NewStatus { get; set; }
}
```

### Event Dispatcher

```csharp
public interface IEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents);
}

public class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(IServiceProvider serviceProvider, ILogger<EventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod("HandleAsync");
                await (Task)method!.Invoke(handler, new object[] { domainEvent })!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {EventType}", domainEvent.EventType);
            }
        }
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent);
        }
    }
}
```

### Event Handlers

```csharp
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}

public class OrderCreatedEmailHandler : IDomainEventHandler<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;

    public OrderCreatedEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(OrderCreatedEvent domainEvent)
    {
        await _emailService.SendOrderConfirmationAsync(
            domainEvent.CustomerId,
            domainEvent.OrderId,
            domainEvent.Total);
    }
}

public class OrderCreatedNotificationHandler : IDomainEventHandler<OrderCreatedEvent>
{
    private readonly INotificationService _notificationService;

    public async Task HandleAsync(OrderCreatedEvent domainEvent)
    {
        await _notificationService.NotifyAdminsAsync(
            $"New order placed: {domainEvent.OrderId} for ${domainEvent.Total}");
    }
}
```

### Event Store

```csharp
public interface IEventStore
{
    Task AppendAsync(string aggregateId, IEnumerable<IDomainEvent> events);
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(string aggregateId);
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(string aggregateId, long fromVersion);
}

public class MartenEventStore : IEventStore
{
    private readonly IDocumentSession _session;

    public MartenEventStore(IDocumentSession session)
    {
        _session = session;
    }

    public async Task AppendAsync(string aggregateId, IEnumerable<IDomainEvent> events)
    {
        _session.Events.Append(aggregateId, events.ToArray());
        await _session.SaveChangesAsync();
    }

    public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(string aggregateId)
    {
        var stream = await _session.Events.FetchStreamAsync(aggregateId);
        return stream.Select(e => (IDomainEvent)e.Data);
    }

    public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
        string aggregateId, 
        long fromVersion)
    {
        var stream = await _session.Events.FetchStreamAsync(aggregateId, fromVersion);
        return stream.Select(e => (IDomainEvent)e.Data);
    }
}
```

## Usage

```csharp
public class OrderService
{
    private readonly IGenericRepository<Order> _repository;
    private readonly IEventDispatcher _eventDispatcher;

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            Items = request.Items,
            Total = request.Items.Sum(i => i.Quantity * i.Price),
            Status = "Pending"
        };

        await _repository.InsertAsync(order);

        // Dispatch domain event
        await _eventDispatcher.DispatchAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Total = order.Total,
            OrderDate = DateTime.UtcNow
        });

        return order;
    }

    public async Task UpdateOrderStatusAsync(string orderId, string newStatus)
    {
        var order = await _repository.FindByIdAsync(orderId);
        var oldStatus = order.Status;
        order.Status = newStatus;
        await _repository.UpdateAsync(order);

        await _eventDispatcher.DispatchAsync(new OrderStatusChangedEvent
        {
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = newStatus
        });
    }
}
```

## Configuration

```csharp
builder.Services.AddAeroEvents();

public static class EventExtensions
{
    public static IServiceCollection AddAeroEvents(this IServiceCollection services)
    {
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddScoped<IEventStore, MartenEventStore>();

        // Register all event handlers
        services.Scan(scan => scan
            .FromAssemblyOf<Program>()
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
```

## Related Packages

- `Aero.Marten` - Event store implementation
- `Aero.RavenDB.ES` - Alternative event store
- `Aero.Core` - Entity definitions
