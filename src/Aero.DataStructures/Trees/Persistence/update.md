# Persistent Tree Data Structures — Spec-Driven Development (v2)

## Changelog from v1

| Area | Change |
| ---| ---|
| `IStorageBackend` | Added `GetPageMetadataAsync`, `UpdatePageMetadataAsync`, `GetFragmentedPagesAsync` |
| `IStorageBackend` header | Extended header page layout to persist per-page metadata |
| New interface | `IZeroCopyStorageBackend` — optional capability for mmap backends |
| New backend | `MmapStorageBackend` — implements `IZeroCopyStorageBackend` |
| New abstraction | `NodeReader<TKey, TValue>` — internal strategy selected at construction time |
| Node structs | `RecordFlags`, `BPlusLeafRecord` — tombstone support via `Flags` byte |
| `BPlusLeafNode` | Added `LiveCount`, `DeadCount`, `TotalSlots` fields |
| `ITree<T>` | Added `DeleteAsync` |
| New interface | `IVacuumable` — optional capability for fragmenting structures |
| New type | `VacuumProgress` record |
| `PersistentBPlusTree` | Implements `IVacuumable`, tombstone delete, compaction, leaf free |
| `PersistentHeapBase` | Implements `DeleteAsync` via O(n) scan + sift — no vacuum needed |
| New service | `AutoVacuumService` — `BackgroundService` with configurable threshold |
| Project structure | New files for all of the above |
| Acceptance criteria | Extended to cover delete, vacuum, and zero-copy path |
| Implementation order | Extended to cover new layers |

---

## Agent Instructions

You are implementing a C# library targeting .NET 8+. Follow this specification precisely.
Apply SOLID principles throughout. Do not deviate from the interface contracts defined here.
Implement one layer at a time in the order specified. After each layer, run the associated
tests before proceeding. Do not proceed to the next step until all tests for the current
step pass.

---

## Project Structure

```
Aero.DataStructures/
├── Trees/Persistence/
│   ├── Storage/
│   │   ├── IStorageBackend.cs
│   │   ├── IZeroCopyStorageBackend.cs          ← NEW
│   │   ├── PageMetadata.cs                     ← NEW
│   │   ├── MemoryStorageBackend.cs
│   │   ├── FileStorageBackend.cs
│   │   └── MmapStorageBackend.cs               ← NEW
│   ├── Serialization/
│   │   ├── INodeSerializer.cs
│   │   ├── IntSerializer.cs
│   │   ├── LongSerializer.cs
│   │   ├── BstNodeSerializer.cs
│   │   └── BPlusNodeSerializer.cs              ← NEW (replaces inline in BPlusLeafNode)
│   ├── Nodes/                                  ← NEW folder
│   │   ├── RecordFlags.cs                      ← NEW
│   │   ├── BPlusLeafRecord.cs                  ← NEW
│   │   ├── BPlusLeafNode.cs                    ← UPDATED
│   │   ├── BPlusInternalNode.cs                ← NEW (extracted from tree)
│   │   └── BstNode.cs
│   ├── Interfaces/
│   │   ├── ITree.cs                            ← UPDATED (DeleteAsync added)
│   │   ├── IOrderedTree.cs
│   │   ├── IPriorityTree.cs
│   │   ├── IDoubleEndedPriorityTree.cs
│   │   └── IVacuumable.cs                      ← NEW
│   ├── Trees/
│   │   ├── Internal/
│   │   │   ├── NodeReader.cs                   ← NEW
│   │   │   ├── ZeroCopyNodeReader.cs            ← NEW
│   │   │   └── CopyingNodeReader.cs             ← NEW
│   │   ├── PersistentHeapBase.cs               ← UPDATED (DeleteAsync)
│   │   ├── PersistentMinHeap.cs
│   │   ├── PersistentMaxHeap.cs
│   │   ├── PersistentMinMaxHeap.cs
│   │   └── PersistentBPlusTree.cs              ← UPDATED (IVacuumable, tombstone)
│   ├── Vacuum/                                 ← NEW folder
│   │   ├── AutoVacuumOptions.cs                ← NEW
│   │   └── AutoVacuumService.cs                ← NEW
│   └── DI/
│       ├── StorageBackendFactory.cs
│       ├── TreeFactory.cs                      ← UPDATED
│       └── ServiceCollectionExtensions.cs      ← UPDATED
└── TreePersistence.Tests/
    ├── Storage/
    │   ├── MemoryStorageBackendTests.cs
    │   ├── FileStorageBackendTests.cs
    │   └── MmapStorageBackendTests.cs          ← NEW
    ├── Serialization/
    │   └── SerializerTests.cs
    └── Trees/
        ├── MinHeapTests.cs                     ← UPDATED (delete tests)
        ├── MaxHeapTests.cs                     ← UPDATED (delete tests)
        ├── MinMaxHeapTests.cs                  ← UPDATED (delete tests)
        ├── BPlusTreeTests.cs                   ← UPDATED (delete, vacuum tests)
        └── VacuumTests.cs                      ← NEW
```

---

## Layer 1 — Storage Backend

### Specification

The storage backend abstracts the physical medium. It has no knowledge of tree structure,
node types, or serialization. All data is treated as raw pages of bytes. The backend does
track lightweight per-page slot metadata (live/dead counts) to support vacuum scheduling
without requiring full page reads.

### PageMetadata

