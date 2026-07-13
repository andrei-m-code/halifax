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
| Halifax.Excel | Halifax.Excel | Excel/CSV import/export (NPOI, CsvHelper, ExcelMapper). Standalone. |

Non-packable:

- `Halifax.Core.Tests`, `Halifax.Excel.Tests` — NUnit 4 test projects (`IsPackable=false`).
- `PeggysCove.Api` — sample/demo app that consumes the libraries via `ProjectReference`
  (not published, no version).

Dependency graph: `Domain ← Core ← {Api, Http}`; `Excel` standalone. `Api` and `Http`
use `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

## Key conventions

- **Target framework:** `net10.0` across every project.
- **SDK:** `global.json` pins SDK `8.0.0` with `rollForward: latestMajor` and
  `allowPrerelease: true`, so a newer installed SDK (currently 10.x) is used.
- **No central package management.** There is no `Directory.Packages.props`; every
  version lives in the individual `.csproj` `<PackageReference>` elements.
- **Internal packages are referenced as published NuGet packages, NOT ProjectReferences.**
  `Halifax.Api`/`Halifax.Http` reference `Halifax.Core` via `<PackageReference ... Version="x.y.z" />`.
  This is why CI packs Domain + Core into a **local NuGet feed** before building the solution
  (see below). The old `ProjectReference` lines are left commented out in the csproj.
- **Nullable reference types enabled** everywhere (`<Nullable>enable</Nullable>`).
- **Pre-existing build warnings** (not introduced by dep bumps, safe to ignore):
  `CS8603` in `Halifax.Http/HalifaxHttpClient.cs:57`, and `SYSLIB0060` (obsolete
  `Rfc2898DeriveBytes` ctor) in `Halifax.Core/Helpers/Crypto.cs:23,44`.

## Versioning & releases

- Each packable project sets its own `<AssemblyVersion>`, `<FileVersion>`, `<Version>`
  (all three kept identical) plus a `<PackageReleaseNotes>` changelog block.
- **Git tags / GitHub releases track the `Halifax.Api` version.** e.g. release `v5.3.0`
  corresponds to `Halifax.Api` 5.3.0. Other packages have their own independent versions
  (Core 5.2.0, Http 5.2.0, Excel 0.4.0 at that point).
- **Pure dependency-update releases have historically been PATCH bumps** (e.g. "v5.1.3 -
  Package dependency updates"). On 2026-07-13 the user chose a **minor** bump instead — so
  confirm bump type with the user rather than assuming.

### Cutting a release (this triggers NuGet publish)

`publish.yml` runs on **GitHub release published** (not on tag push). To release:

```bash
gh release create vX.Y.Z --target main \
  --title "vX.Y.Z — <summary>" --notes "<release notes>"
```

Use the new **Halifax.Api** version for `X.Y.Z`. The workflow packs all packages in
dependency order and pushes to NuGet with `--skip-duplicate` (so unchanged packages that
keep their version are simply skipped). Watch it with:

```bash
gh run list --workflow=publish.yml -L 1
gh run view <run-id>
```

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
   add an explicit top-level `<PackageReference>` pinning the patched version (net10 → 10.0.x)
   in `Halifax.Excel`.
5. Build + test: `dotnet build -c Release && dotnet test --no-build -c Release`
   (79 tests: 53 Core, 26 Excel).

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
