using System;
using System.Collections.Generic;
using System.Linq;

namespace EventSourcing.Library.Domain
{
    /// <summary>
    /// Abstract base class for aggregate roots using event sourcing.
    /// Implements Template Method pattern for event handling.
    /// Derived classes implement the Strategy pattern via ApplyEvent method.
    /// </summary>
    public abstract class AggregateRootBase : IAggregateRoot
    {
        private readonly List<IDomainEvent> _uncommittedEvents = new();
        
        protected AggregateRootBase()
        {
            Id = Guid.NewGuid().ToString();
        }

        protected AggregateRootBase(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(id));
            
            Id = id;
        }

        public string Id { get; protected set; }
        public int Version { get; protected set; }

        public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents()
        {
            return _uncommittedEvents.AsReadOnly();
        }

        public void MarkEventsAsCommitted()
        {
            _uncommittedEvents.Clear();
        }

        public void LoadFromHistory(IEnumerable<IDomainEvent> history)
        {
            foreach (var @event in history.OrderBy(e => e.Version))
            {
                ApplyEvent(@event, isNew: false);
            }
        }

        /// <summary>
        /// Template method for raising new domain events.
        /// Automatically increments version and tracks uncommitted events.
        /// </summary>
        protected void RaiseEvent(IDomainEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            // Set aggregate metadata on the event
            @event.AggregateId = Id;
            @event.Version = Version + 1;
            
            // Validate the event
            if (@event is DomainEventBase baseEvent)
                baseEvent.Validate();

            // Apply the event to update state
            ApplyEvent(@event, isNew: true);
            
            // Track for persistence
            _uncommittedEvents.Add(@event);
        }

        /// <summary>
        /// Applies an event to update aggregate state.
        /// When isNew is true, increments version.
        /// When false (loading from history), version is already set on the event.
        /// </summary>
        private void ApplyEvent(IDomainEvent @event, bool isNew)
        {
            // Strategy pattern: delegate to derived class for specific event handling
            ApplyEventCore(@event);
            
            // Update version - either from event (history) or increment (new)
            Version = @event.Version;
        }

        /// <summary>
        /// Abstract method that derived classes must implement to handle specific event types.
        /// This is the Strategy pattern hook for domain-specific behavior.
        /// </summary>
        protected abstract void ApplyEventCore(IDomainEvent @event);

        /// <summary>
        /// Helper method for pattern matching in ApplyEventCore implementations
        /// </summary>
        protected void When<TEvent>(IDomainEvent @event, Action<TEvent> handler) 
            where TEvent : IDomainEvent
        {
            if (@event is TEvent typedEvent)
                handler(typedEvent);
        }
    }
}
