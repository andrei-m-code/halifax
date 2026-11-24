# Responses and errors

## The response envelope

Every endpoint returns `ApiResponse` or `ApiResponse<T>`:

```csharp
return ApiResponse.With(user);   // success with data
return ApiResponse.Empty;        // success, no data
```

```json
{ "data": { "id": 1, "name": "Alice" }, "success": true, "error": null }
```

## Exceptions

Throw a typed exception anywhere; the Halifax exception handler logs it and writes the response.

| Exception | HTTP status |
|---|---|
| `HalifaxException` | 400 Bad Request |
| `HalifaxUnauthorizedException` | 401 Unauthorized |
| `HalifaxNotFoundException` | 404 Not Found |
| any other exception | 500 Internal Server Error |

```csharp
var user = await db.Users.FindAsync(id)
    ?? throw new HalifaxNotFoundException("User not found");
```

The error body:

```json
{
  "success": false,
  "error": {
    "type": "HalifaxNotFoundException",
    "message": "User not found",
    "trace": "   at UsersController.Get(...)"
  }
}
```

Error bodies are written with ASP.NET Core's web JSON defaults (camelCase), not with `ConfigureJson`.

## Stack traces in error responses

By default `error.trace` carries the exception's stack trace, as in every earlier version. A stack trace shows code paths and file names to whoever calls the API, so turn it off for anything public:

```csharp
builder.Services.AddHalifax(h => h.ConfigureErrorTrace(false));
```

The response keeps the same shape, with `"trace": null`, and the full exception is still logged. Clients that read `type` and `message` see no difference. The setting applies process-wide (it sets `ApiResponseError.IncludeTraceByDefault`), so custom exception handlers that build their body with `ApiResponse.With(exception)` follow it too.

## Custom exception handling

To handle exceptions yourself, turn the Halifax handler off and register your own `IExceptionHandler`. `UseHalifax` keeps the exception-handling middleware, so yours takes over:

```csharp
builder.Services.AddHalifax(h => h.ConfigureExceptionHandler(false));
builder.Services.AddExceptionHandler<MyExceptionHandler>();
```

To keep the Halifax mapping and change only the logging, derive from `HalifaxExceptionHandler`, override `LogErrorRequestAsync(HttpContext, Exception)` and register that the same way. The default logs the exception and the request: method, path and query string, the `X-` headers, and up to 5000 characters of the body.

`ApiResponse.With(exception)` and `ApiResponse.With(exception, includeTrace)` build the same error body for your own handlers.

## Pagination

```csharp
[HttpGet]
public async Task<ApiResponse<Paging<UserDto>>> GetUsers([FromQuery] PagingQuery query)
{
    var items = await db.Users
        .OrderBy(u => u.Name)
        .Skip(query.Skip).Take(query.Take)
        .Select(u => new UserDto(u.Id, u.Name))
        .ToListAsync();
    var total = await db.Users.CountAsync();

    return ApiResponse.With(new Paging<UserDto>(items, query.Skip, query.Take, total));
}
```

- `PagingQuery` binds `Skip` (default 0), `Take` (default 10), `OrderBy` and `OrderDirection` (`Asc` or `Desc`) from the query string.
- `PagingMore<T>(items, skip, take, hasMore)` carries a `HasMore` flag instead of a total, for lists that are expensive to count.
- Both have `Map(item => ...)` to convert their items, e.g. from entities to DTOs.
