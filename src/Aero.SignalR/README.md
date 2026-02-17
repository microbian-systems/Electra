# Aero.SignalR

Real-time communication hub for the Aero framework.

## Overview

`Aero.SignalR` provides real-time communication capabilities for Aero applications. It enables server-to-client push notifications, bidirectional communication, and real-time updates across connected clients.

## Key Components

### Base Hub

```csharp
public abstract class AeroHub : Hub
{
    protected readonly ILogger Logger;
    protected readonly ICurrentUserService CurrentUser;

    protected AeroHub(ILogger logger, ICurrentUserService currentUser)
    {
        Logger = logger;
        CurrentUser = currentUser;
    }

    public override async Task OnConnectedAsync()
    {
        Logger.LogInformation("Client {ConnectionId} connected", Context.ConnectionId);
        
        if (CurrentUser.IsAuthenticated)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{CurrentUser.UserId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        
        if (exception != null)
        {
            Logger.LogError(exception, "Client disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    protected string GetUserId() => CurrentUser.UserId ?? throw new UnauthorizedAccessException();
    protected bool IsAuthenticated => CurrentUser.IsAuthenticated;
}
```

### Notification Hub

```csharp
public interface INotificationClient
{
    Task ReceiveNotification(Notification notification);
    Task NotificationRead(string notificationId);
    Task NotificationCountUpdated(int count);
}

public class NotificationHub : AeroHub<INotificationClient>
{
    private readonly INotificationService _notificationService;

    public NotificationHub(
        INotificationService notificationService,
        ILogger<NotificationHub> logger,
        ICurrentUserService currentUser) 
        : base(logger, currentUser)
    {
        _notificationService = notificationService;
    }

    public async Task MarkAsRead(string notificationId)
    {
        var userId = GetUserId();
        await _notificationService.MarkAsReadAsync(notificationId, userId);
        await Clients.Caller.NotificationRead(notificationId);
    }

    public async Task GetUnreadCount()
    {
        var userId = GetUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        await Clients.Caller.NotificationCountUpdated(count);
    }

    public async Task SubscribeToGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Logger.LogInformation("User {UserId} subscribed to group {Group}", 
            GetUserId(), groupName);
    }

    public async Task UnsubscribeFromGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
```

### Real-Time Data Hub

```csharp
public interface IDataClient
{
    Task DataUpdated(string entityType, string entityId, object data);
    Task DataDeleted(string entityType, string entityId);
    Task SyncState(string syncId, object state);
}

public class DataHub : AeroHub<IDataClient>
{
    private readonly IGenericRepository<Product> _productRepository;
    private static readonly ConcurrentDictionary<string, string> _userConnections = new();

    public DataHub(
        IGenericRepository<Product> productRepository,
        ILogger<DataHub> logger,
        ICurrentUserService currentUser) 
        : base(logger, currentUser)
    {
        _productRepository = productRepository;
    }

    public override async Task OnConnectedAsync()
    {
        if (CurrentUser.IsAuthenticated)
        {
            _userConnections[CurrentUser.UserId!] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinEntityGroup(string entityType, string entityId)
    {
        var groupName = $"{entityType}:{entityId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveEntityGroup(string entityType, string entityId)
    {
        var groupName = $"{entityType}:{entityId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task RequestSync(string syncId, string entityType)
    {
        switch (entityType.ToLower())
        {
            case "product":
                var products = await _productRepository.GetAllAsync();
                await Clients.Caller.SyncState(syncId, products);
                break;
            default:
                throw new ArgumentException($"Unknown entity type: {entityType}");
        }
    }

    public static string? GetConnectionIdForUser(string userId)
    {
        _userConnections.TryGetValue(userId, out var connectionId);
        return connectionId;
    }
}
```

## Setup

### Configuration

```csharp
// Program.cs
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    })
    .AddStackExchangeRedis(options =>
    {
        options.Configuration.ConnectionString = builder.Configuration
            .GetConnectionString("Redis");
    });

// Configure Redis backplane for scaling
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        options.Configuration.ConnectionString = "localhost:6379";
    });

// Add authentication for SignalR
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        // Configure JWT options
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Map hubs
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<DataHub>("/hubs/data");
```

### Hub Context Access

```csharp
public class NotificationService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public NotificationService(
        IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(string userId, Notification notification)
    {
        await _hubContext.Clients
            .Group($"user:{userId}")
            .ReceiveNotification(notification);
    }

    public async Task BroadcastToAllAsync(Notification notification)
    {
        await _hubContext.Clients.All.ReceiveNotification(notification);
    }

    public async Task BroadcastToGroupAsync(string groupName, Notification notification)
    {
        await _hubContext.Clients.Group(groupName).ReceiveNotification(notification);
    }

    public async Task UpdateUnreadCountAsync(string userId)
    {
        var count = await GetUnreadCountAsync(userId);
        await _hubContext.Clients
            .Group($"user:{userId}")
            .NotificationCountUpdated(count);
    }
}
```

