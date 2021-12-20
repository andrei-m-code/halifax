namespace Halifax.Core.Extensions;

public static class GuidExtensions
{
    public static Guid NewIfEmpty(this Guid? guid)
    {
        if (!guid.HasValue || guid == Guid.Empty)
        {
            return Guid.NewGuid();
        }

        return guid.Value;
    }

    public static Guid NewIfEmpty(this Guid guid)
    {
        return NewIfEmpty(guid);
    }    
}