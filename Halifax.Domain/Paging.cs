namespace Halifax.Domain;

/// <summary>
/// Base record for paginated results.
/// </summary>
/// <typeparam name="TObject">The type of items in the page.</typeparam>
public record PagingBase<TObject>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public List<TObject> Items { get; init; } = [];

    /// <summary>
    /// Number of items skipped before this page.
    /// </summary>
    public int Skip { get; init; }

    /// <summary>
    /// Number of items requested for this page.
    /// </summary>
    public int Take { get; init; }
}

/// <summary>
/// Paginated result with a known total count.
/// </summary>
/// <typeparam name="TObject">The type of items in the page.</typeparam>
public record Paging<TObject> : PagingBase<TObject>
{
    /// <summary>
    /// Default constructor for deserialization.
    /// </summary>
    public Paging()
    {
    }

    /// <summary>
    /// Creates a paging result with the specified items and pagination metadata.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="skip">Number of items skipped.</param>
    /// <param name="take">Number of items requested.</param>
    /// <param name="total">Total number of items across all pages.</param>
    public Paging(List<TObject> items, int skip, int take, int total)
    {
        Items = items;
        Skip = skip;
        Take = take;
        Total = total;
    }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Projects items to a different type while preserving pagination metadata.
    /// </summary>
    /// <typeparam name="TDestination">The target type.</typeparam>
    /// <param name="map">The mapping function.</param>
    public Paging<TDestination> Map<TDestination>(Func<TObject, TDestination> map)
    {
        return new Paging<TDestination>
        {
            Items = Items.Select(map).ToList(),
            Total = Total,
            Skip = Skip,
            Take = Take
        };
    }
}

/// <summary>
/// Paginated result with a flag indicating whether more items exist.
/// </summary>
/// <typeparam name="TObject">The type of items in the page.</typeparam>
public record PagingMore<TObject> : PagingBase<TObject>
{
    /// <summary>
    /// Default constructor for deserialization.
    /// </summary>
    public PagingMore()
    {
    }

    /// <summary>
    /// Creates a paging result with the specified items and pagination metadata.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="skip">Number of items skipped.</param>
    /// <param name="take">Number of items requested.</param>
    /// <param name="hasMore">Whether more items exist beyond this page.</param>
    public PagingMore(List<TObject> items, int skip, int take, bool hasMore)
    {
        Items = items;
        Skip = skip;
        Take = take;
        HasMore = hasMore;
    }

    /// <summary>
    /// Whether more items exist beyond this page.
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// Projects items to a different type while preserving pagination metadata.
    /// </summary>
    /// <typeparam name="TDestination">The target type.</typeparam>
    /// <param name="map">The mapping function.</param>
    public PagingMore<TDestination> Map<TDestination>(Func<TObject, TDestination> map)
    {
        return new PagingMore<TDestination>
        {
            Items = Items.Select(map).ToList(),
            HasMore = HasMore,
            Skip = Skip,
            Take = Take
        };
    }
}

/// <summary>
/// Query parameters for paginated requests.
/// </summary>
public record PagingQuery
{
    /// <summary>
    /// Number of items to skip. Default is 0.
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of items to return. Default is 10.
    /// </summary>
    public int Take { get; set; } = 10;

    /// <summary>
    /// Property name to order results by.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Sort direction for ordering.
    /// </summary>
    public OrderDirection OrderDirection { get; set; }
}

/// <summary>
/// Sort direction for ordered queries.
/// </summary>
public enum OrderDirection
{
    /// <summary>Ascending order.</summary>
    Asc,
    /// <summary>Descending order.</summary>
    Desc
}
