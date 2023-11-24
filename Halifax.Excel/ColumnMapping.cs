using System.Linq.Expressions;

namespace Halifax.Excel;

internal class ColumnMapping<TObject>
{
    public string ColumnName { get; set; }
    public string PropertyName { get; set; }
    public Expression<Func<TObject, object>> Expression { get; set; }
}