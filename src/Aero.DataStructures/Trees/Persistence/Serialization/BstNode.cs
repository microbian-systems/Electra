using System;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

/// <summary>
/// Represents a node in a Binary Search Tree or Red-Black Tree for persistence.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public readonly record struct BstNode<T>(
    long Id,
    T Value,
    long LeftId,
    long RightId,
    long ParentId,
    bool IsRed
) where T : unmanaged;
