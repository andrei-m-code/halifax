# Halifax Service Foundation

A set of .NET NuGet libraries that eliminate boilerplate in ASP.NET Core API services
(standardized responses, JWT auth, config, logging, HTTP client, Excel/CSV). Published to
NuGet under the `Halifax.*` package IDs. Repo: https://github.com/andrei-m-code/halifax

## Projects

Packable libraries (each produces a NuGet package; `GeneratePackageOnBuild=true`):

| Project | Package | Notes |
|---|---|---|
| Halifax.Domain | Halifax.Domain | Response models, exceptions, pagination. No external deps. |
| Halifax.Core | Halifax.Core | JWT, config, validation, crypto, logging, JSON. Depends on Domain. |
| Halifax.Api | Halifax.Api | ASP.NET Core integration, middleware, Swagger/Scalar. Depends on Core. |
| Halifax.Http | Halifax.Http | Typed HttpClient with resilience. Depends on Core. |
| Halifax.Excel | Halifax.Excel | Excel/CSV import/export (NPOI only; CSV is built in). Standalone. |

Non-packable:

- `Halifax.Core.Tests`, `Halifax.Excel.Tests`, `Halifax.Api.Tests` — NUnit 5 test projects
  (`IsPackable=false`). `Halifax.Api.Tests` boots `AddHalifax`/`UseHalifax` on an in-memory
  `TestServer` (OpenAPI, Scalar, JWT, exception mapping, correlation ID); Halifax allows one
  app per process, so its tests share one app from `[OneTimeSetUp]`.
- `PeggysCove.Api` — sample/demo app that consumes the libraries via `ProjectReference`
  (not published, no version).

Dependency graph: `Domain ← Core ← {Api, Http}`; `Excel` standalone. `Api` and `Http`
use `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

## Key conventions

- **Backward compatibility comes first.** Production services with thousands of customers run on Halifax
  (the truckmore APIs among them). In a minor or patch release, anything a running service would notice
  keeps its old behavior as the default: response shapes, stored or exchanged formats (encrypted values,
  tokens), settings values, logging. New behavior is opt-in through a builder method or setting, and old
  stored data must keep working forever (decrypt old ciphertexts, validate old tokens). Prove it with golden
  values produced by the previous release (`CryptoCompatibilityTests`, `JwtCompatibilityTests`), and before
  releasing, build and test the dependents against a locally packed version.
- **Docs: README.md is the overview, `docs/` has one page per area** (setup, errors, configuration,
  authentication, http-client, logging, excel, utilities). README.md is packed into every NuGet package,
  so its links to `docs/` are absolute GitHub URLs. Keep both in step with the code.
- **Target framework:** `net11.0` across every project. Single target, no multi-targeting:
  each Halifax major follows one .NET major (see "Versioning & releases").
- **SDK:** `global.json` sets SDK `11.0.100` as the minimum with `rollForward: latestMajor`
  and `allowPrerelease: true`, so any newer installed SDK is used. CI and publish use
  `setup-dotnet` `11.x`.
- **No central package management.** There is no `Directory.Packages.props`; every
  version lives in the individual `.csproj` `<PackageReference>` elements.
- **Internal packages are referenced as published NuGet packages, NOT ProjectReferences.**
  `Halifax.Api`/`Halifax.Http` reference `Halifax.Core` via `<PackageReference ... Version="x.y.z" />`.
  This is why CI packs Domain + Core into a **local NuGet feed** before building the solution
  (see below). The old `ProjectReference` lines are left commented out in the csproj.
- **Nullable reference types enabled** everywhere (`<Nullable>enable</Nullable>`).
- **XML docs are generated and packaged.** Every packable project sets
  `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, so the `Halifax.*.xml`
  doc file ships inside each NuGet package (consumer IntelliSense). This makes `CS1591`
  (missing XML comment on a public member) an active warning — the repo is kept at
  **0 CS1591**, so document every new public type/member or the build warns.
