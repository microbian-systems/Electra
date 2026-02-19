using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a Knowledge Graph - a network of real-world entities and their relationships,
/// structured to enable semantic reasoning and knowledge inference.
/// </summary>
/// <typeparam name="TEntityId">The type of entity identifiers. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// A knowledge graph represents knowledge as a graph of entities (nodes) connected by 
/// relationships (edges), with both having types and attributes. It goes beyond simple
/// property graphs by adding semantic meaning and reasoning capabilities.
/// </para>
/// <para>
/// Key components:
/// <list type="bullet">
/// <item><description>Entities: Real-world objects, concepts, or instances</description></item>
/// <item><description>Relations: Typed connections between entities</description></item>
/// <item><description>Classes/Types: Categories of entities (Ontology)</description></item>
/// <item><description>Properties: Attributes of entities</description></item>
/// <item><description>Facts: Entity-Relation-Entity triples (SPO triples)</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Google Knowledge Graph, Wikidata, DBpedia</description></item>
/// <item><description>Semantic search and question answering</description></item>
/// <item><description>Natural language understanding</description></item>
/// <item><description>Recommendation systems with explainability</description></item>
/// <item><description>Drug discovery and biomedical research</description></item>
/// <item><description>Enterprise knowledge management</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var kg = new KnowledgeGraph&lt;string&gt;();
/// 
/// // Define ontology
/// kg.DefineClass("Person", "A human being");
/// kg.DefineClass("City", "An urban area");
/// kg.DefineClass("Country", "A nation state");
/// 
/// kg.DefineRelation("bornIn", "Person", "City", "Person's birthplace");
/// kg.DefineRelation("capitalOf", "City", "Country", "City is the capital of country");
/// kg.DefineRelation("citizenOf", "Person", "Country", "Person is citizen of country");
/// 
/// // Add entities
/// kg.AddEntity("einstein", "Person", new { name = "Albert Einstein", birthYear = 1879 });
/// kg.AddEntity("ulm", "City", new { name = "Ulm", population = 126000 });
/// kg.AddEntity("germany", "Country", new { name = "Germany", isoCode = "DE" });
/// 
/// // Add facts (triples)
/// kg.AddFact("einstein", "bornIn", "ulm");
/// kg.AddFact("ulm", "locatedIn", "germany");
/// 
/// // Query: Who was born in Germany?
/// var germanBorn = kg.Query()
///     .Match("(p:Person)-[:bornIn]->(:City)-[:locatedIn]->(:Country {name: 'Germany'})")
///     .Return("p");
/// </code>
/// </example>
public class KnowledgeGraph<TEntityId> where TEntityId : notnull
{
    private readonly Dictionary<TEntityId, Entity> _entities = new();
    private readonly Dictionary<string, EntityClass> _classes = new();
    private readonly Dictionary<string, Relation> _relations = new();
    private readonly List<Triple> _triples = new();
    private readonly Dictionary<TEntityId, List<int>> _entityOutgoingTriples = new();
    private readonly Dictionary<TEntityId, List<int>> _entityIncomingTriples = new();

    /// <summary>
    /// Represents an entity (node) in the knowledge graph.
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the entity.
        /// </summary>
        public TEntityId Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the class/type of the entity.
        /// </summary>
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the properties of the entity.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Gets the confidence score of this entity (0-1).
        /// </summary>
        public double Confidence { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the source/provenance of this entity.
        /// </summary>
        public string? Source { get; set; }
    }

    /// <summary>
    /// Represents a class (type) in the ontology.
    /// </summary>
    public class EntityClass
    {
        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the class.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parent classes (for inheritance).
        /// </summary>
        public List<string> ParentClasses { get; set; } = new();

        /// <summary>
        /// Gets or sets the property definitions for this class.
        /// </summary>
        public Dictionary<string, Type> PropertyDefinitions { get; set; } = new();
    }

    /// <summary>
    /// Represents a relation (edge type) in the ontology.
    /// </summary>
    public class Relation
    {
        /// <summary>
        /// Gets or sets the name of the relation.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the domain (source entity types).
        /// </summary>
        public List<string> Domain { get; set; } = new();

        /// <summary>
        /// Gets or sets the range (target entity types).
        /// </summary>
        public List<string> Range { get; set; } = new();

        /// <summary>
        /// Gets or sets the description of the relation.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inverse relation name.
        /// </summary>
        public string? InverseRelation { get; set; }

        /// <summary>
        /// Gets or sets whether this relation is transitive.
        /// </summary>
        public bool IsTransitive { get; set; }