```csharp
namespace TreePersistence.Core.Storage;

/// <summary>
/// Lightweight per-page occupancy metadata tracked by the backend.
/// Does not require reading the full page contents.
/// </summary>
public readonly record struct PageMetadata(
    long PageId,
    int TotalSlots,
    int LiveSlots,
    int DeadSlots,
    bool IsFree
)
{
    /// <summary>Ratio of dead slots to total slots. 0.0 = no fragmentation.</summary>
    public double Fragmentation =>
        TotalSlots == 0 ? 0.0 : (double)DeadSlots / TotalSlots;
}
```

### IStorageBackend Interface Contract

```csharp
namespace TreePersistence.Core.Storage;

public interface IStorageBackend : IAsyncDisposable
{
    /// <summary>
    /// Reads a page by its stable integer ID.
    /// Throws PageNotFoundException if pageId does not exist.
    /// </summary>
    ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default);

    /// <summary>
    /// Writes data to an existing or newly allocated page.
    /// Data.Length must equal the configured page size.
    /// Throws PageSizeMismatchException if data length is wrong.
    /// </summary>
    ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>
    /// Allocates a new page and returns its stable ID.
    /// Reuses freed pages before growing the underlying medium.
    /// Does not write any data — caller must follow with WritePageAsync.
    /// </summary>
    ValueTask<long> AllocatePageAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a page to the free list. Implementations must reuse freed page IDs.
    /// Throws PageNotFoundException if pageId does not exist.
    /// </summary>
    ValueTask FreePageAsync(long pageId, CancellationToken ct = default);

    /// <summary>
    /// Flushes buffered writes to the underlying medium.
    /// No-op for MemoryStorageBackend.
    /// </summary>
    ValueTask FlushAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns cached metadata for a page without reading its full contents.
    /// Throws PageNotFoundException if pageId does not exist.
    /// </summary>
    ValueTask<PageMetadata> GetPageMetadataAsync(long pageId, CancellationToken ct = default);

    /// <summary>
    /// Adjusts the live and dead slot counts for a page after a tree operation.
    /// Called by tree implementations after tombstoning or compacting records.
    /// liveDelta and deadDelta may be negative.
    /// </summary>
    ValueTask UpdatePageMetadataAsync(
        long pageId,
        int liveDelta,
        int deadDelta,
        CancellationToken ct = default);

    /// <summary>
    /// Lazily yields pages whose fragmentation ratio meets or exceeds the threshold.
    /// Ordered by fragmentation descending (most fragmented first).
    /// fragmentationThreshold = 0.0 yields all non-free pages.
    /// </summary>
    IAsyncEnumerable<PageMetadata> GetFragmentedPagesAsync(
        double fragmentationThreshold,
        CancellationToken ct = default);

    /// <summary>The fixed page size in bytes for this backend instance.</summary>
    int PageSize { get; }

    /// <summary>Total number of currently allocated (non-freed) pages.</summary>
    long PageCount { get; }
}
```

### Exceptions

```csharp
public sealed class PageNotFoundException(long pageId)
    : Exception($"Page {pageId} does not exist.");

public sealed class PageSizeMismatchException(int expected, int actual)
    : Exception($"Expected {expected} bytes, got {actual}.");
```

### IZeroCopyStorageBackend Interface Contract

```csharp
namespace TreePersistence.Core.Storage;

/// <summary>
/// Optional capability interface for backends that can provide direct span
/// access into mapped memory. Only MmapStorageBackend implements this.
/// Trees detect this at construction time and route to the fast path.
/// Spans returned from this interface are valid only for the lifetime of
/// the backend and must NOT be held across await boundaries.
/// </summary>
public interface IZeroCopyStorageBackend : IStorageBackend
{
    /// <summary>
    /// Returns a Span directly into mapped memory for the given page.
    /// Zero allocation. Zero copy. Synchronous.
    /// Caller must not hold this span across any await.
    /// </summary>
    Span<byte> GetPageSpan(long pageId);

    /// <summary>
    /// Returns a ref to a typed struct directly within mapped memory.
    /// Writes through this ref go directly to mapped pages.
    /// Caller must not hold this ref across any await.
    /// </summary>
    ref T GetPageRef<T>(long pageId) where T : unmanaged;
}
```

### Implementation: MemoryStorageBackend

- Backed by `Dictionary<long, byte[]>` for pages, `Dictionary<long, PageMetadata>` for metadata
- Default page size: 4096 bytes
- Constructor: `MemoryStorageBackend(int pageSize = 4096)`
- Freed pages tracked in a `Stack<long>`, reused on next `AllocatePageAsync`
- `UpdatePageMetadataAsync` updates the metadata dictionary directly
- `GetFragmentedPagesAsync` iterates metadata dictionary, filters by threshold, orders by fragmentation descending
- All methods synchronous internally, return `ValueTask` for interface compliance
- Thread safety is NOT required

### Implementation: FileStorageBackend

- Constructor: `FileStorageBackend(string path, int pageSize = 4096)`
- Use `FileStream` with `FileOptions.Asynchronous | FileOptions.WriteThrough`
- Page offset = `pageId * pageSize`
- Header page layout (page 0) — extended from v1:
  - Bytes 0–3: magic number `0x54524545` ("TREE" in ASCII)
  - Bytes 4–7: `int32` page size
  - Bytes 8–15: `int64` total page count
  - Bytes 16–23: `int64` free page count
  - Bytes 24–31: `int64` metadata entry count
  - Bytes 32–(32 + freeCount * 8): `int64[]` free page IDs
  - Bytes after free list: repeated `PageMetadataEntry` records:
    ```
    [int64 pageId][int32 totalSlots][int32 liveSlots][int32 deadSlots]
    ```
