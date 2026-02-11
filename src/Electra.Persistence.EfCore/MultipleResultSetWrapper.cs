using System.Collections;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Electra.Persistence.EfCore
{

    public class MultipleResultSetWrapper
    {
        private static ILogger log;
        private readonly DbContext _db;
        private readonly string _storedProcedure;
        public List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>> _resultSets;

        public MultipleResultSetWrapper(DbContext db, string storedProcedure)
        {
            _db = db;
            _storedProcedure = storedProcedure;
            _resultSets = new List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>>();

        }

        public MultipleResultSetWrapper With<TResult>()
        {
            _resultSets.Add((adapter, reader) => adapter
                .ObjectContext
                .Translate<TResult>(reader)
                .ToList());

            return this;
        }

        public List<IEnumerable> Execute()
        {
            var results = new List<IEnumerable>();

            using (var connection = _db.Database.Connection)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "EXEC " + _storedProcedure;
                
                using (var reader = command.ExecuteReader())
                {
                    var adapter = ((IObjectContextAdapter)_db);
                    foreach (var resultSet in _resultSets)
                    {
                        results.Add(resultSet(adapter, reader));
                        reader.NextResult();
                    }
                }

                return results;
            }
        }
    }

}
