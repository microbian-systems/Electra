using System;
using Orleans;

namespace Electra.Actors;

[Serializable]
[GenerateSerializer]
public record Message(Guid id, string content);

[Serializable]
[GenerateSerializer]
public record Message<T>(Guid id, string content, T payload);