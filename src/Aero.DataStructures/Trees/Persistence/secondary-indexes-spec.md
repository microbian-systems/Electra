# Secondary Indexes — Spec-Driven Development

## Context

This spec extends the `TreePersistence.Core` library (v2), the WAL integration spec,
and the isolation strategies spec. It introduces the document layer heap file,
the secondary index infrastructure, and the multi-index write path.

Read all prior specs before this one. All existing contracts remain unchanged
unless listed under Breaking Changes.

---

## Agent Instructions

You are adding a document/object storage layer and secondary index support
to an existing C# .NET 8 storage engine.
- The heap file is a new storage concept — variable-length records in slotted pages
- Secondary indexes are B+ trees registered in the catalog alongside the primary index
- All index updates (primary + all secondary) must occur within a single WAL transaction
- The `unmanaged` constraint applies to index keys — document values are serialized bytes
- Implement strictly in the order specified — each layer depends on the previous
- All tests for the current step must pass before proceeding

---

## Breaking Changes from Prior Specs

| Location | Change | Reason |
|---|---|---|
| `CatalogEntry` | Added `HeapFilePageId`, `IndexType` fields | Heap file and index type tracking |
| `NodeType` constants | Added `0x03` for heap pages | New page kind |
| `TypeCode` enum | Added `StringKey32/64/128/256` variants | String index keys |
| `PageLayout` | No changes | Heap pages have their own layout constants |

---

## Changelog / What Is New

| Item | Description |
|---|---|
| `HeapAddress` | Blittable `(PageId, SlotIndex)` struct — stable document address |
| `SlottedPage` | Fixed-size page with variable-length record slots |
| `IHeapFile` | Interface for variable-length document storage |
| `HeapFile` | Slotted-page heap file implementation |
| `HeapPageLayout` | Constants for heap page byte layout |
| `IDocumentSerializer<T>` | Pluggable serialization contract |
| `SystemTextJsonSerializer<T>` | Default JSON serializer implementation |
| `MessagePackSerializer<T>` | Optional binary serializer implementation |
| `CompositeKey<TField, TId>` | Blittable composite key for non-unique indexes |
| `StringKey32/64/128/256` | Fixed-width UTF-8 string index keys |
| `UniqueConstraintViolationException` | Thrown on duplicate unique index insert |
| `IndexDefinition` | Non-generic base metadata for a registered index |
| `IndexDefinition<TDoc,TField>` | Generic definition carrying key extractor |
| `IIndexUpdater<TDocument>` | Per-index write coordinator |
| `SecondaryIndexUpdater<TDoc,TField>` | Typed updater for one secondary index |
| `IDocumentIndexRegistry<TDocument>` | Registry mapping field names → index definitions |
| `DocumentIndexRegistry<TDocument>` | Concrete registry implementation |
| `IDocumentCollection<TDocument>` | Public CRUD + scan interface |
| `DocumentCollection<TDocument>` | Orchestrates heap + all indexes in one transaction |
| `DocumentCollectionBuilder<TDocument>` | Fluent registration DSL |
| `IIndexExecutor<TDocument>` | Typed index scan executor used by query layer |
| `IndexRebuildService` | Rebuilds a corrupt or missing index from heap scan |
| `FreeSpaceMap` | Tracks available space per heap page for insert routing |
| DI extensions | `AddDocumentCollection<T>`, updated `AddWal` |

---

## Project Structure (additions only)

```
TreePersistence.Core/
├── Heap/
│   ├── HeapAddress.cs
│   ├── HeapPageLayout.cs
│   ├── SlottedPage.cs
│   ├── FreeSpaceMap.cs
│   ├── IHeapFile.cs
│   └── HeapFile.cs
├── Serialization/
│   ├── IDocumentSerializer.cs
│   ├── SystemTextJsonSerializer.cs
│   └── MessagePackSerializer.cs        ← optional, requires MessagePack nuget
├── Indexes/
│   ├── CompositeKey.cs
│   ├── StringKeys.cs                   ← StringKey32/64/128/256
│   ├── IndexDefinition.cs
│   ├── IndexType.cs
│   ├── IIndexUpdater.cs
│   ├── SecondaryIndexUpdater.cs
│   ├── IDocumentIndexRegistry.cs
│   ├── DocumentIndexRegistry.cs
│   └── IIndexExecutor.cs
├── Documents/
│   ├── IDocumentCollection.cs
│   ├── DocumentCollection.cs
│   ├── DocumentCollectionBuilder.cs
│   └── IndexRebuildService.cs
└── DI/
    └── ServiceCollectionExtensions.cs  ← updated

TreePersistence.Tests/
├── Heap/
│   ├── SlottedPageTests.cs
│   ├── HeapFileTests.cs
│   └── FreeSpaceMapTests.cs
├── Indexes/
│   ├── CompositeKeyTests.cs
│   ├── StringKeyTests.cs
│   ├── SecondaryIndexUpdaterTests.cs
│   └── DocumentIndexRegistryTests.cs
└── Documents/
    ├── DocumentCollectionTests.cs
    ├── IndexConsistencyTests.cs
    └── IndexRebuildTests.cs
```

---

## Layer 1 — Heap Address and Page Layout

### HeapAddress

```csharp
namespace TreePersistence.Core.Heap;

/// <summary>
/// Stable address of a document within the heap file.
/// Blittable — used as TValue in the primary B+ tree index.
/// SlotIndex is stable even after page compaction.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HeapAddress(long PageId, short SlotIndex)
    : IComparable<HeapAddress>
{
    public static readonly HeapAddress Null = new(-1, -1);
    public bool IsNull => PageId == -1;

    public int CompareTo(HeapAddress other)
    {
        var cmp = PageId.CompareTo(other.PageId);
        return cmp != 0 ? cmp : SlotIndex.CompareTo(other.SlotIndex);
    }
}
```

### HeapPageLayout

