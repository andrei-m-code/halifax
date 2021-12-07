namespace Halifax.Domain;

public record Paging<TObject>
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

    public List<TObject> Items { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; }
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

public record PagingQuery
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 10;
    public string OrderBy { get; set; }
    public OrderDirection OrderDirection { get; set; }
}

public enum OrderDirection
{
    Asc, Desc
}
