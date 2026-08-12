namespace Halifax.Core.Extensions;

/// <summary>
/// LINQ extension methods for collections and async enumerables.
/// </summary>
public static class LinqExtensions
{
    /// <summary>Executes an action on each item, passing the zero-based index, and returns the materialized list.</summary>
    /// <typeparam name="TObject">The element type of the sequence.</typeparam>
    /// <param name="objects">The sequence to iterate.</param>
    /// <param name="action">The action to invoke for each item and its index.</param>
    /// <returns>The items as a <see cref="List{T}"/> (the same instance when <paramref name="objects"/> is already a list).</returns>
    public static List<TObject> Each<TObject>(this IEnumerable<TObject> objects, Action<TObject, int> action)
    {
        var index = 0;
        var list = objects.Each(item => action(item, index++));
        return list;
    }

    /// <summary>Executes an action on each item and returns the materialized list.</summary>
    /// <typeparam name="TObject">The element type of the sequence.</typeparam>
    /// <param name="objects">The sequence to iterate.</param>
    /// <param name="action">The action to invoke for each item.</param>
    /// <returns>The items as a <see cref="List{T}"/> (the same instance when <paramref name="objects"/> is already a list).</returns>
    /// <remarks>The sequence is enumerated once and materialized, so the action runs eagerly.</remarks>
    public static List<TObject> Each<TObject>(this IEnumerable<TObject> objects, Action<TObject> action)
    {
        var list = objects as List<TObject> ?? objects.ToList();
        list.ForEach(action);
        return list;
    }

    /// <summary>Splits a sequence into consecutive batches of at most <paramref name="size"/> items.</summary>
    /// <typeparam name="TObject">The element type of the sequence.</typeparam>
    /// <param name="objects">The sequence to split.</param>
    /// <param name="size">The maximum number of items per batch.</param>
    /// <returns>A sequence of batches; the final batch may contain fewer than <paramref name="size"/> items.</returns>
    public static IEnumerable<IEnumerable<TObject>> Batch<TObject>(this IEnumerable<TObject> objects, int size)
    {
        return objects
            .Select((item, i) => new { item, i })
            .GroupBy(tuple => tuple.i / size)
            .Select(g => g.Select(tuple => tuple.item));
    }

    /// <summary>Asynchronously enumerates an async sequence into a list.</summary>
    /// <typeparam name="TSource">The element type of the sequence.</typeparam>
    /// <param name="source">The async sequence to materialize.</param>
    /// <returns>A task that resolves to a <see cref="List{T}"/> containing every element of <paramref name="source"/>.</returns>
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