```csharp
namespace TreePersistence.Core.Heap;

/// <summary>
/// Byte layout constants for a slotted heap page.
///
/// Page structure:
///
/// ┌──────────────────────────────────────────────────────────┐
/// │ HEADER (32 bytes)                                        │
/// │  PageLsn         (8)  ulong                             │
/// │  PageVersion     (4)  uint                              │
/// │  NodeType        (1)  byte = 0x03                       │
/// │  Reserved        (3)                                     │
/// │  SlotCount       (2)  ushort — total slots (incl dead)  │
/// │  LiveCount       (2)  ushort — non-deleted slots        │
/// │  FreeSpaceOffset (2)  ushort — byte where free zone begins│
/// │  Reserved        (10)                                    │
/// ├──────────────────────────────────────────────────────────┤
/// │ SLOT ARRAY (grows downward from offset 32)               │
/// │  Slot[0]: offset (ushort) + length (ushort) + flags (byte)│
/// │  Slot[1]: ...                                            │
/// │  ...                                                     │
/// ├──────────────────────────────────────────────────────────┤
/// │ FREE SPACE (middle)                                      │
/// ├──────────────────────────────────────────────────────────┤
/// │ RECORD DATA (grows upward from end of page)              │
/// │  Records packed from page end toward FreeSpaceOffset     │
/// └──────────────────────────────────────────────────────────┘
///
/// A slot is "dead" if its Flags has RecordFlags.Deleted set.
/// Dead slots retain their index in the slot array — slot indexes
/// are stable and used as the SlotIndex in HeapAddress.
/// </summary>
public static class HeapPageLayout
{
    public const byte   NodeType            = 0x03;

    // Header field offsets
    public const int    PageLsnOffset       = 0;
    public const int    PageVersionOffset   = 8;
    public const int    NodeTypeOffset      = 12;
    public const int    SlotCountOffset     = 16;
    public const int    LiveCountOffset     = 18;
    public const int    FreeSpaceOffset     = 20;
    public const int    HeaderSize          = 32;

    // Per-slot size
    public const int    SlotEntrySize       = 5; // 2 (offset) + 2 (length) + 1 (flags)

    // Slot flags
    public const byte   SlotLive            = 0x00;
    public const byte   SlotDeleted         = 0x01;

    public static int SlotOffset(int slotIndex) =>
        HeaderSize + slotIndex * SlotEntrySize;

    public static int MaxSlots(int pageSize) =>
        (pageSize - HeaderSize) / (SlotEntrySize + 1); // rough upper bound

    public static int FreeSpaceAvailable(int pageSize, int currentFreeSpaceOffset, int slotCount) =>
        pageSize - currentFreeSpaceOffset - (slotCount * SlotEntrySize);
}
```

### SlottedPage

```csharp
namespace TreePersistence.Core.Heap;

/// <summary>
/// Operates on a raw page span to read, write, and compact slots.
/// Does not own the memory — caller provides a page span from IStorageBackend.
/// All operations mutate the span in place.
/// </summary>
public ref struct SlottedPage
{
    private readonly Span<byte> _page;
    private readonly int        _pageSize;

    public SlottedPage(Span<byte> page)
    {
        _page     = page;
        _pageSize = page.Length;
    }

    // Header accessors
    public ulong  PageLsn
    {
        get => BinaryPrimitives.ReadUInt64LittleEndian(_page[HeapPageLayout.PageLsnOffset..]);
        set => BinaryPrimitives.WriteUInt64LittleEndian(_page[HeapPageLayout.PageLsnOffset..], value);
    }

    public ushort SlotCount
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_page[HeapPageLayout.SlotCountOffset..]);
        set => BinaryPrimitives.WriteUInt16LittleEndian(_page[HeapPageLayout.SlotCountOffset..], value);
    }

    public ushort LiveCount
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_page[HeapPageLayout.LiveCountOffset..]);
        set => BinaryPrimitives.WriteUInt16LittleEndian(_page[HeapPageLayout.LiveCountOffset..], value);
    }

    public ushort FreeSpaceStart
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_page[HeapPageLayout.FreeSpaceOffset..]);
        set => BinaryPrimitives.WriteUInt16LittleEndian(_page[HeapPageLayout.FreeSpaceOffset..], value);
    }

    public int FreeBytes =>
        _pageSize - FreeSpaceStart - (SlotCount * HeapPageLayout.SlotEntrySize);

    /// <summary>
    /// Writes a new record into the page. Returns the assigned slot index.
    /// Throws if insufficient free space — caller must check FreeBytes first.
    /// </summary>
    public short WriteRecord(ReadOnlySpan<byte> data)
    {
        if (data.Length > FreeBytes)
            throw new InvalidOperationException(
                $"Insufficient space: need {data.Length}, have {FreeBytes}.");

        // Find a dead slot to reuse, or append a new one
        short slotIndex = FindDeadSlot();
        bool  newSlot   = slotIndex == -1;

        if (newSlot)
            slotIndex = (short)SlotCount;

        // Write record data at end of free space (grows upward from page end)
        var dataOffset = (ushort)(_pageSize - FreeSpaceStart - data.Length);
        data.CopyTo(_page[dataOffset..]);

        // Write slot entry
        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(_page[slotOffset..], dataOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(_page[(slotOffset + 2)..], (ushort)data.Length);
        _page[slotOffset + 4] = HeapPageLayout.SlotLive;

        if (newSlot) SlotCount++;
        LiveCount++;
        FreeSpaceStart += (ushort)data.Length;

        return slotIndex;
    }

    /// <summary>
    /// Reads a live record by slot index.
    /// Returns empty span if slot is dead.
    /// </summary>
    public ReadOnlySpan<byte> ReadRecord(short slotIndex)
    {
        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        var flags      = _page[slotOffset + 4];

        if (flags == HeapPageLayout.SlotDeleted)
            return ReadOnlySpan<byte>.Empty;

        var dataOffset = BinaryPrimitives.ReadUInt16LittleEndian(_page[slotOffset..]);
        var dataLength = BinaryPrimitives.ReadUInt16LittleEndian(_page[(slotOffset + 2)..]);

        return _page.Slice(dataOffset, dataLength);
    }

    /// <summary>
    /// Marks a slot as deleted. Does not reclaim space — use Compact() for that.
    /// </summary>
    public void DeleteRecord(short slotIndex)
    {
        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        _page[slotOffset + 4] = HeapPageLayout.SlotDeleted;
        LiveCount--;
    }

    /// <summary>
    /// Updates a slot in place if new data fits in the existing space.
    /// Returns true if successful, false if data is too large (caller must
    /// delete and re-insert on a page with sufficient space).
    /// </summary>
    public bool TryUpdateRecord(short slotIndex, ReadOnlySpan<byte> newData)
    {
        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        var dataLength = BinaryPrimitives.ReadUInt16LittleEndian(_page[(slotOffset + 2)..]);

        if (newData.Length > dataLength) return false;

        var dataOffset = BinaryPrimitives.ReadUInt16LittleEndian(_page[slotOffset..]);
        newData.CopyTo(_page[dataOffset..]);
        BinaryPrimitives.WriteUInt16LittleEndian(_page[(slotOffset + 2)..], (ushort)newData.Length);
        return true;
    }

    /// <summary>
    /// Compacts the page — removes dead slots and reclaims fragmented space.
    /// Slot indexes of LIVE records are preserved.
    /// Returns the number of bytes reclaimed.
    /// </summary>
    public int Compact()
    {
        var liveRecords = new List<(short SlotIndex, byte[] Data)>();

        for (short i = 0; i < SlotCount; i++)
        {
            var record = ReadRecord(i);
            if (!record.IsEmpty)
                liveRecords.Add((i, record.ToArray()));
        }

        // Zero out the data region
        var dataRegionStart = HeapPageLayout.HeaderSize + SlotCount * HeapPageLayout.SlotEntrySize;
        _page[dataRegionStart..].Clear();

        int freedBytes = FreeSpaceStart;

        // Re-pack records from the end of the page upward
        FreeSpaceStart = 0;
        foreach (var (slotIndex, data) in liveRecords)
        {
            var dataOffset = (ushort)(_pageSize - FreeSpaceStart - data.Length);
            data.CopyTo(_page[dataOffset..]);

            var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
            BinaryPrimitives.WriteUInt16LittleEndian(_page[slotOffset..], dataOffset);
            BinaryPrimitives.WriteUInt16LittleEndian(_page[(slotOffset + 2)..], (ushort)data.Length);

            FreeSpaceStart += (ushort)data.Length;
        }

        return freedBytes - FreeSpaceStart;
    }

    public static SlottedPage InitializePage(Span<byte> page)
    {
        page.Clear();
        page[HeapPageLayout.NodeTypeOffset] = HeapPageLayout.NodeType;
        var sp = new SlottedPage(page);
        sp.SlotCount       = 0;
        sp.LiveCount       = 0;
        sp.FreeSpaceStart  = 0;
        return sp;
    }

    private short FindDeadSlot()
    {
        for (short i = 0; i < SlotCount; i++)
        {
            var slotOffset = HeapPageLayout.SlotOffset(i);
            if (_page[slotOffset + 4] == HeapPageLayout.SlotDeleted)
                return i;
        }
        return -1;
    }
}
```

