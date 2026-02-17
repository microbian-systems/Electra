using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Aero.Common;

public static class DataHelper
{
    public static DataTable ToDataTable<T>(this IEnumerable<T> items) where T : class, new()
    {
        var properties = typeof(T).GetProperties()
            .Where(p=>p.GetGetMethod().IsPublic && (p.GetType().IsPrimitive || typeof(string) == p.GetType())).ToList();
        var result = new DataTable();

        //Build the columns
        foreach (var prop in properties)
        {
            result.Columns.Add(prop.Name, prop.PropertyType);
        }

        //Fill the DataTable
        foreach (var item in items)
        {
            var row = result.NewRow();

            foreach (var prop in properties)
            {
                var itemValue = prop.GetValue(item, new object[] { });
                row[prop.Name] = itemValue;
            }

            result.Rows.Add(row);
        }

        return result;
    }
}