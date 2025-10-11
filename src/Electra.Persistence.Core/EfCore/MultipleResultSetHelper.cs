using Microsoft.EntityFrameworkCore;

namespace Feeds.DataAccess.Core
{
    public static partial class MultipleResultSets
    {
        public static MultipleResultSetWrapper MultipleResults(this DbContext db, string storedProcedure)
        {
            return new MultipleResultSetWrapper(db, storedProcedure);
        }
    }
}