- **Source Link + symbol packages.** Every packable project references
  `Microsoft.SourceLink.GitHub` (`PrivateAssets="All"`, build-time only, never a package
  dependency) and sets `PublishRepositoryUrl`, `EmbedUntrackedSources`, `IncludeSymbols`,
  `SymbolPackageFormat=snupkg` and `Deterministic`, so each `.nupkg` ships with a `.snupkg`
  that lets consumers step into the real source. `publish.yml` builds with
  `-p:ContinuousIntegrationBuild=true`; `dotnet nuget push out/*.nupkg` pushes the matching
  `.snupkg` automatically. Bump Source Link with the other dependencies.
- **CSV is built in (6.2.0): `CsvRecords.cs`, no CsvHelper.** A streaming async reader and a writer that
  reproduce exactly what Halifax.Excel 6.1.0 did through CsvHelper 34: separator from
  `CultureInfo.TextInfo.ListSeparator`, quoting when a field has the separator, a quote, a line break or
  leading/trailing whitespace, `\r\n` line ends, blank lines skipped, a short row throws
  `IndexOutOfRangeException`. `CsvCompatibilityTests` records 6.1.0's output for every edge case (quotes,
  line endings, BOM, buffer boundaries) and `ExcelConverterCsvFormatTests` pins the exact output: change the
  parser only with those passing.
- **Halifax.Excel must never need SkiaSharp** (kept lean on purpose, 6.1.0). NPOI 2.8 types number-format
  colours as SkiaSharp's `SKColor`, so anything that runs NPOI's `DataFormatter` (`FormatCellValue`,
  `FormatRawCellContents`, `ToString()` on .xls cells) loads `SkiaSharp.dll`, which NPOI ships without runtime
  assets: consumers then crash with `FileNotFoundException`. The reader works from raw cell values in
  `ExcelCell` (its own number/date text, no `DataFormatter`); that is also why ExcelMapper was dropped (it calls
  `DataFormatter` for numbers read into strings). `AutoSizeColumn` and pictures need the native Skia library
  too: don't use them. `ExcelSkiaSharpGuardTests` fails if SkiaSharp is shipped or loaded; never add SkiaSharp
  to make a test pass. NPOI still copies the native `libSkiaSharp` for every platform (useless without the
  managed DLL), so the NPOI reference has `ExcludeAssets="native"` to keep it out of consumers.
- **Writes never close the caller's stream.** Excel and CSV writes leave it open and rewind a seekable stream
  to where the output starts; CSV reads leave it open too.
- **Pre-existing build warning** (safe to ignore): `CS8603` in `Halifax.Http/HalifaxHttpClient.cs`
  (the `SendAsync<TModel>` return; line number drifts as docs change). Expect **1 warning** on a clean
  solution build. The `SYSLIB0060` warnings went away in 6.2.0, when the legacy crypto moved to
  `Rfc2898DeriveBytes.Pbkdf2` with byte-identical output.

## Versioning & releases

- Each packable project sets its own `<AssemblyVersion>`, `<FileVersion>`, `<Version>`
  (all three kept identical) plus a `<PackageReleaseNotes>` changelog block.
- **Git tags / GitHub releases track the `Halifax.Api` version.** e.g. release `v5.3.0`
  corresponds to `Halifax.Api` 5.3.0. Other packages have their own independent versions
  (Core 5.2.0, Http 5.2.0, Excel 0.4.0 at that point).
