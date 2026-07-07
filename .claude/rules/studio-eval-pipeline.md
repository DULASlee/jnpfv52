# Studio Eval Pipeline：四层评估 + Judge 校准（阶段七）

> Cursor 镜像：`.cursor/rules/studio-eval-pipeline.mdc`  
> 知识库：`openspec/specs/studio-eval-pipeline/spec.md` · `docs/AI原生开发/1、多用户多任务并行/15、全链条第七阶段开发计划.md`

## 四层 Eval Pipeline（2026-07-08）

| 层 | 方法 | LLM |
|----|------|-----|
| L1 组件 | JSON Schema 校验（IR 产出 fragment） | 否 |
| L2 轨迹 | 冗余 LLM 调用检测（≤500 分页） | 否 |
| L3 任务 | DoD 完成率（run 状态 + eventCount） | 否 |
| L4 业务 | Judge pass/fail 二元（跨家族 mimo） | 是 fast |

**fail-fast：** L1 不过跳过 L2/L3/L4。

## Judge 校准（Cohen's kappa）

- 跨家族 Judge（生成 deepseek / Judge mimo），避免自偏好
- pass/fail 二元（非 1-5 分制），Score≥60 → PASS
- 月度 Job `EvalCalibrationJob`（cron `0 0 2 1 * ?`）跑 kappa
- kappa<0.6 → untrusted，L4 降级为 advisory

## 验收

`node scripts/phase7-eval-verify.mjs`（23 项 DoD）· `dotnet build` InteAssistant 0 错误

## 禁止

L1-L3 用 LLM · Judge 绕过 Guard · Judge 同家族 · 删除 IR events · Eval 查询不带 TenantId