## Client Integration

### JavaScript Client

```javascript
// Connect to hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications", {
        accessTokenFactory: () => getAuthToken()
    })
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

// Register handlers
connection.on("ReceiveNotification", (notification) => {
    console.log("New notification:", notification);
    showNotification(notification);
});

connection.on("NotificationCountUpdated", (count) => {
    updateNotificationBadge(count);
});

// Start connection
async function startConnection() {
    try {
        await connection.start();
        console.log("Connected to SignalR");
    } catch (err) {
        console.error("Error connecting to SignalR:", err);
        setTimeout(startConnection, 5000);
    }
}

// Call hub methods
async function markAsRead(notificationId) {
    await connection.invoke("MarkAsRead", notificationId);
}

async function subscribeToGroup(groupName) {
    await connection.invoke("SubscribeToGroup", groupName);
}

startConnection();
```

### TypeScript Client

```typescript
interface Notification {
    id: string;
    title: string;
    message: string;
    createdAt: Date;
    isRead: boolean;
}

class NotificationService {
    private connection: signalR.HubConnection;

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications", {
                accessTokenFactory: () => this.getAuthToken()
            })
            .withAutomaticReconnect()
            .build();

        this.registerHandlers();
    }

    private registerHandlers(): void {
        this.connection.on("ReceiveNotification", (notification: Notification) => {
            this.onNotificationReceived(notification);
        });

        this.connection.on("NotificationCountUpdated", (count: number) => {
            this.onCountUpdated(count);
        });
    }

    async start(): Promise<void> {
        try {
            await this.connection.start();
            console.log("SignalR Connected");
        } catch (err) {
            console.error("SignalR Connection Error:", err);
            setTimeout(() => this.start(), 5000);
        }
    }

    async markAsRead(notificationId: string): Promise<void> {
        await this.connection.invoke("MarkAsRead", notificationId);
    }

    private getAuthToken(): string {
        return localStorage.getItem("authToken") || "";
    }

    private onNotificationReceived(notification: Notification): void {
        // Handle notification
    }

    private onCountUpdated(count: number): void {
        // Update UI
    }
}
```

## Advanced Features

### Streaming

```csharp
public class StreamingHub : Hub
{
    public async IAsyncEnumerable<int> Counter(
        int count,
        int delay,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Delay(delay, cancellationToken);
        }
    }

    public async Task UploadStream(IAsyncEnumerable<string> stream)
    {
        await foreach (var item in stream)
        {
            Logger.LogInformation("Received: {Item}", item);
        }
    }
}
```

### Groups Management

```csharp
public class GroupManagementService
{
    private readonly IHubContext<AeroHub> _hubContext;

    public async Task AddToGroupAsync(string connectionId, string groupName)
    {
        await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
    }

    public async Task RemoveFromGroupAsync(string connectionId, string groupName)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
    }

    public async Task<IEnumerable<string>> GetGroupMembersAsync(string groupName)
    {
        // Requires custom implementation with Redis or database
        return await _groupStore.GetMembersAsync(groupName);
    }
}
```

### Connection Management

```csharp
public class ConnectionManager
{
    private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();

    public void AddConnection(string connectionId, string userId, string? deviceId = null)
    {
        _connections.TryAdd(connectionId, new UserConnection
        {
            ConnectionId = connectionId,
            UserId = userId,
            DeviceId = deviceId,
            ConnectedAt = DateTime.UtcNow
        });
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public IEnumerable<string> GetUserConnections(string userId)
    {
        return _connections.Values
            .Where(c => c.UserId == userId)
            .Select(c => c.ConnectionId);
    }

    public IEnumerable<string> GetAllConnections()
    {
        return _connections.Keys;
    }
}

public class UserConnection
{
    public string ConnectionId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? DeviceId { get; set; }
    public DateTime ConnectedAt { get; set; }
}
```

## Best Practices

1. **Authenticate Connections** - Always validate tokens in SignalR connections
2. **Use Groups** - Organize clients into groups for efficient broadcasting
3. **Handle Reconnections** - Implement reconnection logic on clients
4. **Scale with Redis** - Use Redis backplane for multi-server deployments
5. **Limit Message Size** - Keep messages small to avoid performance issues
6. **Log Connections** - Track connection/disconnection events
7. **Dispose Properly** - Ensure proper cleanup on disconnect

## Related Packages

- `Aero.Web` - HTTP integration
- `Aero.Auth` - Authentication for SignalR
- `Aero.Caching` - Redis backplane support
