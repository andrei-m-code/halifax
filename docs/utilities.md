# Utilities

## Encryption

`Crypto` encrypts strings with a shared secret.

```csharp
var encrypted = Crypto.Encrypt(secret, "sensitive data");
var decrypted = Crypto.Decrypt(secret, encrypted);
Crypto.TryDecrypt(secret, encrypted, out var result);
```

### Formats

| | `CryptoFormat.Legacy` | `CryptoFormat.AesGcm` |
|---|---|---|
| Written by | every Halifax version; `Encrypt(secret, text)` | Halifax.Core 6.2.0 and later; `Encrypt(secret, text, CryptoFormat.AesGcm)` |
| Readable by | every Halifax version | Halifax.Core 6.2.0 and later |
| Cipher | AES-256-CBC | AES-256-GCM |
| Key | PBKDF2-SHA1, 1000 iterations, fixed salt; IV from the same derivation | PBKDF2-SHA256, 600,000 iterations, derived once per secret and cached |
| Same text, same value? | yes | no: a random nonce every time |
| Detects a changed value? | no | yes |
| Output | Base64 of UTF-16 text | `v2.` followed by URL-safe Base64 of UTF-8 text: up to about 25 characters longer for short text, shorter beyond about 20 characters |

`Decrypt` and `TryDecrypt` read both formats, and `Crypto.GetFormat(value)` tells them apart. Values stored by any Halifax version keep decrypting.

### Using AES-GCM

`Encrypt(secret, text)` always writes `Legacy`, exactly as earlier versions did, so upgrading Halifax changes nothing that is stored or compared. AES-GCM is chosen per call, never by a global switch:

```csharp
var sealedValue = Crypto.Encrypt(secret, apiKey, CryptoFormat.AesGcm);
var apiKeyAgain = Crypto.Decrypt(secret, sealedValue);   // reads either format
```

Use it only for values that are decrypted and never compared:

1. Every service that decrypts the value must run Halifax.Core 6.2.0 or later; older versions cannot read AES-GCM values.
2. Never use it where code encrypts a value and compares the result with a stored one, for example checking a password by encrypting it: every AES-GCM call gives a different result, so the comparison would always fail.
3. Lengths change: short values get up to about 25 characters longer (long ones get shorter); check column length limits.
4. To move stored values over: read, `Decrypt`, `Encrypt(..., CryptoFormat.AesGcm)`, write back. `GetFormat` finds the ones left.

For passwords, store a password hash (for example PBKDF2 with a per-user salt, or ASP.NET Core Identity's `PasswordHasher`) rather than an encrypted value.

## Short IDs

```csharp
var id = ShortId.Create();                         // 7 characters, letters and digits
var longer = ShortId.Create(length: 12);           // the minimum length is 7
var letters = ShortId.Create(useNumbers: false);   // letters only
```

Characters come from a cryptographic random generator (from 6.2.0), so IDs cannot be predicted and the method is safe to call from many threads. The alphabet has no `i` and no `Z`. Short IDs are random, not guaranteed unique: use a `Guid` when uniqueness matters.

## JSON

`Json` uses the same options as the API: camelCase names, case-insensitive reads, enums as strings and UTC dates.

```csharp
var json = Json.Serialize(obj);
var indented = Json.Serialize(obj, indented: true);
var obj = Json.Deserialize<MyType>(json);
Json.TryDeserialize<MyType>(json, out var result);
var fromStream = await Json.DeserializeAsync<MyType>(stream);
```

Each method also has an overload that takes your own `JsonSerializerOptions`. `ConfigureJson` on the builder changes the options for the whole app.

## Dates

```csharp
DateTimeHelper.ValidateRange(from, to);    // throws if from > to
DateTimeHelper.IsIn(from, to, pointInTime);

DateTime? date = DateTime.UtcNow;
date.ToIsoFormat();                        // "2026-11-24T10:30:00Z"
```

`ToIsoFormat` works on `DateTime?` and formats the value as it is, without converting it to UTC.

## Extensions

- Strings: `IsEmail`, `IsUrl`, `CapitalizeWords`, `ParseConnectionString`.
- `Guid`: `NewIfEmpty`.
- Collections: `Each`, `Batch`.