### Tests: SlottedPage

```
GIVEN an initialized SlottedPage
WHEN WriteRecord([1,2,3,4]) is called
THEN ReadRecord(0) returns [1,2,3,4]
THEN SlotCount == 1, LiveCount == 1

GIVEN a page with one live record at slot 0
WHEN DeleteRecord(0) is called
THEN ReadRecord(0) returns empty span
THEN LiveCount == 0

GIVEN a page with a dead slot at index 0 and free space
WHEN WriteRecord is called again
THEN the dead slot at index 0 is reused (slot reuse)
THEN SlotCount remains 1

GIVEN a page with 3 live records and 1 dead record
WHEN Compact() is called
THEN all 3 live records are readable at their original slot indexes
THEN FreeBytes increased by the dead record's data size

GIVEN a page with insufficient free space
WHEN WriteRecord is called
THEN throws InvalidOperationException

GIVEN TryUpdateRecord with smaller data
THEN returns true and record is updated

GIVEN TryUpdateRecord with larger data than current slot
THEN returns false and record is unchanged
```

---

## Layer 2 — Free Space Map and Heap File

### FreeSpaceMap

```csharp
namespace TreePersistence.Core.Heap;

/// <summary>
/// Tracks approximate free bytes per heap page for insert routing.
/// In-memory only — rebuilt by scanning page headers on open.
/// Avoids scanning all pages to find space for new records.
/// </summary>
public sealed class FreeSpaceMap
{
    // Quantized to 32-byte buckets to reduce map size
    private const int Quantum = 32;
    private readonly ConcurrentDictionary<long, int> _freeBytes = new();

    public void Record(long pageId, int freeBytes) =>
        _freeBytes[pageId] = (freeBytes / Quantum) * Quantum;

    /// <summary>
    /// Returns the first page with at least requiredBytes free space.
    /// Returns -1 if no suitable page exists — caller must allocate a new page.
    /// </summary>
    public long FindPage(int requiredBytes)
    {
        foreach (var (pageId, free) in _freeBytes)
            if (free >= requiredBytes)
                return pageId;
        return -1;
    }

    public void Remove(long pageId) => _freeBytes.TryRemove(pageId, out _);

    public IEnumerable<long> AllPageIds => _freeBytes.Keys;
}
```

### IHeapFile

```csharp
namespace TreePersistence.Core.Heap;

public interface IHeapFile : IAsyncDisposable
{
    /// <summary>
    /// Writes data to any page with sufficient free space.
    /// Returns the stable HeapAddress of the new record.
    /// Allocates a new page if no existing page has room.
    /// Must be called within an active WAL transaction.
    /// </summary>
    ValueTask<HeapAddress> WriteAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default);

    /// <summary>
    /// Reads the raw bytes at address. Throws if slot is dead.
    /// </summary>
    ValueTask<Memory<byte>> ReadAsync(
        HeapAddress address,
        CancellationToken ct = default);

    /// <summary>
    /// Marks the slot at address as deleted.
    /// Space is reclaimed lazily by CompactPageAsync.
    /// </summary>
    ValueTask DeleteAsync(
        HeapAddress address,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a record. If new data fits in the existing slot, updates in place.
    /// Otherwise deletes the old record and writes to a new location.
    /// Returns the (possibly new) HeapAddress.
    /// </summary>
    ValueTask<HeapAddress> UpdateAsync(
        HeapAddress address,
        ReadOnlyMemory<byte> newData,
        CancellationToken ct = default);

    /// <summary>
    /// Compacts a specific page — reclaims space from deleted records.
    /// Called by vacuum service.
    /// </summary>
    ValueTask CompactPageAsync(
        long pageId,
        CancellationToken ct = default);

    /// <summary>
    /// Scans all live records in insertion order.
    /// Used by index rebuild and full collection scans.
    /// </summary>
    IAsyncEnumerable<(HeapAddress Address, Memory<byte> Data)> ScanAllAsync(
        CancellationToken ct = default);

    int  PageSize  { get; }
    long PageCount { get; }
}
```

### HeapFile Implementation Rules

```csharp
public sealed class HeapFile : IHeapFile
{
    private readonly IStorageBackend _storage;
    private readonly FreeSpaceMap    _freeSpaceMap;
    private const int HeapPageType   = 0x03;
}
```

**WriteAsync:**
```
1. Try FindPage(data.Length + SlotEntrySize) from FreeSpaceMap
2. If -1: allocate new page via _storage.AllocatePageAsync,
          initialize as empty SlottedPage,
          write initialized page to storage
3. Read target page from storage
4. Create SlottedPage from page bytes
5. Call slottedPage.WriteRecord(data) → get slotIndex
6. Write modified page back to storage
7. Update FreeSpaceMap.Record(pageId, newFreeBytes)
8. Return new HeapAddress(pageId, slotIndex)
```

