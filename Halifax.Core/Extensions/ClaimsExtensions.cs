using System.Security.Claims;
using Halifax.Domain.Exceptions;

namespace Halifax.Core.Extensions;

public static class ClaimsExtensions
{
    private static readonly Action<Claim> claimDefaultValueConditionFailed = claim 
        => throw new HalifaxUnauthorizedException("Unauthorized");
    
    public static List<Claim> ClaimNotNullOrWhiteSpace(
        this List<Claim> claims, 
        string claimType, 
        out string value,
        Action<Claim> valueConditionFailed = null) =>
        claims.ClaimValidate(claimType, out value, v => !string.IsNullOrWhiteSpace(v), valueConditionFailed);

    public static List<Claim> ClaimIsEmail(
        this List<Claim> claims,
        string claimType,
        out string email,
        Action<Claim> valueConditionFailed = null)
        => claims.ClaimValidate(claimType, out email, v => v.IsEmail(), valueConditionFailed);

    public static List<Claim> ClaimIsInt(
        this List<Claim> claims,
        string claimType,
        out int parsedValue,
        Action<Claim> valueConditionFailed = null)
    {
        int parsed = default;
        claims.ClaimValidate(claimType, out _, v => int.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }    
    
    public static List<Claim> ClaimIsDouble(
        this List<Claim> claims,
        string claimType,
        out double parsedValue,
        Action<Claim> valueConditionFailed = null)
    {
        double parsed = default;
        claims.ClaimValidate(claimType, out _, v => double.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    } 
    
    public static List<Claim> ClaimIsGuid(
        this List<Claim> claims,
        string claimType,
        out Guid parsedValue,
        Action<Claim> valueConditionFailed = null)
    {
        Guid parsed = default;
        claims.ClaimValidate(claimType, out _, v => Guid.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }
    
    public static List<Claim> ClaimIsBoolean(
        this List<Claim> claims,
        string claimType,
        out bool parsedValue,
        Action<Claim> valueConditionFailed = null)
    {
        bool parsed = default;
        claims.ClaimValidate(claimType, out _, v => bool.TryParse(v, out parsed), valueConditionFailed);
        parsedValue = parsed;
        return claims;
    }    

    public static List<Claim> ClaimValidate(
        this List<Claim> claims, 
        string claimType,
        out string value,
        Predicate<string> valueCondition,
        Action<Claim> valueConditionFailed = null)
    {
        var claim = claims?.FirstOrDefault(c => c.Type == claimType);
        value = claim?.Value;
        
        if (!valueCondition(value))
        {
            (valueConditionFailed ?? claimDefaultValueConditionFailed).Invoke(claim);
        }
        
        return claims;
    }
}