- **Pure dependency-update releases have historically been PATCH bumps** (e.g. "v5.1.3 -
  Package dependency updates"). On 2026-07-13 the user chose a **minor** bump instead — so
  confirm bump type with the user rather than assuming.
- **A new .NET major means a new Halifax major for every package**, Excel included
  (Halifax 6.x = .NET 11, 5.x = .NET 10, 4.x = .NET 9...). The target moves to the new .NET
  only, no multi-targeting; older lines stay on NuGet without updates. The user adopts new
  .NET majors at release (6.0.0 shipped with .NET 11 GA, 2026-11). Keep the README
  "Compatibility" table in step when a new major ships.
- **Halifax.Excel left 0.x at 6.0.0** and now shares the major version with the other
  packages (minor/patch still independent).
- **A release that changes only Halifax.Excel is tagged with the Excel version** (`v6.1.0` shipped Excel
  6.1.0 while Api stayed 6.0.0). `--skip-duplicate` skips the unchanged packages. The next Api release then
  picks a version above the latest tag, which is why Domain/Core/Api/Http went from 6.0.0 to 6.2.0.

### Cutting a release (this triggers NuGet publish)

`publish.yml` runs on **GitHub release published** (not on tag push). To release:

```bash
gh release create vX.Y.Z --target main \
  --title "vX.Y.Z — <summary>" --notes "<release notes>"
```

Use the new **Halifax.Api** version for `X.Y.Z`. The workflow packs Domain + Core into a
local feed, **runs the full test suite (`dotnet test Halifax.sln`) and aborts before
pushing if any test fails**, then packs the remaining packages in dependency order and
pushes to NuGet with `--skip-duplicate` (so unchanged packages that keep their version are
simply skipped). Watch it with:

```bash
gh run list --workflow=publish.yml -L 1
gh run view <run-id>
```

**Trusted publishing, no API key.** The push authenticates with nuget.org trusted
publishing: `NuGet/login@v1` trades the job's GitHub OIDC token (`id-token: write`) for a
one-hour API key, so the repo keeps no `NUGET_API_KEY` secret. The nuget.org policy belongs
to `andrei.mikhalevich`, covers the five `Halifax.*` packages and is bound to `publish.yml`:
renaming the workflow file breaks publishing until the policy is edited to match.

NuGet indexing after a successful push takes a few minutes.

## CI

`ci.yml` runs on push/PR to `main`. It creates a local NuGet source, packs `Halifax.Domain`
then `Halifax.Core` into it, then builds and tests the whole solution. **The repo commits
directly to `main`** — CI and history are all direct-to-main.

## How to upgrade packages (the 2026-07-13 workflow)

1. Read every `.csproj`, collect external `<PackageReference>` versions.
2. Query latest **stable** versions from NuGet, e.g.:
   ```bash
   curl -s https://api.nuget.org/v3-flatcontainer/<pkg-lowercase>/index.json \
     | python3 -c "import sys,json;d=json.load(sys.stdin);vs=[x for x in d['versions'] if '-' not in x];print(vs[-1])"
   ```
3. Bump only the ones behind (leave internal `Halifax.*` refs alone at this stage).
4. `dotnet restore` — **watch for `NU1903` vulnerability warnings** on transitive deps.
   On this repo NPOI pulled in a vulnerable `System.Security.Cryptography.Xml`; the fix is to
   add an explicit top-level `<PackageReference>` pinning the patched version (match the
   target framework: net11 → 11.0.x) in `Halifax.Excel`.
5. Build + test: `dotnet build -c Release && dotnet test --no-build -c Release`
   (277 tests: 92 Core, 170 Excel, 15 Api).

### Validating internal-package version bumps locally

Bumping `Halifax.Core`'s version breaks a plain `dotnet build` with `NU1102` because
`Api`/`Http` reference the not-yet-published new Core version. Replicate CI's local feed to
validate before pushing:

```bash
rm -rf local-packages && mkdir -p local-packages
dotnet nuget add source $(pwd)/local-packages --name halifax-local
dotnet pack Halifax.Domain/Halifax.Domain.csproj -c Release -o local-packages
dotnet pack Halifax.Core/Halifax.Core.csproj  -c Release -o local-packages
dotnet build Halifax.sln -c Release          # now resolves the new Core version
dotnet test  Halifax.sln -c Release --no-build
# cleanup so nothing is committed:
dotnet nuget remove source halifax-local && rm -rf local-packages
```

When you bump `Halifax.Core`'s version, also update the `<PackageReference Include="Halifax.Core" Version="..." />`
in **both** `Halifax.Api` and `Halifax.Http`.

## Preferences (this repo)

- Commit style: imperative subject, sentence case, no prefixes/scopes, no ticket IDs
  (e.g. "Bump package versions for dependency updates"). No AI attribution anywhere.
- Never publish to NuGet / create releases without an explicit ask — it's outward-facing.
