using System;
using System.Runtime.InteropServices;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Format;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CatalogEntry
{
    public ulong PageLsn;
    public long TransactionId;
    public unsafe fixed byte TreeName[128];
    public long RootPageId;
    public byte TreeType;
    public byte KeyTypeCode;
    public byte ValueTypeCode;
    public int PageSize;
    public long CreatedAtUtc;
    public long EntryCount;
    public byte IsolationLevel;

    public const int TreeNameLength = 128;
    public const int Size = 8 + 8 + 128 + 8 + 1 + 1 + 1 + 4 + 8 + 8 + 1;

    public static readonly int SerializedSize = Marshal.SizeOf<CatalogEntry>();

    public string GetTreeName()
    {
        unsafe
        {
            var bytes = new byte[TreeNameLength];
            for (int i = 0; i < TreeNameLength; i++)
            {
                bytes[i] = TreeName[i];
                if (bytes[i] == 0)
                {
                    Array.Resize(ref bytes, i);
                    break;
                }
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }

    public void SetTreeName(string name)
    {
        unsafe
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(name ?? string.Empty);
            var length = Math.Min(bytes.Length, TreeNameLength - 1);
            
            for (int i = 0; i < TreeNameLength; i++)
            {
                TreeName[i] = i < length ? bytes[i] : (byte)0;
            }
        }
    }
}