- On open: validate magic number and page size — throw `InvalidDataException` on mismatch
- `FlushAsync` must persist the header page then call `stream.FlushAsync()`

### Implementation: MmapStorageBackend

- Constructor: `MmapStorageBackend(string path, long capacityBytes, int pageSize = 4096)`
- Implements `IZeroCopyStorageBackend`
- Use `MemoryMappedFile.CreateFromFile` with `MemoryMappedFileAccess.ReadWrite`
- Acquire raw pointer via `SafeMemoryMappedViewHandle.AcquirePointer` — this is the only
  `unsafe` code in the entire codebase and must not leave this class
- `GetPageSpan` constructs `Span<byte>` from the raw pointer — unsafe, private seam
- `GetPageRef<T>` uses `Unsafe.AsRef<T>` on the pointer offset — unsafe, private seam
- All public interface methods are safe code operating on `Span<byte>` and `MemoryMarshal`
- `ReadPageAsync` copies span into a managed buffer (safe return for `IStorageBackend` callers)
- `WritePageAsync` copies `ReadOnlyMemory<byte>` into the mapped span via `CopyTo`
- `FlushAsync` calls `_accessor.Flush()` to force dirty pages to disk
- Metadata tracking identical to `MemoryStorageBackend` (in-memory dictionary)
- Capacity is fixed at construction — do not attempt to grow the mapped region
- `DisposeAsync` must call `SafeMemoryMappedViewHandle.ReleasePointer()` before disposing

### Tests: MemoryStorageBackend

```
GIVEN a new MemoryStorageBackend
WHEN AllocatePageAsync is called
THEN it returns pageId 0

GIVEN a backend with page allocated at id 0
WHEN WritePageAsync is called with data of correct size
THEN ReadPageAsync returns identical data

GIVEN a backend with page allocated at id 0
WHEN WritePageAsync is called with data of wrong size
THEN throws PageSizeMismatchException

GIVEN a backend with no pages
WHEN ReadPageAsync(99) is called
THEN throws PageNotFoundException

GIVEN pages 0 and 1 are allocated, page 0 is freed
WHEN AllocatePageAsync is called again
THEN returns pageId 0 (reuse freed page)

GIVEN a page with UpdatePageMetadataAsync(pageId, liveDelta: -1, deadDelta: +1)
WHEN GetPageMetadataAsync is called
THEN LiveSlots decremented, DeadSlots incremented

GIVEN pages with varying fragmentation
WHEN GetFragmentedPagesAsync(threshold: 0.5) is called
THEN only pages at or above threshold are yielded, ordered descending
```

### Tests: FileStorageBackend

```
GIVEN a new FileStorageBackend pointed at a temp file
WHEN constructed
THEN file is created and header page is written with correct magic number

GIVEN an existing file created by FileStorageBackend
WHEN a new FileStorageBackend instance is opened on same file
THEN previously written pages are readable, freed page list is restored,
     and page metadata is restored

GIVEN a FileStorageBackend with a written page
WHEN FlushAsync is called
THEN no exception is thrown and data persists after reopening the file

GIVEN a corrupted file with wrong magic number
WHEN FileStorageBackend is constructed
THEN throws InvalidDataException

GIVEN metadata updated via UpdatePageMetadataAsync
WHEN the backend is disposed and reopened
THEN metadata values are identical to those before dispose
```

### Tests: MmapStorageBackend

```
GIVEN a new MmapStorageBackend
WHEN WritePageAsync then ReadPageAsync
THEN data round-trips correctly

GIVEN a MmapStorageBackend
WHEN GetPageSpan is called
THEN returns a Span<byte> of length PageSize pointing into mapped region

GIVEN a struct written via GetPageRef<T>
WHEN ReadPageAsync is called on the same page
THEN the bytes reflect the struct that was written

GIVEN MmapStorageBackend cast to IStorageBackend
WHEN used by PersistentBPlusTree
THEN tree operates correctly (zero-copy path exercised internally)

GIVEN a MmapStorageBackend
WHEN FlushAsync is called
THEN no exception is thrown and data survives reopening the file
```

---

## Layer 2 — Node Serialization

### Specification

Serializers convert typed values to and from fixed-size byte spans. Fixed size is mandatory.
Each serializer is stateless. Node structs use `[InlineArray]` (.NET 8) instead of `unsafe fixed`
buffers — no `unsafe` code in node definitions.

### INodeSerializer Interface Contract

```csharp
namespace TreePersistence.Core.Serialization;

public interface INodeSerializer<TNode>
{
    /// <summary>Fixed byte size of one serialized node. Must be a compile-time constant.</summary>
    int SerializedSize { get; }

    TNode Deserialize(ReadOnlySpan<byte> data);

    /// <summary>destination.Length must equal SerializedSize. Throws ArgumentException otherwise.</summary>
    void Serialize(TNode node, Span<byte> destination);
}
```

### Primitive Serializers

**IntSerializer** — `int`, little-endian via `BitConverter`. SerializedSize = 4.

**LongSerializer** — `long`, little-endian via `BitConverter`. SerializedSize = 8.

### RecordFlags and BPlusLeafRecord

