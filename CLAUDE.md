# CLAUDE.md

This file provides guidance to Claude Code when working in **`d:\JNPF-v52`**.

See also [AGENTS.md](AGENTS.md) and [README.md](README.md).

## Workspace

Monorepo: `backend/` + three frontends + `docs/`. Daily work here only; `d:\liu202505v2` is archive.

## Build & Run

```bash
cd d:\JNPF-v52\backend && dotnet build
cd d:\JNPF-v52\backend && dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
cd d:\JNPF-v52\jnpf-web-vue3 && pnpm run dev
cd d:\JNPF-v52\jnpf-web-datascreen && pnpm run dev
cd d:\JNPF-v52\jnpf-app-vue3 && python scripts/proxy_server.py
```

SDK: `backend/global.json` (.NET 6, rollForward latestMajor).

## Architecture

Same as AGENTS.md: `backend/framework`, `backend/modularity`, `backend/application`, `backend/web`, plus `jnpf-web-vue3`, `jnpf-web-datascreen`, `jnpf-app-vue3`.

## Database

- Connection strings: `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json` (gitignored)
- SQL: `backend/web/jnpf_sundial_init.sql`

## Docs & toolchain

- Demo: `docs/v52-demo-manual.md`
- Architecture rules: `docs/architecture/ARCHITECTURE_DOC_RULES.md`
- Toolchain: `TOOLCHAIN.md`, `.cursor/toolchain.manifest.json` (`D--JNPF-v52`)

## Agent toolchain

[`.cursor/rules/toolchain-division.mdc`](.cursor/rules/toolchain-division.mdc) — OpenSpec (KB), Superpowers (dev), Serena (`backend/modularity/`), episodic-memory.
