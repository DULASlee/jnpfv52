# ImageSharp Security Upgrade

**Date:** 2026-06-08
**Upgrader:** Engineer (Claude Code)

## Before

| Item | Value |
|---|---|
| Package | SixLabors.ImageSharp 3.0.2 |
| Vulnerabilities | 5 known (2 High, 3 Moderate) |
| GHSA IDs | GHSA-65x7-c272-7g7r, GHSA-g85r-6x2q-45w7, GHSA-5x7m-6737-26cr, GHSA-63p8-c4ww-9cg7, GHSA-2cmq-823j-5qj8 |

## After

| Item | Value |
|---|---|
| Package | SixLabors.ImageSharp 3.1.11 |
| Vulnerabilities | 0 |
| Build | 0 errors |

## Changes

1. Upgraded `SixLabors.ImageSharp` from `3.0.2` to `3.1.11` in `JNPF.Common.csproj`
2. Added `<UseImageSharp>enable</UseImageSharp>` to `JNPF.Common.csproj` — 3.1.x changed the implicit using condition from `ImplicitUsings` to `UseImageSharp`

## Verification

```
dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj → 0 errors
dotnet list package --vulnerable → ImageSharp no longer listed
```