```csharp
namespace TreePersistence.Core.Nodes;

[Flags]
public enum RecordFlags : byte
{
    None     = 0x00,
    Deleted  = 0x01,   // tombstone — record is logically deleted
    Overflow = 0x02,   // value stored in overflow page (reserved, not implemented in v1)
}

/// <summary>
/// A single record slot within a B+ tree leaf page.
/// Blittable — safe to use with MemoryMarshal.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusLeafRecord<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    public RecordFlags Flags;
    public TKey Key;
    public TValue Value;

    public bool IsLive    => (Flags & RecordFlags.Deleted) == 0;
    public bool IsDeleted => (Flags & RecordFlags.Deleted) != 0;

    public void MarkDeleted()
    {
        Flags |= RecordFlags.Deleted;
        Value  = default; // clear value on tombstone — no data leakage
    }

    public static BPlusLeafRecord<TKey, TValue> Tombstone(TKey key) => new()
    {
        Flags = RecordFlags.Deleted,
        Key   = key,
        Value = default
    };
}
```

### Node Structs

Use `[InlineArray]` for all fixed-size arrays — no `unsafe` required.

```csharp
// Inline array wrappers — one per required capacity
[InlineArray(128)] public struct PageIdBuffer   { private long _e; }
[InlineArray(256)] public struct KeyByteBuffer  { private byte _e; }
[InlineArray(512)] public struct RecordByteBuffer { private byte _e; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusInternalNode<TKey> where TKey : unmanaged
{
    public byte  NodeType;     // always 0x01
    public int   KeyCount;
    public PageIdBuffer  ChildPageIds;  // max 128 children
    public KeyByteBuffer RawKeys;       // reinterpreted as TKey[] via MemoryMarshal

    public Span<TKey> GetKeys() =>
        MemoryMarshal.Cast<byte, TKey>(
            MemoryMarshal.CreateSpan(ref RawKeys._element, 256));

    public Span<long> GetChildPageIds() =>
        MemoryMarshal.CreateSpan(ref ChildPageIds._element, 128);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusLeafNode<TKey, TValue>
    where TKey   : unmanaged
    where TValue : unmanaged
{
    public byte NodeType;          // always 0x02
    public int  TotalSlots;        // physical capacity of record buffer
    public int  LiveCount;         // non-tombstoned records
    public int  DeadCount;         // tombstoned records
    public long PrevLeafPageId;    // -1 if first leaf
    public long NextLeafPageId;    // -1 if last leaf
    public RecordByteBuffer RawRecords;

    public double Fragmentation =>
        TotalSlots == 0 ? 0.0 : (double)DeadCount / TotalSlots;

    public bool NeedsCompaction(double threshold = 0.5) =>
        Fragmentation >= threshold;

    public Span<BPlusLeafRecord<TKey, TValue>> GetRecords() =>
        MemoryMarshal.Cast<byte, BPlusLeafRecord<TKey, TValue>>(
            MemoryMarshal.CreateSpan(ref RawRecords._element, 512));
}
```

**BstNode** — unchanged from v1.

### Tests

```
GIVEN IntSerializer
WHEN Serialize(42) then Deserialize
THEN returns 42

GIVEN IntSerializer
WHEN Serialize called with span shorter than SerializedSize
THEN throws ArgumentException

GIVEN BstNodeSerializer<int>
WHEN a BstNode is round-tripped through Serialize/Deserialize
THEN all fields are identical including LeftId = -1 sentinel

GIVEN BPlusLeafRecord<int, Guid>
WHEN MarkDeleted is called
THEN IsDeleted = true, IsLive = false, Value = default

GIVEN BPlusLeafNode<int, Guid>
WHEN GetRecords() is called
THEN span length equals TotalSlots capacity

GIVEN BPlusInternalNode<int>
WHEN GetKeys() and GetChildPageIds() are called
THEN spans are correctly sized and reinterpret raw byte buffers
```

---

## Layer 3 — Tree Interfaces

### Specification

Interfaces are split by capability following ISP. No interface declares a method that a
valid implementation would need to throw `NotSupportedException` for. `DeleteAsync` is on
`ITree<T>` because all tree types support deletion (though the internal strategy differs).
`IVacuumable` is separate because heaps cannot fragment and must not be forced to implement it.

### Contracts