        /// <summary>
        /// Gets or sets whether this relation is symmetric.
        /// </summary>
        public bool IsSymmetric { get; set; }
    }

    /// <summary>
    /// Represents a fact (Subject-Predicate-Object triple).
    /// </summary>
    public class Triple
    {
        /// <summary>
        /// Gets or sets the subject entity ID.
        /// </summary>
        public TEntityId Subject { get; set; } = default!;

        /// <summary>
        /// Gets or sets the predicate (relation name).
        /// </summary>
        public string Predicate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object entity ID.
        /// </summary>
        public TEntityId Object { get; set; } = default!;

        /// <summary>
        /// Gets or sets the confidence score of this fact.
        /// </summary>
        public double Confidence { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the source/provenance of this fact.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this fact was recorded.
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Gets the number of entities in the knowledge graph.
    /// </summary>
    public int EntityCount => _entities.Count;

    /// <summary>
    /// Gets the number of facts (triples) in the knowledge graph.
    /// </summary>
    public int TripleCount => _triples.Count;

    /// <summary>
    /// Gets the number of classes in the ontology.
    /// </summary>
    public int ClassCount => _classes.Count;

    /// <summary>
    /// Gets the number of relations in the ontology.
    /// </summary>
    public int RelationCount => _relations.Count;

    /// <summary>
    /// Defines a new entity class in the ontology.
    /// </summary>
    public EntityClass DefineClass(string name, string description, params string[] parentClasses)
    {
        var entityClass = new EntityClass
        {
            Name = name,
            Description = description,
            ParentClasses = parentClasses.ToList()
        };
        _classes[name] = entityClass;
        return entityClass;
    }

    /// <summary>
    /// Defines a new relation in the ontology.
    /// </summary>
    public Relation DefineRelation(string name, string domainClass, string rangeClass, 
        string description = "", string? inverseRelation = null, 
        bool isTransitive = false, bool isSymmetric = false)
    {
        var relation = new Relation
        {
            Name = name,
            Domain = new List<string> { domainClass },
            Range = new List<string> { rangeClass },
            Description = description,
            InverseRelation = inverseRelation,
            IsTransitive = isTransitive,
            IsSymmetric = isSymmetric
        };
        _relations[name] = relation;
        return relation;
    }

    /// <summary>
    /// Adds an entity to the knowledge graph.
    /// </summary>
    public Entity AddEntity(TEntityId id, string entityClass, object? properties = null, 
        string? label = null, double confidence = 1.0, string? source = null)
    {
        var entity = new Entity
        {
            Id = id,
            Class = entityClass,
            Label = label ?? id?.ToString() ?? string.Empty,
            Confidence = confidence,
            Source = source
        };

        if (properties != null)
        {
            var propDict = properties.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(properties)!);
            entity.Properties = propDict;
        }

        _entities[id] = entity;
        _entityOutgoingTriples[id] = new List<int>();
        _entityIncomingTriples[id] = new List<int>();

        return entity;
    }

    /// <summary>
    /// Adds a fact (triple) to the knowledge graph.
    /// </summary>
    public Triple AddFact(TEntityId subject, string predicate, TEntityId obj,
        double confidence = 1.0, string? source = null)
    {
        if (!_entities.ContainsKey(subject))
            throw new ArgumentException($"Subject entity '{subject}' not found.");

        if (!_entities.ContainsKey(obj))
            throw new ArgumentException($"Object entity '{obj}' not found.");

        var triple = new Triple
        {
            Subject = subject,
            Predicate = predicate,
            Object = obj,
            Confidence = confidence,
            Source = source,
            Timestamp = DateTime.UtcNow
        };

        var index = _triples.Count;
        _triples.Add(triple);
        _entityOutgoingTriples[subject].Add(index);
        _entityIncomingTriples[obj].Add(index);

        if (_relations.TryGetValue(predicate, out var relation) && relation.IsSymmetric)
        {
            var inverseTriple = new Triple
            {
                Subject = obj,
                Predicate = predicate,
                Object = subject,
                Confidence = confidence,
                Source = source,
                Timestamp = DateTime.UtcNow
            };
            var inverseIndex = _triples.Count;
            _triples.Add(inverseTriple);
            _entityOutgoingTriples[obj].Add(inverseIndex);
            _entityIncomingTriples[subject].Add(inverseIndex);
        }

        return triple;
    }

    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    public Entity? GetEntity(TEntityId id) => _entities.TryGetValue(id, out var entity) ? entity : null;

