---
name: architecture-doc
description: Write or revise Fruit+JNPF architecture/technical internal reference documents. Use when the user asks to write architecture docs, technical internal references (技术内参), module design docs, or review architecture documentation quality.
scope: JNPF-v52
---

# Architecture Document Writing

Write Fruit+JNPF architecture/technical internal reference documents.

## Mandatory First Step

Read and follow **`docs/architecture/ARCHITECTURE_DOC_RULES.md`** in full before writing or revising any architecture document. This is a permanent, non-negotiable project standard.

Also respect `.cursor/rules/architecture-doc-standards.mdc`.

## Workflow

1. **Scope** — Identify module(s), source paths (`modularity/`, `framework/`, `application/`), and DB tables (from `web/jnpf_sundial_init.sql` or provided DDL).
2. **Verify in source** — Search codebase for classes/methods before documenting. Mark uncertain items **【待源码验证】**; never invent paths or names.
3. **Structure** — Numbered sections (1 → 1.1 → 1.1.1); end each chapter with:
   - 本节核心表清单
   - 本节关键代码路径索引
4. **Diagrams** — Required Mermaid/ASCII for flows, interactions, ER, and architecture layers; numbered titles; label classes/methods/tables.
5. **Code excerpts** — 20–50 lines per core mechanism with file path and `// ★ 关键` comments on important lines.
6. **Self-check** — Run the checklist at the end of `docs/ARCHITECTURE_DOC_RULES.md` before delivery.

## Output Location

| 类型 | 路径 |
|------|------|
| v5.2 模块架构内参 | `docs/architecture/v52/{module-name}.md` |
| v5.2 编写总纲 | `docs/architecture/v52/00-outline-*.md` |
| 跨模块专项设计 | `docs/architecture/v52/design-{topic}.md` |
| OpenSpec 设计 | `openspec/changes/{change-name}/design.md` |

## Project Reminders

- APIs are often auto-generated via `DynamicApiController` from `*Service` classes — document services, not fabricated controllers.
- ORM: SqlSugar; connection strings in gitignored `ConnectionStrings.json`.