```csharp
namespace TreePersistence.Core.Interfaces;

/// <summary>Base capability — all trees.</summary>
public interface ITree<T>
{
    ValueTask InsertAsync(T value, CancellationToken ct = default);

    /// <summary>
    /// Marks the value as deleted.
    /// Returns true if found and deleted, false if not found.
    /// For B+ trees: O(log n) tombstone. For heaps: O(n) scan + sift.
    /// </summary>
    ValueTask<bool> DeleteAsync(T value, CancellationToken ct = default);

    ValueTask<bool> ContainsAsync(T value, CancellationToken ct = default);
    ValueTask<long> CountAsync(CancellationToken ct = default);

    /// <summary>Removes all records. Frees all pages except metadata page.</summary>
    ValueTask ClearAsync(CancellationToken ct = default);
}

/// <summary>Ordered access — B+ tree, BST. NOT heaps.</summary>
public interface IOrderedTree<T> : ITree<T> where T : IComparable<T>
{
    ValueTask<T?> MinAsync(CancellationToken ct = default);
    ValueTask<T?> MaxAsync(CancellationToken ct = default);
    IAsyncEnumerable<T> InOrderAsync(CancellationToken ct = default);

    /// <summary>Range scan inclusive on both bounds. Lazy — does not buffer.</summary>
    IAsyncEnumerable<T> ScanAsync(T from, T to, CancellationToken ct = default);
}

/// <summary>Priority access — min or max heap.</summary>
public interface IPriorityTree<T> : ITree<T>
{
    /// <summary>Returns root without removing it. Throws InvalidOperationException if empty.</summary>
    ValueTask<T> PeekAsync(CancellationToken ct = default);

    /// <summary>Removes and returns root. Throws InvalidOperationException if empty.</summary>
    ValueTask<T> ExtractAsync(CancellationToken ct = default);
}

/// <summary>Double-ended priority — min-max heap only.</summary>
public interface IDoubleEndedPriorityTree<T> : IPriorityTree<T>
{
    ValueTask<T> PeekMaxAsync(CancellationToken ct = default);
    ValueTask<T> ExtractMaxAsync(CancellationToken ct = default);
}

/// <summary>
/// Optional vacuum capability. Implemented by B+ tree and BST only.
/// Heaps cannot fragment — do NOT add this to IPriorityTree.
/// </summary>
public interface IVacuumable
{
    /// <summary>Overall fragmentation ratio across all pages (0.0–1.0).</summary>
    ValueTask<double> GetFragmentationAsync(CancellationToken ct = default);

    /// <summary>
    /// Compacts the single most fragmented page that meets the default threshold.
    /// Returns true if a page was compacted, false if nothing needed doing.
    /// Safe to call frequently — low-impact incremental operation.
    /// </summary>
    ValueTask<bool> VacuumPageAsync(CancellationToken ct = default);

    /// <summary>
    /// Compacts all pages at or above fragmentationThreshold.
    /// Reports progress via IProgress if provided.
    /// Potentially expensive — run during low traffic.
    /// </summary>
    ValueTask VacuumAsync(
        double fragmentationThreshold = 0.5,
        IProgress<VacuumProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>Progress snapshot reported during VacuumAsync.</summary>
public readonly record struct VacuumProgress(
    int  TotalPages,
    int  ProcessedPages,
    int  CompactedPages,
    long BytesReclaimed
);
```

### Interface Constraints

- `IOrderedTree<T>` must NOT be implemented by any heap type
- `IPriorityTree<T>` must NOT be implemented by `PersistentBPlusTree`
- `IVacuumable` must NOT be implemented by any heap type
- `ScanAsync` must be lazy — `IAsyncEnumerable<T>`, never buffer all results
- `PeekAsync` / `PeekMaxAsync` on empty structure must throw `InvalidOperationException`
- `ExtractAsync` / `ExtractMaxAsync` on empty structure must throw `InvalidOperationException`
- `DeleteAsync` on a value not present must return `false`, never throw
- `ContainsAsync` must return `false` for tombstoned records

---

## Layer 4 — Internal NodeReader Strategy

### Specification

`NodeReader<TKey, TValue>` is an internal abstract class that encapsulates how nodes are
read and written. The correct subclass is selected once at tree construction time by
inspecting whether the backend implements `IZeroCopyStorageBackend`. Tree operation methods
call `NodeReader` without knowing which path they are on.

```csharp
namespace TreePersistence.Core.Trees.Internal;

internal abstract class NodeReader<TKey, TValue>
    where TKey   : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    public abstract ValueTask<BPlusInternalNode<TKey>> ReadInternalAsync(
        long pageId, CancellationToken ct);

    public abstract ValueTask<BPlusLeafNode<TKey, TValue>> ReadLeafAsync(
        long pageId, CancellationToken ct);

    public abstract ValueTask WriteInternalAsync(
        long pageId, in BPlusInternalNode<TKey> node, CancellationToken ct);

    public abstract ValueTask WriteLeafAsync(
        long pageId, in BPlusLeafNode<TKey, TValue> node, CancellationToken ct);

    /// <summary>
    /// Factory — selects ZeroCopyNodeReader if backend supports it,
    /// otherwise CopyingNodeReader. Called once at tree construction.
    /// </summary>
    public static NodeReader<TKey, TValue> Create(IStorageBackend storage) =>
        storage is IZeroCopyStorageBackend zc
            ? new ZeroCopyNodeReader<TKey, TValue>(zc)
            : new CopyingNodeReader<TKey, TValue>(storage);
}
```

### ZeroCopyNodeReader

- Holds `IZeroCopyStorageBackend`
- `ReadInternalAsync`: calls `GetPageRef<BPlusInternalNode<TKey>>(pageId)` — returns struct copy
  (unavoidable for `ValueTask<T>` return; the ref access itself is zero-copy)
- `WriteInternalAsync`: calls `GetPageRef<BPlusInternalNode<TKey>>(pageId) = node` — direct write
  to mapped memory, no intermediate buffer
- All methods return `ValueTask.FromResult(...)` — synchronous in the fast path, no allocation

### CopyingNodeReader

- Holds `IStorageBackend`
- `ReadInternalAsync`: calls `ReadPageAsync`, then `MemoryMarshal.AsRef<T>` to reinterpret
- `WriteInternalAsync`: rents a buffer from `ArrayPool<byte>`, writes struct via `MemoryMarshal.Write`,
  calls `WritePageAsync`, returns buffer to pool
- Must not allocate per-call — use `ArrayPool<byte>.Shared`

---

## Layer 5 — Tree Implementations

### PersistentHeapBase\<T\> (abstract)

**Base for:** `PersistentMinHeap<T>`, `PersistentMaxHeap<T>`, `PersistentMinMaxHeap<T>`  
**Constraint:** `T : unmanaged, IComparable<T>`  
**Does NOT implement:** `IVacuumable` — heaps cannot fragment

