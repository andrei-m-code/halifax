namespace Halifax.Domain;

public record PagingBase<TObject>
{
    public List<TObject> Items { get; init; } = [];
    public int Skip { get; init; }
    public int Take { get; init; }
}

public record Paging<TObject> : PagingBase<TObject>
{
    public Paging()
    {
    }

    public Paging(List<TObject> items, int skip, int take, int total)
    {
        Items = items;
        Skip = skip;
        Take = take;
        Total = total;
    }

    public int Total { get; init; }

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

public record PagingMore<TObject> : PagingBase<TObject>
{
    public PagingMore()
    {
    }

    public PagingMore(List<TObject> items, int skip, int take, bool hasMore)
    {
        Items = items;
        Skip = skip;
        Take = take;
        HasMore = hasMore;
    }

    public bool HasMore { get; init; }
    
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

public record PagingQuery
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 10;
    public string? OrderBy { get; set; }
    public OrderDirection OrderDirection { get; set; }
}

public enum OrderDirection
{
    Asc, Desc
}
