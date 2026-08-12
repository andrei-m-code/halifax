namespace Halifax.Core.Extensions;

/// <summary>
/// Extension methods for working with <see cref="Guid"/> values, such as substituting a fresh
/// value for an empty or missing one.
/// </summary>
public static class GuidExtensions
{
    /// <summary>
    /// Returns new Guid if passed in Guid is null or empty
    /// </summary>
    /// <param name="guid">Guid in question</param>
    /// <returns>Guid or new Guid</returns>
    public static Guid NewIfEmpty(this Guid? guid)
    {
        return guid == Guid.Empty || !guid.HasValue
            ? Guid.NewGuid()
            : guid.Value;
    }

    /// <summary>
    /// Returns new Guid if passed in Guid is empty
    /// </summary>
    /// <param name="guid">Guid in question</param>
    /// <returns>Guid or new Guid</returns>
    public static Guid NewIfEmpty(this Guid guid)
    {
        return guid == Guid.Empty ? Guid.NewGuid() : guid;
    }    
}