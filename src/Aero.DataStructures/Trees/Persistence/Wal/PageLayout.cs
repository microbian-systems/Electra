namespace Aero.DataStructures.Trees.Persistence.Wal;

public static class PageLayout
{
    public const int PageLsnOffset = 0;
    public const int PageLsnLength = sizeof(ulong);
    public const int NodeTypeOffset = 8;
    public const int HeaderSize = 16;
}
