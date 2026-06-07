# Check 2: Existing CI/CD Infrastructure

## 2.1 CI/CD Configuration Files

| File | Exists? | Key Content |
|---|---|---|
| .github/workflows/ | Yes | 3 workflows: ci.yml, cd-staging.yml, cd-production.yml |
| .gitlab-ci.yml | No | — |
| Jenkinsfile | No | — |
| azure-pipelines.yml | No | — |
| .drone.yml | No | — |
| Dockerfile (root) | Yes | `D:/JNPF-v52/Dockerfile` — jnpf-web-vue3 frontend, node:20-alpine multi-stage + nginx |
| Dockerfile (backend) | Yes | `backend/application/JNPF.API.Entry/Dockerfile` — dotnet:6.0 multi-stage build |
| Dockerfile (datascreen) | Yes | `jnpf-web-datascreen/Dockerfile` — node:20-alpine + nginx |
| docker-compose.staging.yml | Yes | Staging: sqlserver, redis, api, web, datascreen |
| docker-compose.production.yml | Yes | Production: sqlserver, redis, api, web, datascreen with hardening |
| .dockerignore | No | — |
| build.sh / build.bat | No | — |
| Makefile | No | — |

### Existing CI/CD Coverage

**Comprehensive GitHub Actions pipeline** covering:

1. **CI (ci.yml)** — Push/PR to main and develop:
   - Backend: `dotnet restore` → `dotnet build` → `dotnet test` with coverage
   - Frontend PC: pnpm install + lint + build
   - Frontend DataScreen: pnpm install + build
   - Docker: validates docker-compose + Dockerfile syntax
   - Config: validates JSON configs + checks .env.example

2. **CD Staging (cd-staging.yml)** — Push to develop or manual:
   - Backend tests (skippable), Docker builds for 3 services (push to ghcr.io)
   - SSH deploy with zero-downtime docker compose, health check verification

3. **CD Production (cd-production.yml)** — Release or manual:
   - Guard: requires "deploy-production" confirmation
   - Production Docker images with semver + SHA tags
   - Pre-deployment DB backup, zero-downtime rolling deploy, health check

## 2.2 Build Configuration

| File | Exists? | Key Settings |
|---|---|---|
| .editorconfig (root) | Yes | 163 lines — frontend: 2-space indent; C#: 4-space, crlf, utf-8-bom, Allman braces |
| .editorconfig (backend) | Yes | 302 lines — comprehensive C# + VB rules, naming conventions, expression-bodied preferences |
| .editorconfig (framework) | Yes | 284 lines — legacy Furion, similar to backend |
| Directory.Build.props (backend) | Yes | net8.0, Version=3.6.0, ImplicitUsings+Nullable, GenerateDocumentationFile=true, CI_BUILD gates |
| Directory.Build.props (framework) | Yes | net8.0, Version=3.4.7, GeneratePackageOnBuild=true |
| Directory.Build.targets | Yes | Sets DocumentationFile to OutputPath |
| global.json | Yes | `backend/global.json`: SDK 8.0.410, rollForward=latestPatch |
| nuget.config | No | — |
| PowerShell scripts | Yes | `scripts/install-toolchain.ps1`, `scripts/night-dev.ps1` |
| Shell scripts (.sh) | No | — |
| Batch scripts (.bat) | Yes | `scripts/claude-full-permission.bat` |

## Summary for Stage 7.2 (CI/CD)

- **Extend existing, do not replace.** The project has a solid multi-environment GitHub Actions pipeline.
- **What's missing:**
  1. No local build convenience scripts (build.sh/bat, Makefile)
  2. No nuget.config for private package feeds
  3. No .dockerignore at project root
  4. Backend Dockerfile references .NET 6.0 (should be .NET 8.0 per global.json)
  5. CI lacks code quality gates (SonarQube, etc.)
  6. No deployment rollback mechanism in CD pipelines
  7. CI references Dockerfile.staging variants that may not exist
- **No conflicts** with existing build scripts.
