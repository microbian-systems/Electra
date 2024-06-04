namespace Electra.Common
{
    public static class QueryableExtensions
    {
        public static Task<PaginatedResult<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize) where T : class
        {
            if (source == null) 
                throw new ArgumentNullException();
            
            pageNumber = pageNumber == 0 ? 1 : pageNumber;
            pageSize = pageSize == 0 ? 10 : pageSize;
            var count = source.Count();
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            
            var items =  source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsEnumerable();
            
            return Task.FromResult(PaginatedResult<T>
                .Success(items, count, pageNumber, pageSize));
        }
    }
    
    public class PaginatedResult<T> //: Result
    {
        public PaginatedResult() { }
        public PaginatedResult(List<T> data)
        {
            Data = data;
        }

        public IEnumerable<T> Data { get; set; } = new List<T>();
        public bool Succeeded { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        internal PaginatedResult(bool succeeded, IEnumerable<T> data = default, IEnumerable<string> messages = null, int count = 0, int page = 1, int pageSize = 10)
        {
            Data = data;
            CurrentPage = page;
            Succeeded = succeeded;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            TotalCount = count;
        }
        
        public static PaginatedResult<T> Failure(IEnumerable<string> messages)
        {
            return new PaginatedResult<T>(false, default, messages);
        }

        public static PaginatedResult<T> Success(IEnumerable<T> data, int count, int page, int pageSize)
        {
            return new PaginatedResult<T>(true, data, null, count, page, pageSize);
        }
    }
}