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
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="value">The matched claim value, or <see langword="null"/> when the claim is absent.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value is null or whitespace.</exception>
    public static IEnumerable<Claim> ClaimNotNullOrWhiteSpace(
        this IEnumerable<Claim> claims,
        string claimType,
        out string? value,
        Action<Claim?>? valueConditionFailed = null) =>
        claims.ClaimValidate(claimType, out value, v => !string.IsNullOrWhiteSpace(v), valueConditionFailed);

    /// <summary>Validates that a claim value is a valid email address.</summary>
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="email">The matched claim value, or <see langword="null"/> when the claim is absent.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value is not a valid email.</exception>
    public static IEnumerable<Claim> ClaimIsEmail(
        this IEnumerable<Claim> claims,
        string claimType,
        out string? email,
        Action<Claim?>? valueConditionFailed = null)
        => claims.ClaimValidate(claimType, out email, v => v.IsEmail(), valueConditionFailed);

    /// <summary>Validates that a claim value is a valid integer.</summary>
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="parsedValue">The parsed integer, or <c>0</c> when parsing fails.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value is not a valid integer.</exception>
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
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="parsedValue">The parsed double, or <c>0</c> when parsing fails.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value is not a valid double.</exception>
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
    /// <typeparam name="TEnum">The enum type to parse the claim value into.</typeparam>
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="parsedValue">The parsed enum value, or its default when parsing fails.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value is not a valid <typeparamref name="TEnum"/>.</exception>
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
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="parsedValue">The parsed GUID, or <see cref="Guid.Empty"/> when parsing fails.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value is not a valid GUID.</exception>
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
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="parsedValue">The parsed boolean, or <see langword="false"/> when parsing fails.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value is not a valid boolean.</exception>
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
    /// <typeparam name="T">A type implementing <see cref="IParsable{TSelf}"/> to parse the claim value into.</typeparam>
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="parsedValue">The parsed value, or the default of <typeparamref name="T"/> when parsing fails.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value cannot be parsed to <typeparamref name="T"/>.</exception>
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

    /// <summary>Validates a claim value against a custom predicate. This is the primitive the other <c>Claim*</c> methods build on.</summary>
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="value">The matched claim value, or <see langword="null"/> when the claim is absent.</param>
    /// <param name="valueCondition">The predicate the value must satisfy.</param>
    /// <param name="valueConditionFailed">Action invoked (with the matched claim, which may be <see langword="null"/>) when the predicate fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when <paramref name="valueCondition"/> returns <see langword="false"/>.</exception>
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

    /// <summary>Validates that a claim value equals an expected value (compared as strings).</summary>
    /// <param name="claims">The claims to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <param name="expectedClaimValue">The expected value; its <see cref="object.ToString"/> result is compared against the claim value.</param>
    /// <param name="valueConditionFailed">Action invoked when validation fails; defaults to throwing <see cref="HalifaxUnauthorizedException"/>.</param>
    /// <returns>The original <paramref name="claims"/> sequence to allow chaining.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown by the default failure action when the value does not equal <paramref name="expectedClaimValue"/>.</exception>
    public static IEnumerable<Claim> ClaimExpected(
        this IEnumerable<Claim> claims,
        string claimType,
        object expectedClaimValue,
        Action<Claim?>? valueConditionFailed = null)
    {
        return claims.ClaimValidate(claimType, out _, v => v == expectedClaimValue.ToString(), valueConditionFailed);
    }
}
