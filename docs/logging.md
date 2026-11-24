# Logging

## The L logger

`L` is a static Serilog logger that writes to the console. It needs no setup and works anywhere, including static code.

```csharp
L.Info("User {UserId} created", userId);
L.Warning("Timeout after {Seconds} s", seconds);
L.Error(exception, "Failed to process order {OrderId}", orderId);
```

- Methods: `Info`, `Warning`, `Error` (each with an `Exception` overload where it makes sense) and `Fatal`, which writes at the `Error` level.
- Use placeholders: properties without a `{Name}` in the template are not shown.
- `L.Configure(config => ...)` changes the Serilog configuration (sinks, levels, enrichers); `L.Configure(loggerConfiguration)` replaces it.
- Mark a property with `[NotLogged]` to keep it out of destructured log output, e.g. a password on a request model.

The default output template is `[{Timestamp:M/d, HH:mm:ss} {Level:u3}] {Message:lj}` and the exception.

## ASP.NET Core's own logs

`ILogger` logs from the `Microsoft` and `System` categories (hosting, routing, authentication, EF Core and so on) are kept quiet: `AddHalifax` sets their minimum level to `Critical`, as in every earlier version.

To see them, for example while diagnosing startup, routing or authentication problems:

```csharp
builder.Services.AddHalifax(h => h.ConfigureFrameworkLogging(LogLevel.Warning));
```

`LogLevel.Information` shows every request. Outside `AddHalifax`, `services.CleanupDefaultLogging(level)` does the same.