**ReadAsync:**
```
1. Read page at address.PageId from storage
2. Create SlottedPage from page bytes
3. Return slottedPage.ReadRecord(address.SlotIndex)
4. If empty span returned: throw RecordDeletedException(address)
```

**DeleteAsync:**
```
1. Read page at address.PageId
2. slottedPage.DeleteRecord(address.SlotIndex)
3. Write modified page back
4. Update FreeSpaceMap with new free byte count
```

**UpdateAsync:**
```
1. Read page at address.PageId
2. Try slottedPage.TryUpdateRecord(address.SlotIndex, newData)
3. If true (fits in place): write page back, return same address
4. If false (too large):
   a. slottedPage.DeleteRecord(address.SlotIndex) — mark old slot dead
   b. Write modified page back
   c. Call WriteAsync(newData) → get new HeapAddress
   d. Return new HeapAddress
```

**ScanAllAsync:**
```
For each pageId in FreeSpaceMap.AllPageIds (+ any pages not in map from initial scan):
  Read page from storage
  Create SlottedPage
  For each slotIndex 0..SlotCount-1:
    record = slottedPage.ReadRecord(slotIndex)
    if not empty: yield (HeapAddress(pageId, slotIndex), record.ToArray())
```

**Construction — rebuild FreeSpaceMap:**
```
On open, scan all pages of type 0x03:
  Read each page header
  Record FreeSpaceMap.Record(pageId, computedFreeBytes)
This ensures the map is accurate after crash recovery.
```

### Tests: HeapFile

```
GIVEN a HeapFile backed by MemoryStorageBackend
WHEN WriteAsync([1,2,3]) is called
THEN ReadAsync returns [1,2,3] at the returned address

GIVEN a written record
WHEN DeleteAsync is called
THEN ReadAsync throws RecordDeletedException

GIVEN a record and UpdateAsync with smaller data
THEN ReadAsync at same address returns new data

GIVEN a record and UpdateAsync with larger data
THEN returned address differs from original
THEN ReadAsync at new address returns new data
THEN ReadAsync at old address throws RecordDeletedException

GIVEN a page near full and a large record
WHEN WriteAsync is called
THEN a new page is allocated
THEN FreeSpaceMap is updated

GIVEN 100 records written then 50 deleted
WHEN ScanAllAsync is called
THEN exactly 50 records are yielded
THEN all addresses are valid

GIVEN a HeapFile closed and reopened
WHEN ScanAllAsync is called
THEN same records are present (persistence test)

GIVEN multiple records on the same page
WHEN CompactPageAsync is called
THEN all live records remain readable at original addresses
THEN free space increases
```

---

## Layer 3 — Document Serialization

### IDocumentSerializer

```csharp
namespace TreePersistence.Core.Serialization;

public interface IDocumentSerializer<TDocument>
    where TDocument : class
{
    /// <summary>
    /// Serializes a document to bytes.
    /// Returned memory must remain valid for the duration of the call site.
    /// </summary>
    ReadOnlyMemory<byte> Serialize(TDocument document);

    /// <summary>
    /// Deserializes bytes back to a document.
    /// Throws SerializationException on malformed data.
    /// </summary>
    TDocument Deserialize(ReadOnlyMemory<byte> bytes);
}
```

### SystemTextJsonSerializer

```csharp
public sealed class SystemTextJsonSerializer<TDocument> : IDocumentSerializer<TDocument>
    where TDocument : class
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented               = false,
        };
    }

    public ReadOnlyMemory<byte> Serialize(TDocument document)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        JsonSerializer.Serialize(writer, document, _options);
        return buffer.WrittenMemory;
    }

    public TDocument Deserialize(ReadOnlyMemory<byte> bytes)
    {
        var result = JsonSerializer.Deserialize<TDocument>(bytes.Span, _options);
        return result ?? throw new SerializationException(
            $"Deserialization of {typeof(TDocument).Name} returned null.");
    }
}
```

### Tests: Serializers

```
GIVEN SystemTextJsonSerializer<Customer>
WHEN Serialize then Deserialize
THEN all fields round-trip correctly including nulls and nested objects

GIVEN malformed JSON bytes
WHEN Deserialize is called
THEN throws SerializationException

GIVEN a document with a null property
WHEN serialized
THEN the null field is omitted (WhenWritingNull)
THEN deserialized document has null for that field
```

---

## Layer 4 — Composite Keys and String Keys

### CompositeKey

```csharp
namespace TreePersistence.Core.Indexes;

/// <summary>
/// Combines a field value with a document ID to form a unique B+ tree key.
/// Used for non-unique secondary indexes — ensures each entry is unique
/// while still allowing range scans on the field portion alone.
///
/// Ordering: field value first, document ID second (as tiebreaker).
/// Range scan for all documents with field = X:
///   ScanAsync(new(X, Guid.Empty), new(X, Guid.MaxValue))
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CompositeKey<TField, TId>(TField Field, TId Id)
    : IComparable<CompositeKey<TField, TId>>
    where TField : unmanaged, IComparable<TField>
    where TId    : unmanaged, IComparable<TId>
{
    public int CompareTo(CompositeKey<TField, TId> other)
    {
        var cmp = Field.CompareTo(other.Field);
        return cmp != 0 ? cmp : Id.CompareTo(other.Id);
    }

    /// <summary>Lower bound for range scan: all documents with this exact field value.</summary>
    public static CompositeKey<TField, TId> RangeLo(TField field) =>
        new(field, default);

    /// <summary>Upper bound for range scan — requires Guid.MaxValue equivalent for TId.</summary>
    public static CompositeKey<Guid, Guid> RangeHiGuid(Guid field) =>
        new(field, GuidMax);

    private static readonly Guid GuidMax = new(
        int.MaxValue, short.MaxValue, short.MaxValue,
        byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
        byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
}
```

### String Keys

Fixed-width UTF-8 byte array keys for string field indexing.
Strings longer than the key width are truncated — post-filter step required.

