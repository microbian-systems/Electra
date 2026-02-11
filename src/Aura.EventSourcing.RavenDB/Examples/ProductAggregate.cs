using System;
using EventSourcing.RavenDB.Domain;

namespace EventSourcing.RavenDB.Examples
{
    // ============================================================================
    // DOMAIN EVENTS
    // ============================================================================

    public class ProductCreatedEvent : DomainEventBase
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
    }

    public class ProductPriceUpdatedEvent : DomainEventBase
    {
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ProductRenamedEvent : DomainEventBase
    {
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
    }

    public class ProductDiscontinuedEvent : DomainEventBase
    {
        public string Reason { get; set; } = string.Empty;
        public DateTime DiscontinuedDate { get; set; }
    }

    public class ProductRestockedEvent : DomainEventBase
    {
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;
    }

    // ============================================================================
    // PRODUCT AGGREGATE
    // ============================================================================

    /// <summary>
    /// Product aggregate root demonstrating event sourcing patterns.
    /// Follows DDD aggregate pattern with strong encapsulation.
    /// </summary>
    public class Product : AggregateRootBase
    {
        // Private fields - encapsulated state
        private string _name = string.Empty;
        private decimal _price;
        private string _category = string.Empty;
        private string _sku = string.Empty;
        private bool _isDiscontinued;
        private DateTime? _discontinuedDate;
        private int _stockQuantity;

        // Public properties for read-only access
        public string Name => _name;
        public decimal Price => _price;
        public string Category => _category;
        public string Sku => _sku;
        public bool IsDiscontinued => _isDiscontinued;
        public DateTime? DiscontinuedDate => _discontinuedDate;
        public int StockQuantity => _stockQuantity;

        // Parameterless constructor for EF Core and hydration from history
        private Product() : base()
        {
        }

        // Factory method for creating new products
        public static Product Create(string name, decimal price, string category, string sku)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty", nameof(category));

            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU cannot be empty", nameof(sku));

            var product = new Product();
            var @event = new ProductCreatedEvent
            {
                Name = name,
                Price = price,
                Category = category,
                Sku = sku
            };

            product.RaiseEvent(@event);
            return product;
        }

        // Business methods that enforce invariants and raise events
        public void UpdatePrice(decimal newPrice, string reason)
        {
            if (_isDiscontinued)
                throw new InvalidOperationException("Cannot update price of discontinued product");

            if (newPrice < 0)
                throw new ArgumentException("Price cannot be negative", nameof(newPrice));

            if (newPrice == _price)
                return; // No change needed

            var @event = new ProductPriceUpdatedEvent
            {
                OldPrice = _price,
                NewPrice = newPrice,
                Reason = reason
            };

            RaiseEvent(@event);
        }

        public void Rename(string newName)
        {
            if (_isDiscontinued)
                throw new InvalidOperationException("Cannot rename discontinued product");

            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Name cannot be empty", nameof(newName));

            if (newName == _name)
                return; // No change needed

            var @event = new ProductRenamedEvent
            {
                OldName = _name,
                NewName = newName
            };

            RaiseEvent(@event);
        }

        public void Discontinue(string reason)
        {
            if (_isDiscontinued)
                return; // Already discontinued

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty", nameof(reason));

            var @event = new ProductDiscontinuedEvent
            {
                Reason = reason,
                DiscontinuedDate = DateTime.UtcNow
            };

            RaiseEvent(@event);
        }

        public void Restock(int quantity, string location)
        {
            if (_isDiscontinued)
                throw new InvalidOperationException("Cannot restock discontinued product");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Location cannot be empty", nameof(location));

            var @event = new ProductRestockedEvent
            {
                Quantity = quantity,
                Location = location
            };

            RaiseEvent(@event);
        }

        // Event application - updates internal state based on events
        protected override void ApplyEventCore(IDomainEvent @event)
        {
            // Using pattern matching for type-safe event handling
            switch (@event)
            {
                case ProductCreatedEvent created:
                    Apply(created);
                    break;
                case ProductPriceUpdatedEvent priceUpdated:
                    Apply(priceUpdated);
                    break;
                case ProductRenamedEvent renamed:
                    Apply(renamed);
                    break;
                case ProductDiscontinuedEvent discontinued:
                    Apply(discontinued);
                    break;
                case ProductRestockedEvent restocked:
                    Apply(restocked);
                    break;
            }
        }

        private void Apply(ProductCreatedEvent @event)
        {
            _name = @event.Name;
            _price = @event.Price;
            _category = @event.Category;
            _sku = @event.Sku;
            _stockQuantity = 0;
            _isDiscontinued = false;
        }

        private void Apply(ProductPriceUpdatedEvent @event)
        {
            _price = @event.NewPrice;
        }

        private void Apply(ProductRenamedEvent @event)
        {
            _name = @event.NewName;
        }

        private void Apply(ProductDiscontinuedEvent @event)
        {
            _isDiscontinued = true;
            _discontinuedDate = @event.DiscontinuedDate;
        }

        private void Apply(ProductRestockedEvent @event)
        {
            _stockQuantity += @event.Quantity;
        }
    }
}
