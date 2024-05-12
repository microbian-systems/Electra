using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Electra.Models.Entities
{
    public interface IEntity<TKey> where TKey : IEquatable<TKey>
    {
        [Key]
        [JsonPropertyName("id")]
        TKey Id { get; set; }
        
        [JsonPropertyName("created_on")]
        public DateTimeOffset CreatedOn { get; set; }
        
        [JsonPropertyName("modified_on")]
        public DateTimeOffset? ModifiedOn { get; set; }
        
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; }
        
        [JsonPropertyName("modified_by")]
        public string ModifiedBy { get; set; }
    }

    /// <summary>
    /// Represents a persisted entity for Electra
    /// </summary>
    public abstract record Entity : Entity<Guid>
    {
        //public override Guid Id { get; set; } = Guid.Empty; // avoid virtual call in constructor
    }

    /// <summary>
    /// Represents a persisted entity for Electra
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public abstract record Entity<TKey> : EntityBase<TKey> where TKey : IEquatable<TKey>
    {

    }
    
    /// <summary>
    /// Represents an enetity that can be persisted
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public abstract record EntityBase<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
    {
        [Key]
        [JsonPropertyName("id")]
        public TKey Id { get; set; }
        
        [JsonPropertyName("created_on")]
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        
        [JsonPropertyName("modified_on")]
        public DateTimeOffset? ModifiedOn { get; set; }
        
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; }
        
        [JsonPropertyName("updated_by")]
        public string ModifiedBy { get; set; }
    }
}