namespace Halifax.Core.Extensions;

public static class LinqExtensions
{
    public static List<TObject> Each<TObject>(this IEnumerable<TObject> objects, Action<TObject> action)
    {
        var list = objects as List<TObject> ?? objects.ToList();
        list.ForEach(action);
        return list;
    }

    public static IEnumerable<IEnumerable<TObject>> Batch<TObject>(this IEnumerable<TObject> objects, int size)
    {
        return objects
            .Select((item, i) => new { item, i })
            .GroupBy(tuple => tuple.i / size)
            .Select(g => g.Select(tuple => tuple.item));
    }

    public static async Task<List<TSource>> ToListAsync<TSource>(this IAsyncEnumerable<TSource> source)
    {
        var result = new List<TSource>();

        await foreach (var element in source)
        {
            result.Add(element);
        }

        return result;
    }
}