**Storage layout:** unchanged from v1. Array index = pageId. Implicit structure.

**Sift math:** unchanged from v1.

**DeleteAsync — O(n) scan + sift:**

```
1. Scan all pages 0..count-1 for the value — O(n)
2. If not found, return false
3. Read last element (pageId = count - 1)
4. Write last element into the found index's page
5. Free page at count - 1
6. Decrement count
7. SiftUp from found index   (value may be smaller than parent)
8. SiftDown from found index (value may be larger than children)
9. Return true
```

Note: both SiftUp and SiftDown must be called because replacing with the last
element could violate the heap property in either direction.

Heaps do not tombstone. There are no dead slots. No vacuum is needed or appropriate.

### PersistentMinHeap\<T\>

**Implements:** `IPriorityTree<T>`  
Subclass of `PersistentHeapBase<T>`. Comparison delegate: `(a, b) => a.CompareTo(b)`.

### PersistentMaxHeap\<T\>

**Implements:** `IPriorityTree<T>`  
Subclass of `PersistentHeapBase<T>`. Comparison delegate: `(a, b) => b.CompareTo(a)`.
Zero duplication of sift logic from `PersistentMinHeap<T>`.

### PersistentMinMaxHeap\<T\>

**Implements:** `IDoubleEndedPriorityTree<T>`  
Level rules, SiftUpMinMax, SiftDownMinMax: unchanged from v1.

**DeleteAsync:** delegates to `PersistentHeapBase<T>.DeleteAsync` — same O(n) scan strategy,
followed by `SiftUpMinMax` and `SiftDownMinMax` from the found index.

### PersistentBPlusTree\<TKey, TValue\>

**Implements:** `IOrderedTree<TKey>`, `IVacuumable`  
**Constraint:** `TKey : unmanaged, IComparable<TKey>`, `TValue : unmanaged`

**Construction:**

```csharp
public PersistentBPlusTree(IStorageBackend storage)
{
    _storage = storage;
    _reader  = NodeReader<TKey, TValue>.Create(storage); // capability detection here
}
```

**Leaf page layout** — updated from v1:

```
[byte  nodeType=0x02]
[int   totalSlots]
[int   liveCount]
[int   deadCount]
[long  prevLeafPageId]
[long  nextLeafPageId]
[BPlusLeafRecord<TKey,TValue>[] records]  ← replaces bare TKey[] keys
```

**InsertAsync:** unchanged from v1 structurally. After inserting a record, call:
```
UpdatePageMetadataAsync(pageId, liveDelta: +1, deadDelta: 0)
```

**ContainsAsync:** must skip records where `IsDeleted == true`.

**ScanAsync / InOrderAsync:** must skip records where `IsDeleted == true`.

**DeleteAsync — O(log n) tombstone:**

```
1. Traverse tree to find leaf page containing the key
2. Binary search leaf records for key — skip tombstoned records
3. If not found, return false
4. Call record.MarkDeleted() on the found record slot
5. leaf.DeadCount++; leaf.LiveCount--
6. Write leaf back via _reader.WriteLeafAsync
7. Call UpdatePageMetadataAsync(pageId, liveDelta: -1, deadDelta: +1)
8. If leaf.LiveCount < MinKeysPerLeaf → call HandleUnderflowAsync(pageId)
9. If leaf.NeedsCompaction(CompactionThreshold) → call CompactPageAsync(pageId)
10. Return true
```

**HandleUnderflowAsync:**

```
1. Get left and right sibling page IDs from parent
2. Try borrowing from right sibling (if rightSibling.LiveCount > MinKeys)
   — move smallest live record from right to current leaf
   — update parent separator key
   — return
3. Try borrowing from left sibling (if leftSibling.LiveCount > MinKeys)
   — move largest live record from left to current leaf
   — update parent separator key
   — return
4. Merge: combine current leaf with chosen sibling
   — copy all live records into one page
   — update linked list pointers (prev/next)
   — free the now-empty page via _storage.FreePageAsync
   — remove child pointer from parent
   — recursively call HandleUnderflowAsync on parent if parent underflows
```

**CompactPageAsync:**

```
1. Read leaf page
2. Two-pointer pack: iterate records, copy live records to front, skip dead
3. Zero out tail slots after live records
4. leaf.DeadCount = 0; leaf.TotalSlots = liveCount
5. Write compacted leaf back
6. Call UpdatePageMetadataAsync(pageId, liveDelta: 0, deadDelta: -reclaimedCount)
7. If leaf.LiveCount == 0 after compaction → FreeLeafPageAsync(pageId)
```

**FreeLeafPageAsync:**

```
1. Read leaf to get prev/next page IDs
2. If prev != -1: read prev, set prev.NextLeafPageId = leaf.NextLeafPageId, write prev
3. If next != -1: read next, set next.PrevLeafPageId = leaf.PrevLeafPageId, write next
4. Call RemoveFromParentAsync(pageId) — may cascade up
5. Call _storage.FreePageAsync(pageId)
```

**GetFragmentationAsync:**

```
1. Iterate GetFragmentedPagesAsync(0.0) to sum totalSlots and deadSlots
2. Return deadSlots / totalSlots or 0.0 if totalSlots == 0
```

**VacuumPageAsync:**