```csharp
namespace TreePersistence.Core.Indexes;

/// <summary>
/// Marker interface for all fixed-width string key types.
/// </summary>
public interface IStringKey<TSelf> : IComparable<TSelf>
    where TSelf : unmanaged, IStringKey<TSelf>
{
    static abstract TSelf From(string value);
    string ToDisplayString();
    bool IsTruncated { get; }  // true if original string was longer than key width
}

// Generated pattern — one struct per width
// Only StringKey64 shown in full; others follow identical pattern

[InlineArray(64)]
internal struct StringBytes64 { private byte _e; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StringKey64 : IStringKey<StringKey64>
{
    private StringBytes64 _data;

    public static StringKey64 From(string value)
    {
        var key = new StringKey64();
        var span = MemoryMarshal.CreateSpan(ref key._data._e, 64);
        span.Clear();

        var encoded    = Encoding.UTF8.GetBytes(value);
        var truncated  = encoded.AsSpan()[..Math.Min(encoded.Length, 64)];
        truncated.CopyTo(span);

        key.IsTruncated = encoded.Length > 64;
        return key;
    }

    public bool IsTruncated { get; private set; }

    public int CompareTo(StringKey64 other)
    {
        var thisSpan  = MemoryMarshal.CreateReadOnlySpan(ref _data._e, 64);
        var otherSpan = MemoryMarshal.CreateReadOnlySpan(ref other._data._e, 64);
        return thisSpan.SequenceCompareTo(otherSpan);
    }

    public string ToDisplayString()
    {
        var span = MemoryMarshal.CreateReadOnlySpan(ref _data._e, 64);
        var end  = span.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end >= 0 ? span[..end] : span);
    }
}

// StringKey32, StringKey128, StringKey256 follow the same pattern
// Width selection is part of IndexDefinition — recorded in catalog
```

### Tests: Keys

```
GIVEN CompositeKey(Age=25, Id=someGuid)
WHEN compared with CompositeKey(Age=25, Id=otherGuid)
THEN ordered by Guid comparison (field tie broken by Id)

GIVEN CompositeKey(Age=25, Id) and CompositeKey(Age=30, Id)
WHEN compared
THEN Age=25 < Age=30 regardless of Id values

GIVEN ScanAsync from RangeLo(25) to RangeHiGuid(25)
THEN only keys with Field == 25 are returned

GIVEN StringKey64.From("hello")
WHEN ToDisplayString is called
THEN returns "hello"
THEN IsTruncated == false

GIVEN StringKey64.From(string of 100 UTF-8 characters)
WHEN created
THEN IsTruncated == true
THEN ToDisplayString returns first 64 bytes as string

GIVEN StringKey64.From("abc") and StringKey64.From("abd")
WHEN CompareTo is called
THEN "abc" < "abd" (lexicographic)
```

---

## Layer 5 — Index Definitions and Registry

### IndexType and IndexDefinition

```csharp
namespace TreePersistence.Core.Indexes;

public enum IndexType : byte
{
    Primary   = 0x01,
    Secondary = 0x02,
    Unique    = 0x03,   // secondary with uniqueness enforced
    Composite = 0x04,   // multi-field secondary
}

/// <summary>
/// Non-generic metadata for a registered index.
/// Stored in catalog and used by query planner.
/// </summary>
public sealed class IndexDefinition
{
    public string    Name        { get; init; } = string.Empty;
    public IndexType Type        { get; init; }
    public bool      IsUnique    { get; init; }
    public bool      IsDescending { get; init; }
    public Type      FieldType   { get; init; } = typeof(object);
    public string    FieldName   { get; init; } = string.Empty;

    /// <summary>Page ID of this index's B+ tree root. Set when index is first created.</summary>
    public long      RootPageId  { get; set; } = -1;

    /// <summary>
    /// For string indexes: the key width (32, 64, 128, 256).
    /// Zero for non-string indexes.
    /// </summary>
    public int       StringKeyWidth { get; init; }

    /// <summary>True if this index was defined on a string field requiring fixed-width key.</summary>
    public bool      IsStringIndex => StringKeyWidth > 0;
}

/// <summary>
/// Generic definition carrying the key extractor function.
/// Created at registration time from a LINQ expression.
/// </summary>
public sealed class IndexDefinition<TDocument, TField> : IndexDefinition
    where TField : unmanaged, IComparable<TField>
{
    /// <summary>Compiled key extractor — called on every insert/update/delete.</summary>
    public required Func<TDocument, TField>              KeyExtractor  { get; init; }

    /// <summary>Original expression — retained for query planner inspection.</summary>
    public required Expression<Func<TDocument, TField>>  KeyExpression { get; init; }
}
```

### IDocumentIndexRegistry

```csharp
namespace TreePersistence.Core.Indexes;

public interface IDocumentIndexRegistry<TDocument>
{
    /// <summary>Finds an index definition by document field name. Null if not registered.</summary>
    IndexDefinition? FindByField(string fieldName);

    /// <summary>All registered secondary index definitions.</summary>
    IReadOnlyList<IndexDefinition> AllIndexes { get; }

    /// <summary>Gets a typed executor capable of performing scans on a specific index.</summary>
    IIndexExecutor<TDocument> GetExecutor(IndexDefinition definition);

    /// <summary>
    /// Registers a new index. Called by DocumentCollectionBuilder.
    /// Throws if an index with the same name already exists.
    /// </summary>
    void Register<TField>(
        IndexDefinition<TDocument, TField> definition,
        IOrderedTree<CompositeKey<TField, Guid>, Guid> tree)
        where TField : unmanaged, IComparable<TField>;

    void RegisterUnique<TField>(
        IndexDefinition<TDocument, TField> definition,
        IOrderedTree<TField, Guid> tree)
        where TField : unmanaged, IComparable<TField>;
}
```

### IIndexExecutor

```csharp
namespace TreePersistence.Core.Indexes;

/// <summary>
/// Typed executor for an index scan.
/// Returned by IDocumentIndexRegistry.GetExecutor and used by the query planner.
/// Returns document IDs — callers resolve to documents via the primary index.
/// </summary>
public interface IIndexExecutor<TDocument>
{
    IndexDefinition Definition { get; }

    /// <summary>Point lookup — equality on the index field.</summary>
    IAsyncEnumerable<Guid> LookupAsync(
        object fieldValue,
        CancellationToken ct = default);

    /// <summary>
    /// Range scan — inclusive bounds on both ends.
    /// Pass null for unbounded end.
    /// </summary>
    IAsyncEnumerable<Guid> ScanRangeAsync(
        object? from,
        object? to,
        CancellationToken ct = default);
}
```

### DocumentIndexRegistry Implementation Rules

```csharp
public sealed class DocumentIndexRegistry<TDocument> : IDocumentIndexRegistry<TDocument>
    where TDocument : class
{
    private readonly Dictionary<string, IndexDefinition>              _byField     = new();
    private readonly Dictionary<string, IIndexExecutor<TDocument>>   _executors   = new();
    private readonly Dictionary<string, IIndexUpdater<TDocument>>    _updaters    = new();

    // Register is called during collection builder setup
    // GetExecutor and GetUpdaters used at runtime
    public IReadOnlyList<IIndexUpdater<TDocument>> AllUpdaters =>
        _updaters.Values.ToList();
}
```

---

