using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace ZauberCMS.Core.Shared.Models;

public class PaginatedList<T>
{
    public PaginatedList()
    {
    }
    
    public int PageIndex { get; set; } = 1;
    public int TotalPages { get; set; }
    public long TotalItems { get; set; }
    public IEnumerable<T> Items { get; set; } = [];
    public bool IsFirstPage => PageIndex <= 1;
    public bool IsLastPage => PageIndex >= TotalPages;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
    public PaginatedList(IRavenQueryable<T> items, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        var count = items.Count();
        TotalPages = (int) Math.Ceiling(count / (double) pageSize);
        var skip = (pageIndex-1) * pageSize;
        Items = skip > 0 ? items.Skip(skip).Take(pageSize).ToList() : items.Take(pageSize).ToList();
        TotalItems = count;
    }
    
    public PaginatedList(List<T> items, long count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int) Math.Ceiling(count / (double) pageSize);
        Items = items;
        TotalItems = count;
    }
}