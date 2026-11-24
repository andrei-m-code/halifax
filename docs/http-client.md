# HTTP client

`Halifax.Http` gives typed clients for service-to-service calls, with JSON, error mapping and optional resilience.

## A typed client

```csharp
public class PaymentClient(HttpClient http) : HalifaxHttpClient(http)
{
    public async Task<PaymentDto> GetAsync(string id)
    {
        var msg = CreateMessage(HttpMethod.Get, $"/api/payments/{id}");
        return await SendAsync<PaymentDto>(msg);
    }

    public async Task<HttpStatusCode> CreateAsync(CreatePaymentRequest request)
    {
        var msg = CreateMessage(HttpMethod.Post, "/api/payments", request);
        return await SendAsync(msg);
    }
}
```

- `CreateMessage(method, url, body)` builds the request; a body is sent as JSON with the app's JSON options.
- `SendAsync<T>` sends, checks the status and deserializes the response.
- `SendAsync` without a type only returns the status code and never throws on an error status.

## Errors

`SendAsync<T>` logs a failed response, body included, and throws:

| Response | Exception |
|---|---|
| 400, 401 or 404 | `Exception`: "Error reading the response of the {client}" |
| any other failure | `Exception`: "Unsuccessful request. {client}. HTTP {code}" |
| 2xx with a body that does not deserialize | `Exception`: "Error reading the response of the {client}" |

The code is meant to turn a Halifax error body on a 400, 401 or 404 back into `HalifaxException`, `HalifaxUnauthorizedException` or `HalifaxNotFoundException`, but `ApiResponse` does not deserialize its `error`, so it never gets there and callers always see `Exception`. That will be fixed in a release that says so, because callers would start getting different exception types. Until then, override `HandleUnsuccessfulResponseAsync` to map errors yourself.

## Registration

```csharp
builder.Services.AddHalifaxHttpClient<PaymentClient>(
    defaultBaseUrl: "https://payments.api.com",
    defaultBearerToken: token);
```

The base URL is required and must be absolute. Other overloads take an `Action<HttpClient>` or `Action<IServiceProvider, HttpClient>` to configure the client, e.g. headers or a timeout.

### Resilience

```csharp
// Standard resilience: retry, circuit breaker and timeout
builder.Services.AddHalifaxHttpClientWithResilience<PaymentClient>(
    defaultBaseUrl: "https://payments.api.com");

// Or take the IHttpClientBuilder and configure it yourself
builder.Services.AddHalifaxHttpClientBuilder<PaymentClient>(
        defaultBaseUrl: "https://payments.api.com",
        defaultBearerToken: null,
        configure: null)
    .AddStandardResilienceHandler(options => options.Retry.MaxRetryAttempts = 3);
```

## Correlation IDs

`UseHalifax` adds `CorrelationIdMiddleware`, which reads the incoming `X-Correlation-Id` header (or generates a GUID when it is missing), sets `HttpContext.TraceIdentifier` to it and adds it to the response.

To pass the ID on to the services you call, attach `CorrelationIdDelegatingHandler`:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services
    .AddHalifaxHttpClientBuilder<PaymentClient>("https://payments.api.com", null, null)
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
```

The header name is `CorrelationIdMiddleware.HeaderName`.