```
1. Call GetFragmentedPagesAsync(CompactionThreshold) — take first result
2. If result found: CompactPageAsync(result.PageId), return true
3. If no result: return false
```

**VacuumAsync:**

```
1. Collect all pages from GetFragmentedPagesAsync(fragmentationThreshold) into a list
2. For each page in list:
   a. ct.ThrowIfCancellationRequested()
   b. Record before.DeadSlots
   c. CompactPageAsync(pageId)
   d. Accumulate bytesReclaimed += before.DeadSlots * RecordSize
   e. Report progress via IProgress<VacuumProgress> if not null
```

**Constants:**

```csharp
private const double CompactionThreshold = 0.5;
private const byte   InternalNodeType    = 0x01;
private const byte   LeafNodeType        = 0x02;
private const long   NullPageId          = -1;
```

---

## Layer 6 — Auto Vacuum Service

### AutoVacuumOptions

```csharp
namespace TreePersistence.Core.Vacuum;

public sealed class AutoVacuumOptions
{
    /// <summary>How often to check fragmentation. Default: 5 minutes.</summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Fragmentation ratio at which vacuum is triggered (0.0–1.0). Default: 0.5.
    /// </summary>
    public double FragmentationThreshold { get; set; } = 0.5;
}
```

### AutoVacuumService

```csharp
namespace TreePersistence.Core.Vacuum;

public sealed class AutoVacuumService : BackgroundService
{
    private readonly IVacuumable _tree;
    private readonly AutoVacuumOptions _options;
    private readonly ILogger<AutoVacuumService> _logger;

    public AutoVacuumService(
        IVacuumable tree,
        IOptions<AutoVacuumOptions> options,
        ILogger<AutoVacuumService> logger)
    {
        _tree    = tree;
        _options = options.Value;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_options.CheckInterval, ct);

            var fragmentation = await _tree.GetFragmentationAsync(ct);

            if (fragmentation < _options.FragmentationThreshold)
                continue;

            _logger.LogInformation(
                "Fragmentation {Ratio:P0} exceeds threshold {Threshold:P0} — vacuuming",
                fragmentation, _options.FragmentationThreshold);

            var progress = new Progress<VacuumProgress>(p =>
                _logger.LogDebug(
                    "Vacuum {Processed}/{Total} pages, {Bytes} bytes reclaimed",
                    p.ProcessedPages, p.TotalPages, p.BytesReclaimed));

            await _tree.VacuumAsync(_options.FragmentationThreshold, progress, ct);

            _logger.LogInformation("Vacuum complete");
        }
    }
}
```

---

## Layer 7 — Factory & Dependency Injection

### StorageBackendFactory

```csharp
public static class StorageBackendFactory
{
    public static IStorageBackend CreateInMemory(int pageSize = 4096);
    public static IStorageBackend CreateOnDisk(string path, int pageSize = 4096);

    /// <summary>
    /// Creates a memory-mapped backend. capacityBytes must be specified upfront.
    /// Returns IZeroCopyStorageBackend — callers may cast if needed.
    /// </summary>
    public static IZeroCopyStorageBackend CreateMmap(
        string path, long capacityBytes, int pageSize = 4096);
}
```

### TreeFactory

```csharp
public static class TreeFactory
{
    public static IPriorityTree<T> CreateMinHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>;

    public static IPriorityTree<T> CreateMaxHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>;

    public static IDoubleEndedPriorityTree<T> CreateMinMaxHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>;

    /// <summary>
    /// Creates a B+ tree. If storage implements IZeroCopyStorageBackend,
    /// the zero-copy read path is selected automatically.
    /// </summary>
    public static IOrderedTree<TKey> CreateBPlusTree<TKey, TValue>(IStorageBackend storage)
        where TKey   : unmanaged, IComparable<TKey>
        where TValue : unmanaged;
}
```

### ServiceCollectionExtensions

```csharp
public static class ServiceCollectionExtensions
{
    /// Registers in-memory min heap as IPriorityTree<T>
    public static IServiceCollection AddInMemoryMinHeap<T>(
        this IServiceCollection services)
        where T : unmanaged, IComparable<T>;

    /// Registers disk-backed B+ tree as IOrderedTree<TKey>
    public static IServiceCollection AddDiskBPlusTree<TKey, TValue>(
        this IServiceCollection services,
        string filePath)
        where TKey   : unmanaged, IComparable<TKey>
        where TValue : unmanaged;

    /// Registers mmap-backed B+ tree as IOrderedTree<TKey>
    /// Also registers the concrete type as IVacuumable for AutoVacuumService.
    public static IServiceCollection AddMmapBPlusTree<TKey, TValue>(
        this IServiceCollection services,
        string filePath,
        long capacityBytes)
        where TKey   : unmanaged, IComparable<TKey>
        where TValue : unmanaged;

    /// Registers AutoVacuumService as a hosted background service.
    /// Requires IVacuumable to be registered first (e.g. via AddMmapBPlusTree).
    public static IServiceCollection AddAutoVacuum(
        this IServiceCollection services,
        Action<AutoVacuumOptions>? configure = null);
}
```

---

## Cross-Cutting Constraints

- All public async methods must accept and honour `CancellationToken`
- No `async void` anywhere
- `IStorageBackend` must be injected — no tree creates its own storage
- All trees and backends must be `IAsyncDisposable` and dispose their dependencies
- `unsafe` code is restricted to two methods in `MmapStorageBackend` only:
  `AcquirePointer` call in constructor, and `GetPageSpan` / `GetPageRef` implementations
