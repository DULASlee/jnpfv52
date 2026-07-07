# Studio S2：compile 主链 + C# 物化（ADR-004）

> Cursor 镜像：`.cursor/rules/studio-s2-compile.mdc`  
> 知识库：`openspec/specs/studio-s2-compile/spec.md` · ADR-004 · `docs/architecture/studio-s2-compile-materialize.md`

## 两大架构变更（2026-07-06）

1. **主链路径** — SA 九步 Agent 与生产主链分离；默认 compile → `SaNineViewCompiler` 确定性投影；Skills 负责语义/双审。
2. **持久化** — `sa_*` 九表物化由 C# `SaMaterializer` 在用户 confirm 后写入主库；**不再**经 sa-service 写库。

## 双模式

| compile（默认） | agent（回归） |
|-----------------|---------------|
| 不需 sa-service | 需 :3001 |
| S2 不写九表 | legacy 同步写库（禁止主链） |
| 物化：C# 直连 | 物化仍走 C#（主链） |

## 验收

`node scripts/phase-sup-s2-e2e.mjs verify --pipeline-id 311` · `E2E_PIPELINE_ID=311 pnpm test:api`

## 禁止

compile 主链依赖 sa-service · S2 期间写九表 · sa-service 物化主库