## Layer 6 — Index Updaters

### IIndexUpdater

```csharp
namespace TreePersistence.Core.Indexes;

public interface IIndexUpdater<TDocument>
{
    ValueTask OnInsertAsync(Guid id, TDocument document, CancellationToken ct);
    ValueTask OnUpdateAsync(Guid id, TDocument oldDoc, TDocument newDoc, CancellationToken ct);
    ValueTask OnDeleteAsync(Guid id, TDocument document, CancellationToken ct);
}
```

### SecondaryIndexUpdater (Non-Unique)

```csharp
public sealed class SecondaryIndexUpdater<TDocument, TField> : IIndexUpdater<TDocument>
    where TField : unmanaged, IComparable<TField>
{
    private readonly IOrderedTree<CompositeKey<TField, Guid>, Guid> _index;
    private readonly Func<TDocument, TField>                         _extractor;

    public async ValueTask OnInsertAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);
        await _index.InsertAsync(new CompositeKey<TField, Guid>(key, id), id, ct);
    }

    public async ValueTask OnUpdateAsync(
        Guid id, TDocument old, TDocument updated, CancellationToken ct)
    {
        var oldKey = _extractor(old);
        var newKey = _extractor(updated);

        if (EqualityComparer<TField>.Default.Equals(oldKey, newKey))
            return; // field unchanged — no index work needed

        await _index.DeleteAsync(new CompositeKey<TField, Guid>(oldKey, id), ct);
        await _index.InsertAsync(new CompositeKey<TField, Guid>(newKey, id), id, ct);
    }

    public async ValueTask OnDeleteAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);
        await _index.DeleteAsync(new CompositeKey<TField, Guid>(key, id), ct);
    }
}
```

### UniqueIndexUpdater

```csharp
public sealed class UniqueIndexUpdater<TDocument, TField> : IIndexUpdater<TDocument>
    where TField : unmanaged, IComparable<TField>
{
    private readonly IOrderedTree<TField, Guid> _index;
    private readonly Func<TDocument, TField>    _extractor;
    private readonly string                     _indexName;

    public async ValueTask OnInsertAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);

        if (await _index.ContainsAsync(key, ct))
            throw new UniqueConstraintViolationException(_indexName, key.ToString()!);

        await _index.InsertAsync(key, id, ct);
    }

    public async ValueTask OnUpdateAsync(
        Guid id, TDocument old, TDocument updated, CancellationToken ct)
    {
        var oldKey = _extractor(old);
        var newKey = _extractor(updated);

        if (EqualityComparer<TField>.Default.Equals(oldKey, newKey))
            return;

        if (await _index.ContainsAsync(newKey, ct))
            throw new UniqueConstraintViolationException(_indexName, newKey.ToString()!);

        await _index.DeleteAsync(oldKey, ct);
        await _index.InsertAsync(newKey, id, ct);
    }

    public async ValueTask OnDeleteAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);
        await _index.DeleteAsync(key, ct);
    }
}

public sealed class UniqueConstraintViolationException(string indexName, string value)
    : Exception($"Unique index '{indexName}' already contains value '{value}'.");
```

### Tests: Updaters

```
GIVEN a SecondaryIndexUpdater for Customer.Age
WHEN OnInsertAsync(id, customer{Age=25}) is called
THEN ScanRangeAsync on the index returns id for Age=25

GIVEN two customers with Age=25
WHEN both are inserted via SecondaryIndexUpdater
THEN ScanRangeAsync returns both IDs (composite key uniqueness)

GIVEN a customer with Age=25 updated to Age=30
WHEN OnUpdateAsync is called
THEN ScanRangeAsync for Age=25 no longer returns the ID
THEN ScanRangeAsync for Age=30 returns the ID

GIVEN OnUpdateAsync called with same field value
THEN no index operations are performed (optimization verified via mock)

GIVEN UniqueIndexUpdater for Customer.Email
WHEN two customers with same email are inserted
THEN second insert throws UniqueConstraintViolationException

GIVEN a unique index entry that is deleted
WHEN same value is inserted again
THEN insert succeeds
```

---

## Layer 7 — Document Collection

### IDocumentCollection

```csharp
namespace TreePersistence.Core.Documents;

public interface IDocumentCollection<TDocument> where TDocument : class
{
    /// <summary>
    /// Inserts a new document. Extracts the primary key from the document.
    /// All secondary indexes updated atomically in the same WAL transaction.
    /// Throws UniqueConstraintViolationException if a unique index is violated.
    /// </summary>
    ValueTask<Guid> InsertAsync(
        TDocument document,
        CancellationToken ct = default);

    /// <summary>
    /// Finds a document by primary key. Returns null if not found.
    /// </summary>
    ValueTask<TDocument?> FindAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Replaces the document at id. Re-extracts all index field values
    /// from both old and new document and updates changed indexes.
    /// Returns false if document not found.
    /// </summary>
    ValueTask<bool> UpdateAsync(
        Guid id,
        TDocument document,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a document and all its index entries.
    /// Returns false if not found.
    /// </summary>
    ValueTask<bool> DeleteAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a queryable for LINQ query composition.
    /// See the LINQ provider spec for usage.
    /// </summary>
    IQueryable<TDocument> AsQueryable();

    /// <summary>
    /// Explicit index range scan. Bypasses query planning — always uses the named index.
    /// fieldSelector must refer to an indexed field.
    /// Throws ArgumentException if field is not indexed.
    /// </summary>
    IAsyncEnumerable<TDocument> ScanIndexAsync<TField>(
        Expression<Func<TDocument, TField>> fieldSelector,
        TField from,
        TField to,
        CancellationToken ct = default)
        where TField : unmanaged, IComparable<TField>;

    /// <summary>
    /// Full collection scan — iterates all documents in heap order.
    /// O(n). Use only when no applicable index exists.
    /// </summary>
    IAsyncEnumerable<TDocument> ScanAllAsync(
        CancellationToken ct = default);

    long ApproximateCount { get; }
}
```

### DocumentCollection Implementation

