using System;
using Orleans;

namespace Electra.Actors;

[Serializable]
[GenerateSerializer]
public record Message(Guid? Id, string content);

[Serializable]
[GenerateSerializer]
public record Message<T>(string? Id, string content, T payload);