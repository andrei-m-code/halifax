using System.Linq.Expressions;

namespace Halifax.Excel;

internal class ColumnMapping<TObject>
{
    public required string ColumnName { get; set; }
    public required string PropertyName { get; set; }
    public required Expression<Func<TObject, object>> Expression { get; set; }
}
