# Capability Registry — Phase 0.5

> **Principle:** MCP is Driver/Provider, not Governance

Each Capability defines: `CapabilityId, Provider, Scope, AllowedConsumers, InputContract, OutputContract, Permission, Authority, AuditRequirement`

---

## Registry (from inventory 11 MCP)

| CapabilityId | Provider | Scope | AllowedConsumers | InputContract | OutputContract | Permission | Authority | Audit |
|--------------|----------|-------|------------------|---------------|----------------|------------|-----------|-------|
| SymbolSearch | Serena (`serena.exe start-mcp-server --project D:/JNPF-v52`) | PROJECT | `generic-class-refactor-expert`, `table-refactor-expert`, `architecture-gate` | `find_symbol(name_path)` | `symbols + locations` | AgentOS Policy: Expert may use | AgentOS, Governance NONE | Required (log) |
| CallGraph | codegraph (`codegraph serve --mcp`) | PROJECT | `architecture-gate` | `explore(query)` | `callers/callees` | Allow | AgentOS | Required |
| DotNetDebug | netcoredbg (`netcoredbg-mcp-wrapper.py`) | PROJECT | `data-driven-debug` | `attach/breakpoint` | `stack` | Allow | AgentOS | Required |
| ToolRouter | tool-search (`tool-search.mjs`) | PROJECT | All | `query` | `tool recommendation` | Allow | AgentOS | Log |
| BrowserE2E | playwright (`@playwright/mcp`) | PROJECT | `playwright` skill | `navigate/click` | `screenshot` | Allow | AgentOS | Evidence |
| KnowledgeGraph | knowledge-graph (`server-memory`) | PROJECT | Memory | `search_nodes` | `graph` | Allow | AgentOS | — |
| CodebaseMemory | codebase-memory (`codebase-memory-mcp.exe`) | PROJECT | Capability | `search` | `snippets` | Allow | AgentOS | — |
| ChromeDevTools | chrome-devtools | PROJECT | Browser | `devtools` | `DOM` | Allow | AgentOS | — |
| SequentialThinking | sequential-thinking | PROJECT | Advisory | `think` | `steps` | Allow | AgentOS | — |
| InteractiveFeedback | interactive-feedback-mcp (`uv run server.py`) | PROJECT | Advisory | `feedback` | `text` | Allow | AgentOS | — |
| CodebaseMemory-Cursor | codebase-memory | USER | Capability | `search` | `snippets` | Allow | AgentOS | — |

## Governance

- **Authority:** AgentOS (not MCP)
- **Governance:** NONE — MCP cannot define Policy/Gate/State, cannot override Gate, cannot modify Policy priority, cannot declare final authority

## Verification

- `hooks/policy-adversarial` + `blackbox-adversarial` — `Unauthorized Capability → BLOCK` (e.g., fake Serena as governance) and `Authorized Capability → ALLOW` (SymbolSearch for refactor expert)
- `HARNESS-BASELINE.json` counts MCP 11

