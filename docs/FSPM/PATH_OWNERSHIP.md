# FSPM Path Ownership Contract

> Contract between Compiler AI and MCP AI — defines physical isolation, branch
> isolation, and path ownership for the FSPM multi-agent workstream.
>
> Locked on: fspm-compiler-wt @ edbfda33 (Phase 1-3 work preserved)
> Authority: Architect-controlled; any change requires Cross-Workstream Review.

---

## 1. Worktree Layout

| Path | Owner | Branch |
|------|-------|--------|
| `D:\JNPF-v52\` | MCP AI | `feature/fspm-mcp-stdio-adapter` (after MCP reassignment) |
| `D:\JNPF-FSPM-Worktrees\compiler\` | Compiler AI | `fspm-compiler-wt` (tracking origin fspm-compiler) |

### Why two branches?

`fspm-compiler` is currently checked out at `D:\JNPF-v52`. Git forbids two
worktrees on the same branch. To keep both worktrees live simultaneously, the
Compiler side tracks `fspm-compiler-wt` (a local branch at the same commit).
When MCP reassigns `D:\JNPF-v52` to its own branch, Compiler can later move to
plain `fspm-compiler`.

---

## 2. Path Ownership Matrix

| Path | Owner | Allowed Modifier |
|------|-------|------------------|
| `backend/modularity/Foundry.FSPM.Compiler/` | Compiler | Compiler AI |
| `backend/tests/Foundry.FSPM.Compiler.Tests/` | Compiler | Compiler AI |
| `backend/modularity/Foundry.FSPM.Mcp/` | MCP | MCP AI |
| `backend/tests/Foundry.FSPM.Mcp.Tests/` | MCP | MCP AI |
| `backend/modularity/Foundry.FSPM.Core/` | Shared Contract | Integration Review |
| `backend/tests/Foundry.FSPM.SemanticProof.Tests/` | Pre-existing | Out of scope (Phase 1+) |
| `backend/modularity/Foundry.FSPM.Login*/` | Pre-existing | Out of scope |
| `backend/tools/Foundry.FSPM.Analyzer/` | Pre-existing | Out of scope |
| `docs/FSPM/INTERFACE_LOCKDOWN.md` | Shared Contract | MCP / Architect Review |
| `docs/FSPM/PATH_OWNERSHIP.md` | Shared Contract | Architect-controlled |
| `docs/superpowers/specs/` | Architecture Docs | Architecture Review |
| `docs/superpowers/plans/` | Architecture Docs | Architecture Review |
| `openspec/` | Shared | No single-Agent restructure |
| `backend/zx_lowcode_netcore.sln` | Shared | Integration Review (any add/move) |
| `backend/global.json`, `backend/.editorconfig` | Shared | Integration Review |
| `.workbuddy/` | Agent-local | FORBIDDEN cross-Agent write |

---

## 3. Shared Path Change Procedure

For any path marked Shared Contract:

1. **Change Proposal**: agent writes the proposed delta to a CR document
   under `.claude/change-requests/`.
2. **Impact Analysis**: list affected downstream files, tests, and the other
   Workstream.
3. **Cross-Workstream Review**: the other Agent must ack the change before
   commit. Silent overwrites are a hard violation.
4. **Commit**: only after review, with explicit co-author trailer.

---

## 4. Agent-Local Rules

Each Worktree owns its `.workbuddy/`, agent-specific `.claude/evidence/`, and
any local-only session state. These must NEVER be committed to the shared
branch and NEVER cross over between worktrees.

If a host tool forces shared `.workbuddy/`, the Agent MUST report it; it must
not silently git-clean it away.

---

## 5. Worktree Creation / Removal Rules

- Creating a new worktree from a branch is allowed ONLY when no other worktree
  has the same branch checked out.
- Worktrees are local-only state in `.git/worktrees/`. They are NOT committed.
- Worktree path must not overlap with another worktree tracked files.
- Removing a worktree: `git worktree remove <path>`. NEVER `rm -rf`.

---

## 6. Iron Laws (Reaffirmation)

1. NEVER delete any existing file (`git clean`, `git reset --hard`, `rm`,
   `Remove-Item`, `del`).
2. NEVER overwrite another Agent working tree (`git checkout --`, `git restore .`,
   `git switch`, `git stash` in the shared tree).
3. Investigate first, then create. Read-only inventory before any write.

---

## 7. Violation Reporting

Any violation of this contract must be reported to the Architect immediately.
A soft violation (e.g., accidentally touching a Shared path) is fixable; a
hard violation (e.g., deleting another Agent untracked work) requires a
rollback and root-cause analysis.