```csharp
public sealed class DocumentCollection<TDocument> : IDocumentCollection<TDocument>
    where TDocument : class
{
    private readonly IWalStorageBackend                          _storage;
    private readonly IHeapFile                                   _heap;
    private readonly IOrderedTree<Guid, HeapAddress>             _primaryIndex;
    private readonly IDocumentIndexRegistry<TDocument>           _indexRegistry;
    private readonly IDocumentSerializer<TDocument>              _serializer;
    private readonly Func<TDocument, Guid>                       _idExtractor;
    private long _approximateCount;

    public long ApproximateCount => Interlocked.Read(ref _approximateCount);

    public async ValueTask<Guid> InsertAsync(TDocument document, CancellationToken ct = default)
    {
        await using var txn = await _storage.BeginTransactionAsync(ct);
        using var _scope    = TransactionContext.Scope(txn.TransactionId);

        try
        {
            var id    = _idExtractor(document);
            if (id == Guid.Empty) id = Guid.NewGuid();

            if (await _primaryIndex.ContainsAsync(id, ct))
                throw new DuplicateKeyException(id);

            var bytes   = _serializer.Serialize(document);
            var address = await _heap.WriteAsync(bytes, ct);

            await _primaryIndex.InsertAsync(id, address, ct);

            foreach (var updater in _indexRegistry.AllUpdaters)
                await updater.OnInsertAsync(id, document, ct);

            await txn.CommitAsync(ct);
            Interlocked.Increment(ref _approximateCount);
            return id;
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }
    }

    public async ValueTask<TDocument?> FindAsync(Guid id, CancellationToken ct = default)
    {
        // Reads do not require a full transaction — single atomic read
        var address = await _primaryIndex.FindAsync(id, ct);
        if (address is null || address.Value.IsNull) return null;

        var bytes = await _heap.ReadAsync(address.Value, ct);
        return _serializer.Deserialize(bytes);
    }

    public async ValueTask<bool> UpdateAsync(
        Guid id, TDocument document, CancellationToken ct = default)
    {
        await using var txn = await _storage.BeginTransactionAsync(ct);
        using var _scope    = TransactionContext.Scope(txn.TransactionId);

        try
        {
            var oldAddress = await _primaryIndex.FindAsync(id, ct);
            if (oldAddress is null || oldAddress.Value.IsNull) return false;

            var oldBytes   = await _heap.ReadAsync(oldAddress.Value, ct);
            var oldDoc     = _serializer.Deserialize(oldBytes);
            var newBytes   = _serializer.Serialize(document);
            var newAddress = await _heap.UpdateAsync(oldAddress.Value, newBytes, ct);

            if (newAddress != oldAddress.Value)
                await _primaryIndex.UpdateAsync(id, newAddress, ct);

            foreach (var updater in _indexRegistry.AllUpdaters)
                await updater.OnUpdateAsync(id, oldDoc, document, ct);

            await txn.CommitAsync(ct);
            return true;
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }
    }

    public async ValueTask<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var txn = await _storage.BeginTransactionAsync(ct);
        using var _scope    = TransactionContext.Scope(txn.TransactionId);

        try
        {
            var address = await _primaryIndex.FindAsync(id, ct);
            if (address is null || address.Value.IsNull) return false;

            var bytes  = await _heap.ReadAsync(address.Value, ct);
            var oldDoc = _serializer.Deserialize(bytes);

            // Secondary indexes first — need old field values
            foreach (var updater in _indexRegistry.AllUpdaters)
                await updater.OnDeleteAsync(id, oldDoc, ct);

            await _primaryIndex.DeleteAsync(id, ct);
            await _heap.DeleteAsync(address.Value, ct);

            await txn.CommitAsync(ct);
            Interlocked.Decrement(ref _approximateCount);
            return true;
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }
    }

    public async IAsyncEnumerable<TDocument> ScanIndexAsync<TField>(
        Expression<Func<TDocument, TField>> fieldSelector,
        TField from,
        TField to,
        [EnumeratorCancellation] CancellationToken ct = default)
        where TField : unmanaged, IComparable<TField>
    {
        var fieldName = GetFieldName(fieldSelector);
        var def       = _indexRegistry.FindByField(fieldName)
                        ?? throw new ArgumentException(
                            $"Field '{fieldName}' is not indexed.", nameof(fieldSelector));

        var executor = _indexRegistry.GetExecutor(def);

        await foreach (var docId in executor.ScanRangeAsync(from, to, ct))
        {
            var doc = await FindAsync(docId, ct);
            if (doc is not null) yield return doc;
        }
    }

    public async IAsyncEnumerable<TDocument> ScanAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (_, data) in _heap.ScanAllAsync(ct))
            yield return _serializer.Deserialize(data);
    }

    private static string GetFieldName<TField>(Expression<Func<TDocument, TField>> expr) =>
        ((MemberExpression)expr.Body).Member.Name;
}
```

---

## Layer 8 — Builder, Index Rebuild, and DI

### DocumentCollectionBuilder

```csharp
namespace TreePersistence.Core.Documents;

public sealed class DocumentCollectionBuilder<TDocument>
    where TDocument : class
{
    private readonly IWalStorageBackend              _storage;
    private readonly IDocumentIndexRegistry<TDocument> _registry;
    private Func<TDocument, Guid>?                   _idExtractor;
    private IDocumentSerializer<TDocument>?          _serializer;

    public DocumentCollectionBuilder<TDocument> HasPrimaryKey(
        Expression<Func<TDocument, Guid>> keySelector)
    {
        _idExtractor = keySelector.Compile();
        return this;
    }

    public DocumentCollectionBuilder<TDocument> HasIndex<TField>(
        Expression<Func<TDocument, TField>> fieldSelector,
        Action<IndexOptions>? configure = null)
        where TField : unmanaged, IComparable<TField>
    {
        var options   = new IndexOptions();
        configure?.Invoke(options);
        var fieldName = GetFieldName(fieldSelector);

        var def = new IndexDefinition<TDocument, TField>
        {
            Name         = options.Name ?? fieldName,
            Type         = options.IsUnique ? IndexType.Unique : IndexType.Secondary,
            IsUnique     = options.IsUnique,
            IsDescending = options.IsDescending,
            FieldName    = fieldName,
            FieldType    = typeof(TField),
            KeyExtractor = fieldSelector.Compile(),
            KeyExpression = fieldSelector,
        };

        if (options.IsUnique)
        {
            var tree = CreateUniqueTree<TField>(def.Name);
            _registry.RegisterUnique(def, tree);
        }
        else
        {
            var tree = CreateCompositeTree<TField>(def.Name);
            _registry.Register(def, tree);
        }

        return this;
    }

    public DocumentCollectionBuilder<TDocument> UseSerializer(
        IDocumentSerializer<TDocument> serializer)
    {
        _serializer = serializer;
        return this;
    }

    public IDocumentCollection<TDocument> Build()
    {
        if (_idExtractor is null)
            throw new InvalidOperationException("Primary key must be configured via HasPrimaryKey.");

        _serializer ??= new SystemTextJsonSerializer<TDocument>();

        var heap         = CreateHeapFile();
        var primaryIndex = CreatePrimaryIndex();

        return new DocumentCollection<TDocument>(
            _storage, heap, primaryIndex, _registry, _serializer, _idExtractor);
    }

    private static string GetFieldName<TField>(Expression<Func<TDocument, TField>> expr) =>
        ((MemberExpression)expr.Body).Member.Name;
}

public sealed class IndexOptions
{
    public string? Name        { get; set; }
    public bool    IsUnique    { get; set; }
    public bool    IsDescending { get; set; }
}
```