    /// <summary>
    /// Gets all entities of a specific class.
    /// </summary>
    public IEnumerable<Entity> GetEntitiesByClass(string entityClass) =>
        _entities.Values.Where(e => e.Class == entityClass);

    /// <summary>
    /// Gets all outgoing facts from an entity.
    /// </summary>
    public IEnumerable<Triple> GetOutgoingFacts(TEntityId entityId)
    {
        if (!_entityOutgoingTriples.TryGetValue(entityId, out var indices))
            yield break;

        foreach (var index in indices)
            yield return _triples[index];
    }

    /// <summary>
    /// Gets all incoming facts to an entity.
    /// </summary>
    public IEnumerable<Triple> GetIncomingFacts(TEntityId entityId)
    {
        if (!_entityIncomingTriples.TryGetValue(entityId, out var indices))
            yield break;

        foreach (var index in indices)
            yield return _triples[index];
    }

    /// <summary>
    /// Gets facts matching a specific predicate.
    /// </summary>
    public IEnumerable<Triple> GetFacts(string predicate) =>
        _triples.Where(t => t.Predicate == predicate);

    /// <summary>
    /// Gets facts matching subject and predicate.
    /// </summary>
    public IEnumerable<Triple> GetFacts(TEntityId subject, string predicate) =>
        GetOutgoingFacts(subject).Where(t => t.Predicate == predicate);

    /// <summary>
    /// Checks if a specific fact exists.
    /// </summary>
    public bool HasFact(TEntityId subject, string predicate, TEntityId obj) =>
        _triples.Any(t => t.Subject.Equals(subject) && t.Predicate == predicate && t.Object.Equals(obj));

    /// <summary>
    /// Finds all entities connected to a given entity via any relation.
    /// </summary>
    public IEnumerable<Entity> GetConnectedEntities(TEntityId entityId)
    {
        var connected = new HashSet<TEntityId>();

        foreach (var triple in GetOutgoingFacts(entityId))
            connected.Add(triple.Object);

        foreach (var triple in GetIncomingFacts(entityId))
            connected.Add(triple.Subject);

        foreach (var id in connected)
            yield return _entities[id];
    }

    /// <summary>
    /// Performs simple inference based on transitive relations.
    /// </summary>
    public IEnumerable<Triple> InferTransitiveFacts(string transitiveRelation)
    {
        if (!_relations.TryGetValue(transitiveRelation, out var relation) || !relation.IsTransitive)
            yield break;

        var inferred = new HashSet<(TEntityId, TEntityId)>();

        foreach (var entity in _entities.Values)
        {
            var reachable = new HashSet<TEntityId>();
            var queue = new Queue<TEntityId>();
            queue.Enqueue(entity.Id);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var triple in GetFacts(current, transitiveRelation))
                {
                    if (reachable.Add(triple.Object))
                    {
                        queue.Enqueue(triple.Object);

                        if (!entity.Id.Equals(triple.Object) && 
                            !HasFact(entity.Id, transitiveRelation, triple.Object))
                        {
                            inferred.Add((entity.Id, triple.Object));
                        }
                    }
                }
            }
        }

        foreach (var (subject, obj) in inferred)
        {
            yield return new Triple
            {
                Subject = subject,
                Predicate = transitiveRelation,
                Object = obj,
                Confidence = 0.9,
                Source = "inferred:transitive"
            };
        }
    }

    /// <summary>
    /// Finds entities matching a property value.
    /// </summary>
    public IEnumerable<Entity> FindEntities(string propertyKey, object value) =>
        _entities.Values.Where(e => e.Properties.TryGetValue(propertyKey, out var v) && Equals(v, value));

    /// <summary>
    /// Gets all classes in the ontology.
    /// </summary>
    public IEnumerable<EntityClass> GetClasses() => _classes.Values;

    /// <summary>
    /// Gets all relations in the ontology.
    /// </summary>
    public IEnumerable<Relation> GetRelations() => _relations.Values;

    /// <summary>
    /// Exports all facts as triples.
    /// </summary>
    public IEnumerable<(TEntityId Subject, string Predicate, TEntityId Object)> ExportTriples() =>
        _triples.Select(t => (t.Subject, t.Predicate, t.Object));

    /// <summary>
    /// Clears all entities, facts, and ontology from the knowledge graph.
    /// </summary>
    public void Clear()
    {
        _entities.Clear();
        _classes.Clear();
        _relations.Clear();
        _triples.Clear();
        _entityOutgoingTriples.Clear();
        _entityIncomingTriples.Clear();
    }
}
