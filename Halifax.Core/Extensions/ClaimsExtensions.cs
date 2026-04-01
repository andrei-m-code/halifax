using System.Security.Claims;
using Halifax.Domain.Exceptions;

namespace Halifax.Core.Extensions;

/// <summary>
/// Fluent extension methods for validating and extracting values from JWT claims.
/// Methods are chainable and throw <see cref="HalifaxUnauthorizedException"/> on validation failure by default.
/// </summary>
public static class ClaimsExtensions
{
    private static readonly Action<Claim?> claimDefaultValueConditionFailed = claim
        => throw new HalifaxUnauthorizedException("Unauthorized");

    /// <summary>Validates that a claim value is not null or whitespace.</summary>
    public static IEnumerable<Claim> ClaimNotNullOrWhiteSpace(
        this IEnumerable<Claim> claims,
        string claimType,
        out string? value,
        Action<Claim?>? valueConditionFailed = null) =>
        claims.ClaimValidate(claimType, out value, v => !string.IsNullOrWhiteSpace(v), valueConditionFailed);

    /// <summary>Validates that a claim value is a valid email address.</summary>
    public static IEnumerable<Claim> ClaimIsEmail(
        this IEnumerable<Claim> claims,
        string claimType,
        out string? email,
        Action<Claim?>? valueConditionFailed = null)
        => claims.ClaimValidate(claimType, out email, v => v.IsEmail(), valueConditionFailed);

    /// <summary>Validates that a claim value is a valid integer.</summary>
    public static IEnumerable<Claim> ClaimIsInt(
        this IEnumerable<Claim> claims,
        string claimType,
        out int parsedValue,
        Action<Claim?>? valueConditionFailed = null)
    {
        int parsed = default;
        claims.ClaimValidate(claimType, out _, v => int.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }

    /// <summary>Validates that a claim value is a valid double.</summary>
    public static IEnumerable<Claim> ClaimIsDouble(
        this IEnumerable<Claim> claims,
        string claimType,
        out double parsedValue,
        Action<Claim?>? valueConditionFailed = null)
    {
        double parsed = default;
        claims.ClaimValidate(claimType, out _, v => double.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }

    /// <summary>Validates that a claim value is a valid enum of type <typeparamref name="TEnum"/>.</summary>
    public static IEnumerable<Claim> ClaimIsEnum<TEnum>(
        this IEnumerable<Claim> claims,
        string claimType,
        out TEnum parsedValue,
        Action<Claim?>? valueConditionFailed = null) where TEnum : struct
    {
        TEnum parsed = default;
        claims.ClaimValidate(claimType, out _, v => Enum.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }

    /// <summary>Validates that a claim value is a valid GUID.</summary>
    public static IEnumerable<Claim> ClaimIsGuid(
        this IEnumerable<Claim> claims,
        string claimType,
        out Guid parsedValue,
        Action<Claim?>? valueConditionFailed = null)
    {
        Guid parsed = default;
        claims.ClaimValidate(claimType, out _, v => Guid.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }

    /// <summary>Validates that a claim value is a valid boolean.</summary>
    public static IEnumerable<Claim> ClaimIsBoolean(
        this IEnumerable<Claim> claims,
        string claimType,
        out bool parsedValue,
        Action<Claim?>? valueConditionFailed = null)
    {
        bool parsed = default;
        claims.ClaimValidate(claimType, out _, v => bool.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }

    /// <summary>Validates that a claim value can be parsed to type <typeparamref name="T"/>.</summary>
    public static IEnumerable<Claim> ClaimIs<T>(
        this IEnumerable<Claim> claims,
        string claimType,
        out T? parsedValue,
        Action<Claim?>? valueConditionFailed = null) where T : IParsable<T>
    {
        T? parsed = default;
        claims.ClaimValidate(claimType, out _, v => v != null && T.TryParse(v, null, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }

    /// <summary>Validates a claim value against a custom predicate.</summary>
    public static IEnumerable<Claim> ClaimValidate(
        this IEnumerable<Claim> claims,
        string claimType,
        out string? value,
        Predicate<string?> valueCondition,
        Action<Claim?>? valueConditionFailed = null)
    {
        var claim = claims?.FirstOrDefault(c => c.Type == claimType);
        value = claim?.Value;

        if (!valueCondition(value))
        {
            (valueConditionFailed ?? claimDefaultValueConditionFailed).Invoke(claim);
        }

        return claims!;
    }

    /// <summary>Validates that a claim value equals an expected value.</summary>
    public static IEnumerable<Claim> ClaimExpected(
        this IEnumerable<Claim> claims,
        string claimType,
        object expectedClaimValue,
        Action<Claim?>? valueConditionFailed = null)
    {
        return claims.ClaimValidate(claimType, out _, v => v == expectedClaimValue.ToString(), valueConditionFailed);
    }
}
