# Configuration

Halifax reads settings from environment variables into your own types, and loads a `.env` file for local runs.

## The .env file

`AddHalifax` calls `Env.Load()`, which looks for `.env` in the current directory and then each parent directory, and loads the first one it finds into the process environment.

```dotenv
# comments and blank lines are ignored
AppSettings__ConnectionString=Server=localhost;Database=orders
AppSettings__HttpTimeout=120
```

- A missing file is ignored. `Env.Load(".env.local", swallowErrors: false)` loads another file and throws if it is missing or malformed.
- Values from the file override variables that are already set.
- Values are taken as written: quotes are not removed.

In containers, set the variables in the deployment instead; no file is needed.

## Settings types

```csharp
record AppSettings(string ConnectionString, int HttpTimeout, bool UseCache = false);

builder.Services.AddHalifax(h => h.AddSettings<AppSettings>());
```

- `AddSettings<T>()` binds `T` and registers it as a singleton for injection.
- `AddSettings<T>(out var settings)` also hands the instance back, for use during startup.
- `Env.GetSection<T>()` binds without dependency injection. `Env.GetSection<T>("Orders")` reads `Orders__*` instead of `{TypeName}__*`.

Each member is read from a variable named `{TypeName}__{Member}` (`AppSettings__HttpTimeout`). The constructor with the most parameters is used, then any remaining settable properties are bound. Results are cached per type.

| Situation | Result |
|---|---|
| Variable missing | Constructor parameters get their default value (or `default`); properties keep their initial value |
| Variable empty, member not a `string` | Same as missing |
| Value does not parse | `FormatException` naming the variable (the value is left out, since settings often hold secrets) |
| Constructor parameter of an unsupported type, variable set | `NotSupportedException` |
| Property of an unsupported type | Skipped |

## Supported types

`string`, `bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, enums, and the nullable version of each.

- `bool` accepts `true`/`false` in any case, and `1`/`0`.
- Enums accept a member name in any case, or its number.
- Numbers and dates are parsed with the invariant culture: `1.5`, `2026-11-24`, `11/24/2026`, `00:02:00`.

## Changed in 6.2.0

Earlier versions supported fewer types and were inconsistent between constructor parameters and properties. Nothing that worked before reads differently now:

- `bool`, `char`, `sbyte`, `ushort`, `uint`, `ulong`, enums, `DateTimeOffset`, `DateOnly` and `TimeOnly` used to throw `NotSupportedException` when their variable was set; now they bind.
- Settable properties of type `decimal` or a nullable primitive (`int?`, `bool?`, ...) used to be skipped silently; now they bind.
- Parsing used the current culture. Containers run with the invariant culture, so their values parse exactly as before.
- An empty value for a non-string property used to throw; now the property keeps its initial value.
