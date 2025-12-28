using SnowflakeGenerator;
using Wolverine;

namespace Electra.Events;

public interface IEvent
{
    
}

public interface IEventMessage : IEvent, IMessage
{
    long Id { get; init; } 
    public DateTimeOffset CreatedAt { get; init; } 
}

public abstract record EventMessageBase : IEventMessage
{
    // var (timeStamp, machineId, sequence) = sonyflake.DecodeID(uniqueId);
    private static readonly Snowflake snowflake = new();
    public long Id { get; init; } = snowflake.NextID();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public abstract record EventMessage : EventMessageBase { }

public abstract record EventMessage<T> : EventMessageBase
{
    public required T Payload;
}

