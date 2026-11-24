# Authentication

Halifax uses JWT bearer tokens signed with a shared secret (HMAC-SHA256).

## Turning it on

```csharp
builder.Services.AddHalifax(h => h
    .ConfigureAuthentication(jwtSecret,
        validateAudience: false,
        validateIssuer: false,
        requireExpirationTime: false));
```

Endpoints marked `[Authorize]`, on the action or the controller, then require `Authorization: Bearer {token}`; a missing or invalid token gives 401. Endpoints without `[Authorize]` stay open.

For full control, pass your own `TokenValidationParameters` to `ConfigureAuthentication` instead. `Jwt.GetSigningKeys(secret)` gives the keys to put in `IssuerSigningKeys`.

### The secret

- Use at least 32 characters: HS256 needs a 256-bit key, and `Jwt.Create` rejects anything under 16.
- Prefer an ASCII secret: `Jwt.Create` replaces each non-ASCII character with `?`, which weakens the key (see below).
- Use the same secret in every service that issues or validates tokens.

## Creating and reading tokens

```csharp
var claims = new List<Claim>
{
    new("sub", user.Id.ToString()),
    new("email", user.Email),
    new("role", user.Role)
};

string token = Jwt.Create(jwtSecret, claims, DateTime.UtcNow.AddDays(30));

List<Claim> read = Jwt.Read(jwtSecret, token);
```

`Jwt.Read(secret, token, throwUnauthorized, validateAudience, validateIssuer, validateLifetime)`:

- returns the token's claims;
- throws `HalifaxUnauthorizedException` for an invalid token, or returns an empty list with `throwUnauthorized: false`;
- does not check expiry unless you pass `validateLifetime: true`. The middleware does check it.

## Non-ASCII secrets (6.2.0)

`Jwt.Create` and `Jwt.Read` turn the secret into bytes as ASCII, so every non-ASCII character becomes `?`, while the middleware (and most other JWT code) uses UTF-8. For an ASCII secret both give the same bytes. For a non-ASCII secret, tokens from `Jwt.Create` never passed the middleware up to 6.0.0.

From 6.2.0 validation accepts both, without changing how tokens are signed:

- `Jwt.Create` still signs with the ASCII bytes, so every existing validator, including other services on older Halifax versions or with their own ASCII-based validation, keeps accepting new tokens;
- `Jwt.Read` and the middleware accept tokens signed with either the ASCII or the UTF-8 bytes (`Jwt.GetSigningKeys`).

Nothing changes for an ASCII secret.

## Claims-based authorization

Derive from `ClaimsAuthorizeFilterAttribute` for checks on the token's claims. A failed check returns 401.

```csharp
class AdminOnly : ClaimsAuthorizeFilterAttribute
{
    protected override bool IsAuthorized(ActionExecutingContext context, List<Claim> claims)
    {
        claims.ClaimExpected("role", "admin");
        return true;
    }
}

[HttpGet("admin")]
[AdminOnly]
public ApiResponse<string> GetAdminData() => ApiResponse.With("secret");
```

Claim checks:

- `ClaimExpected(type, expectedValue)`
- `ClaimNotNullOrWhiteSpace`, `ClaimIsEmail`, `ClaimIsInt`, `ClaimIsDouble`, `ClaimIsEnum<T>`, `ClaimIsGuid`, `ClaimIsBoolean`, `ClaimIs<T>` (any `IParsable<T>`) and `ClaimValidate` (your own predicate), which hand back the value through `out`

A failed check throws `HalifaxUnauthorizedException` (401), unless you pass your own `valueConditionFailed` action. Each returns the claims, so checks chain:

```csharp
claims.ClaimIsGuid("sub", out var userId)
      .ClaimExpected("role", "admin");
```
