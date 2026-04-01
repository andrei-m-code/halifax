namespace Halifax.Core.Extensions;

/// <summary>
/// LINQ extension methods for collections and async enumerables.
/// </summary>
public static class LinqExtensions
{
    /// <summary>Executes an action on each item with its index and returns the list.</summary>
    public static List<TObject> Each<TObject>(this IEnumerable<TObject> objects, Action<TObject, int> action)
    {
        var index = 0;
        var list = objects.Each(item => action(item, index++));
        return list;
    }

    /// <summary>Executes an action on each item and returns the list.</summary>
    public static List<TObject> Each<TObject>(this IEnumerable<TObject> objects, Action<TObject> action)
    {
        var list = objects as List<TObject> ?? objects.ToList();
        list.ForEach(action);
        return list;
    }

    /// <summary>Splits a sequence into batches of the specified size.</summary>
    public static IEnumerable<IEnumerable<TObject>> Batch<TObject>(this IEnumerable<TObject> objects, int size)
    {
        return objects
            .Select((item, i) => new { item, i })
            .GroupBy(tuple => tuple.i / size)
            .Select(g => g.Select(tuple => tuple.item));
    }

    /// <summary>Materializes an async enumerable into a list.</summary>
    public static async Task<List<TSource>> ToListAsync<TSource>(this IAsyncEnumerable<TSource> source)
    {
        List<TSource> result = [];

        await foreach (var element in source)
        {
            result.Add(element);
        }

        return result;
    }
}
