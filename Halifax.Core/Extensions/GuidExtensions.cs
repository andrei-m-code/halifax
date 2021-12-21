namespace Halifax.Core.Extensions;

public static class GuidExtensions
{
    public static Guid NewIfEmpty(this Guid? guid)
    {
        return guid == Guid.Empty || !guid.HasValue
            ? Guid.NewGuid()
            : guid.Value;
    }

    public static Guid NewIfEmpty(this Guid guid)
    {
        return guid == Guid.Empty ? Guid.NewGuid() : guid;
    }    
}