### IndexRebuildService

```csharp
namespace TreePersistence.Core.Documents;

/// <summary>
/// Rebuilds a corrupt or missing secondary index from a full heap scan.
/// Should only be run offline or during a maintenance window.
/// </summary>
public sealed class IndexRebuildService<TDocument> where TDocument : class
{
    private readonly IDocumentCollection<TDocument>    _collection;
    private readonly IDocumentIndexRegistry<TDocument> _registry;
    private readonly IDocumentSerializer<TDocument>    _serializer;
    private readonly IHeapFile                         _heap;

    public async ValueTask RebuildAllAsync(
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        foreach (var index in _registry.AllIndexes)
            await RebuildIndexAsync(index.FieldName, progress, ct);
    }

    public async ValueTask RebuildIndexAsync(
        string fieldName,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var def     = _registry.FindByField(fieldName)
                      ?? throw new ArgumentException($"No index found for field '{fieldName}'.");
        var updater = _registry.GetUpdaterForField(fieldName);

        // Step 1: clear the existing index tree
        await ClearIndexAsync(def, ct);

        int count = 0;

        // Step 2: scan heap and re-insert every live document into the index
        await foreach (var (address, data) in _heap.ScanAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            var document = _serializer.Deserialize(data);
            var id       = ExtractId(document);

            await updater.OnInsertAsync(id, document, ct);
            progress?.Report(++count);
        }
    }
}
```

### Updated ServiceCollectionExtensions

```csharp
public static IServiceCollection AddDocumentCollection<TDocument>(
    this IServiceCollection services,
    Action<DocumentCollectionBuilder<TDocument>> configure)
    where TDocument : class
{
    services.AddSingleton<IDocumentIndexRegistry<TDocument>,
        DocumentIndexRegistry<TDocument>>();

    services.AddSingleton<IDocumentCollection<TDocument>>(sp =>
    {
        var storage  = sp.GetRequiredService<IWalStorageBackend>();
        var registry = sp.GetRequiredService<IDocumentIndexRegistry<TDocument>>();
        var builder  = new DocumentCollectionBuilder<TDocument>(storage, registry);
        configure(builder);
        return builder.Build();
    });

    return services;
}
```

### Usage Example

```csharp
// Registration
builder.Services
    .AddMmapBPlusTree<Guid, HeapAddress>("customers.tpd", 2_147_483_648)
    .AddWal("customers.tpw", IsolationLevel.SnapshotMVCC)
    .AddCheckpointService()
    .AddAutoVacuum()
    .AddDocumentCollection<Customer>(col =>
    {
        col.HasPrimaryKey(c => c.Id);
        col.HasIndex(c => c.Age);
        col.HasIndex(c => c.Email, opts => opts.IsUnique = true);
        col.HasIndex(c => c.LastName);
        col.UseSerializer(new MessagePackSerializer<Customer>());
    });

// Usage
IDocumentCollection<Customer> customers = ...;

var id = await customers.InsertAsync(new Customer
{
    Id    = Guid.NewGuid(),
    Name  = "Alice",
    Age   = 30,
    Email = "alice@example.com"
});

var alice = await customers.FindAsync(id);

// Explicit index scan
var adults = customers.ScanIndexAsync(c => c.Age, from: 18, to: 65);
```

---

## Acceptance Criteria

```
Slotted Page
✅ WriteRecord then ReadRecord returns identical bytes
✅ Dead slot reuse on subsequent WriteRecord
✅ Compact preserves live records at original slot indexes
✅ FreeBytes increases correctly after Compact

Heap File
✅ Write/Read round-trip across backend implementations
✅ Update in-place when data fits existing slot
✅ Update allocates new page when data grows beyond slot
✅ ScanAllAsync yields only live records
✅ FreeSpaceMap rebuilt correctly on reopen
✅ CompactPageAsync reclaims space without data loss

Composite Keys
✅ Ordering: field comparison first, Id tiebreaker second
✅ Range scan for single field value using RangeLo/RangeHi bounds

String Keys
✅ UTF-8 round-trip for ASCII and multi-byte characters
✅ IsTruncated correct for strings exceeding key width
✅ Lexicographic ordering consistent with string.Compare

Index Updaters
✅ Non-unique: composite key insert/update/delete
✅ Non-unique: two documents with same field value both retrievable
✅ Unique: duplicate insert throws UniqueConstraintViolationException
✅ Update skips index when field value unchanged

DocumentCollection
✅ Insert: heap + primary index + all secondary indexes updated atomically
✅ Find: returns null for missing ID
✅ Update: only changed index fields trigger index updates
✅ Delete: all index entries removed before heap record deleted
✅ Crash during insert (simulated): all indexes consistent after recovery
✅ Crash during update (simulated): all indexes consistent after recovery
✅ ScanIndexAsync returns only documents matching range
✅ ScanAllAsync yields all documents

Index Rebuild
✅ RebuildIndexAsync produces identical index to original
✅ RebuildAllAsync processes all registered indexes
✅ Rebuild is idempotent — running twice produces same result
```

---

## Implementation Order

1. `HeapAddress` + `HeapPageLayout` constants + tests
2. `SlottedPage` ref struct + tests (all slot operations)
3. `FreeSpaceMap` + tests
4. `IHeapFile` + `HeapFile` implementation + tests
5. `IDocumentSerializer<T>` + `SystemTextJsonSerializer<T>` + tests
6. `CompositeKey<TField, TId>` + range scan tests
7. `StringKey32/64/128/256` + ordering + truncation tests
8. `IndexDefinition` hierarchy + `IndexType` enum
9. `IIndexUpdater<T>` + `SecondaryIndexUpdater` + `UniqueIndexUpdater` + tests
10. `IDocumentIndexRegistry<T>` + `DocumentIndexRegistry<T>` + `IIndexExecutor<T>`
11. `IDocumentCollection<T>` interface
12. `DocumentCollection<T>` — Insert + Find + tests
13. `DocumentCollection<T>` — Update + Delete + tests
14. `DocumentCollectionBuilder<T>` + `IndexOptions`
15. `IndexRebuildService<T>` + rebuild tests
16. Updated `ServiceCollectionExtensions` + DI integration tests
17. Cross-cutting: crash simulation tests for multi-index consistency
18. Cross-cutting: MVCC + document collection concurrent access tests