- No `unsafe` code anywhere outside `MmapStorageBackend`
- Use `[InlineArray]` for all fixed-size arrays in node structs — no `fixed` keyword
- Use `MemoryMarshal` for all struct/span reinterpretation — no pointer casts outside mmap seam
- Use `ArrayPool<byte>.Shared` in `CopyingNodeReader` — no per-call heap allocations
- Enable `<Nullable>enable</Nullable>` in all projects
- Target framework: `net8.0`
- No third-party dependencies outside `Microsoft.Extensions.DependencyInjection`
  and `Microsoft.Extensions.Hosting` (for `BackgroundService`)

---

## Acceptance Criteria

The implementation is complete when all of the following pass:

```
Storage
✅ MemoryStorageBackend round-trips all page operations
✅ MemoryStorageBackend page metadata updates and fragmentation queries work correctly
✅ FileStorageBackend survives process restart with data, free-list, and metadata intact
✅ MmapStorageBackend read/write round-trips correctly
✅ MmapStorageBackend GetPageSpan returns span pointing into mapped region (verified by write-through)
✅ All three backends are substitutable for IStorageBackend with identical tree behaviour

Serialization
✅ All serializers round-trip without data loss
✅ BPlusLeafRecord tombstone sets Deleted flag and clears Value

Heaps
✅ PersistentMinHeap extracts in ascending order for any insertion sequence
✅ PersistentMaxHeap extracts in descending order for any insertion sequence
✅ PersistentMinMaxHeap PeekAsync = global min, PeekMaxAsync = global max at all times
✅ DeleteAsync on heaps returns false for absent values
✅ DeleteAsync on heaps maintains heap property after removal (both sift directions verified)
✅ Heaps do NOT implement IOrderedTree or IVacuumable

B+ Tree
✅ InsertAsync maintains sorted order for random insertion sequences
✅ ContainsAsync returns false for tombstoned keys
✅ ScanAsync skips tombstoned records
✅ ScanAsync returns only keys within [from, to] inclusive, in order
✅ InOrderAsync returns all live keys in ascending order
✅ DeleteAsync returns false for absent keys
✅ DeleteAsync tombstones record — ContainsAsync returns false immediately after
✅ DeleteAsync triggers HandleUnderflow when LiveCount drops below minimum
✅ HandleUnderflow borrows from sibling when possible, merges when not
✅ CompactPageAsync packs live records, zeroes tail, updates metadata
✅ FreeLeafPageAsync unlinks from leaf list and frees page in backend
✅ B+ tree does NOT implement IPriorityTree

Vacuum
✅ GetFragmentationAsync returns correct ratio after deletions
✅ VacuumPageAsync compacts exactly one page per call
✅ VacuumAsync compacts all pages above threshold and reports accurate progress
✅ VacuumAsync respects CancellationToken
✅ AutoVacuumService triggers vacuum when fragmentation exceeds threshold
✅ AutoVacuumService does not trigger vacuum below threshold

Zero-Copy
✅ PersistentBPlusTree backed by MmapStorageBackend uses ZeroCopyNodeReader (verified by test double)
✅ PersistentBPlusTree backed by FileStorageBackend uses CopyingNodeReader
✅ Both paths produce identical logical results for all tree operations

General
✅ CancellationToken cancels in-progress async I/O without corrupting state
✅ No unsafe code outside MmapStorageBackend (verified by Roslyn analyser or grep)
✅ No per-operation heap allocations in ZeroCopyNodeReader (verified by allocation profiler or BenchmarkDotNet)
```

---

## Implementation Order

Implement strictly in this order. Do not proceed until all tests for the current step pass.

1. `PageMetadata` record
2. `IStorageBackend` (updated interface) + exceptions
3. `MemoryStorageBackend` (including metadata methods) + tests
4. `FileStorageBackend` (including extended header) + tests
5. `IZeroCopyStorageBackend` + `MmapStorageBackend` + tests
6. `INodeSerializer<T>` + `IntSerializer` + `LongSerializer` + tests
7. `RecordFlags` + `BPlusLeafRecord<TKey,TValue>` + `[InlineArray]` buffer types
8. `BPlusInternalNode<TKey>` + `BPlusLeafNode<TKey,TValue>` + `BstNode<T>` + tests
9. All tree interfaces (`ITree<T>`, `IOrderedTree<T>`, `IPriorityTree<T>`,
   `IDoubleEndedPriorityTree<T>`, `IVacuumable`) — contracts only, no implementation
10. `NodeReader<TKey,TValue>` abstract + `ZeroCopyNodeReader` + `CopyingNodeReader` + tests
11. `PersistentHeapBase<T>` including `DeleteAsync` + tests
12. `PersistentMinHeap<T>` + tests
13. `PersistentMaxHeap<T>` + tests
14. `PersistentMinMaxHeap<T>` + tests
15. `PersistentBPlusTree<TKey,TValue>` — Insert, Contains, Scan, InOrder + tests
16. `PersistentBPlusTree<TKey,TValue>` — Delete (tombstone + underflow handling) + tests
17. `PersistentBPlusTree<TKey,TValue>` — Vacuum (IVacuumable implementation) + tests
18. `AutoVacuumOptions` + `AutoVacuumService` + tests
19. `StorageBackendFactory` + `TreeFactory` + `ServiceCollectionExtensions`
20. Full integration tests — storage backend swap, zero-copy path verification,
    end-to-end insert/delete/vacuum lifecycle
```