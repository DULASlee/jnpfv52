---
status: active
owner_mode: goal
objective: "执行《MASTER-JNPF后端重构与Aspire微服务化总体实施计划》v2.1 第一阶段（S0摸底建安全网 ∥ S1家底定稿），全部为只读与快照操作：T0.1 Build/Test基线快照；T0.2 Runtime冒烟；T0.3 行为特征考卷(最高优先,tests/characterization核心API快照用例≥30条入CI)；T0.4 Legacy Compatibility Registry初版；T1.1 platform-asset-inventory.v1定稿(289=157+132复算)；T1.2 数据所有权档案+gen-l1-order.ps1排序清单复核；T1.3 提交人工签收后STOP。硬约束：零业务代码修改、零数据库变更、零删除；Task按九段卡留痕.claude/evidence/backend-refactor/；命中不可逆清单立即STOP等人工；S0与S1双PASS前不得启动T2.0首批5张表级螺旋(细则见docs/superpowers/plans/L1-表级螺旋执行手册-v1.0.md)"
updated_at: 2026-08-27T01:55:59+08:00
adapter_id: jnpf-v52-goal
---

# Active Goal State

## Objective

执行《MASTER-JNPF后端重构与Aspire微服务化总体实施计划》v2.1 第一阶段（S0摸底建安全网 ∥ S1家底定稿），全部为只读与快照操作：T0.1 Build/Test基线快照；T0.2 Runtime冒烟；T0.3 行为特征考卷(最高优先,tests/characterization核心API快照用例≥30条入CI)；T0.4 Legacy Compatibility Registry初版；T1.1 platform-asset-inventory.v1定稿(289=157+132复算)；T1.2 数据所有权档案+gen-l1-order.ps1排序清单复核；T1.3 提交人工签收后STOP。硬约束：零业务代码修改、零数据库变更、零删除；Task按九段卡留痕.claude/evidence/backend-refactor/；命中不可逆清单立即STOP等人工；S0与S1双PASS前不得启动T2.0首批5张表级螺旋(细则见docs/superpowers/plans/L1-表级螺旋执行手册-v1.0.md)

## Authority Sources

- No explicit goal document was provided during bootstrap.

## Operating Contract

- Treat this file as the durable goal state for future agent ticks.
- Treat the authority sources above as the first context to inspect before acting.
- Read current project evidence before choosing the next action.
- Run a bounded progress segment when useful; it does not have to be one tiny step.
- Keep private evidence, credentials, local paths, and raw logs out of public commits.
- End each tick with changed files, validation, residual risk, and the next action.

## Execution Profile

- `cadence=bounded_progress_segment minimum=multi_surface_or_implementation include=coherent_artifact,targeted_validation,state_writeback spend_rule=spend_only_after_artifact_validation_writeback small_streak_threshold=2`
- Repeated small-scale follow-through should expand the next delivery batch or report a blocker before spending quota.

## Non-Goals

- Do not perform irreversible production operations without explicit approval.
- Do not publish private project evidence.
- Do not optimize for activity if no useful artifact or decision can be produced.


## User Todo / Owner Review Reading Queue

- [x] [GATE] T1.3 S0∥S1双PASS人工签收：核验六工件+考卷入CI+inventory定稿后三态裁决(PASS/REFINE/BLOCK)；签收前禁止启动T2.0首批5张表级螺旋
  <!-- loopx:todo todo_id=todo_5f4e10b35b0c status=done task_class=user_gate decision_outcome=approve bound_agent=opencode-refactor-01 completion_continuation=active_goal completed_at=2026-08-26T05:27:40%2B08:00 updated_at=2026-08-26T05:27:40%2B08:00 completion_turn_key=local_completion_588571b046ecce6c3bb97d5a9805fe1d -->

## Agent Todo

- [x] [P1] Run `loopx check` against the project registry and record the first project-specific adapter signal or an explicit no-follow-up rationale.
  <!-- loopx:todo todo_id=todo_fa501099a20c status=done task_class=advancement_task action_kind=onboarding_connection_validation completion_continuation=active_goal completed_at=2026-08-27T01:55:59%2B08:00 updated_at=2026-08-27T01:55:59%2B08:00 completion_turn_key=local_completion_e2d4de529ee4f35548ebeae01b1b9b3e -->
- [x] [P0] T0.1 Build/Test基线快照：dotnet build -c Release(含/p:CI_BUILD=true)+dotnet test backend/zx_lowcode_netcore.sln+verify-toolchain.mjs+test-hooks.mjs，全绿输出归档.claude/evidence/backend-refactor/s0/
  <!-- loopx:todo todo_id=todo_11b523c7f36c status=done task_class=advancement_task action_kind=snapshot_build_test_baseline claimed_by=opencode-refactor-01 completion_continuation=active_goal completed_at=2026-08-26T04:29:29%2B08:00 updated_at=2026-08-26T04:29:29%2B08:00 completion_turn_key=local_completion_2ca40d6eebd675b776f22a5d2c798382 -->
- [ ] [P0] T0.3 行为特征考卷(最高优先)：tests/characterization/ 核心API请求响应快照用例≥30条(登录/用户/字典/菜单/一条表单流/一条审批流)纳入CI，逐条标记四分层(核心冻结/边缘可调/未定义/已知bug)
  <!-- loopx:todo todo_id=todo_6800f8a08d0c status=blocked task_class=advancement_task action_kind=write_characterization_tests claimed_by=opencode-refactor-01 reason=30%E6%9D%A1%E5%8F%AA%E8%AF%BB%E5%BF%AB%E7%85%A7%E5%B7%B2%E8%BE%BE%E6%A0%87%E5%85%A5CI%3B%20%E5%AE%8C%E6%95%B4%E5%85%B3%E9%97%AD%E9%9C%80%E5%BD%95%E5%88%B6%E8%A1%A8%E5%8D%95%E6%B5%81%2F%E5%AE%A1%E6%89%B9%E6%B5%81%E5%86%99%E6%93%8D%E4%BD%9C%E9%93%BE%E8%B7%AF%28%E5%8F%91%E8%B5%B7%E6%8F%90%E4%BA%A4%E5%AE%A1%E6%89%B9%29%2C%20%E6%B6%89%E5%8F%8A%E5%BC%80%E5%8F%91%E5%BA%93%E5%86%99%E5%85%A5%2C%20%E7%AD%89%E7%94%A8%E6%88%B7%E6%98%BE%E5%BC%8F%E6%8E%88%E6%9D%83 updated_at=2026-08-26T14:16:26%2B08:00 -->
- [x] [P1] T0.2 Runtime基线：start-dev.ps1冒烟+jnpf-api.mjs GET /api/oauth/CurrentUser+E2E_PIPELINE_ID=311 pnpm test:api 快照归档
  <!-- loopx:todo todo_id=todo_732145c28871 status=done task_class=advancement_task action_kind=smoke_runtime_snapshot claimed_by=opencode-refactor-01 completion_continuation=active_goal completed_at=2026-08-26T05:41:53%2B08:00 updated_at=2026-08-26T05:41:53%2B08:00 completion_turn_key=local_completion_46f2a391f0f78177ba8d4f2d3c0eeae2 -->
- [x] [P1] T1.1 Platform Asset Inventory定稿：合并ng1a/ng1b产出docs/architecture/platform-asset-inventory.v1.md/.csv(157进入/132处置冻结)，复算289=157+132一致
  <!-- loopx:todo todo_id=todo_179112c78ef3 status=done task_class=advancement_task action_kind=finalize_inventory_doc claimed_by=opencode-refactor-01 completion_continuation=active_goal evidence=docs%2Farchitecture%2Fplatform-asset-inventory.v1.md%20%2B%20.csv%28289x15%29%20%E4%BA%A7%E5%87%BA%3B%20%E5%A4%8D%E7%AE%97%20total%3D289%3DENTER%28157%29%2BFREEZE%28132%29%3B%20%E9%94%AE%E4%B8%80%E8%87%B4%E6%80%A7%20ng1a%E4%B8%8Eng1b%E4%BA%A4%E9%9B%86289%E5%B7%AE%E9%9B%860%3B%20L1%E7%9B%B8%E5%AE%B9%E6%80%A7%20l1-batch-order%20142%2F142%20%E5%85%A8%E8%90%BD%20ENTER completed_at=2026-08-26T14:03:07%2B08:00 updated_at=2026-08-26T14:03:07%2B08:00 completion_turn_key=local_completion_a5ddeee5f08f499ed217cee33fa71c1d -->
- [x] [P1] T1.2 数据所有权档案：JNPF数据责任与模块边界映射主表升格docs/architecture/data-ownership-profile.v1.md；复核gen-l1-order.ps1排序清单(142入围/135 ELIGIBLE/7暂缓)
  <!-- loopx:todo todo_id=todo_ad08f36df63d status=done task_class=advancement_task action_kind=build_ownership_profile claimed_by=opencode-refactor-01 completion_continuation=active_goal evidence=docs%2Farchitecture%2Fdata-ownership-profile.v1.md%20%E5%AE%9A%E7%A8%BF:%20%E5%9F%9F%E7%BA%A7%E5%BD%92%E5%B1%9E%E6%80%BB%E5%9B%BE%2B%E8%A1%A8%E7%BA%A7write_owner%E9%87%8F%E5%8C%96%28ENTER157%E5%90%AB81%E7%BC%BA%E7%9C%81%E6%8C%89%E5%9F%9F%E7%BA%A7%E5%BD%92%E5%B1%9E%E8%A7%84%E5%88%99%29%2BMULTI_WRITER7%E5%BC%A0%E8%A3%81%E5%86%B3%E9%98%9F%E5%88%97%2BNG0%E4%BF%AE%E6%AD%A3%28blade_visual8%E8%A1%A8%E5%AE%9E%E4%B8%BAvisualdata%E8%BF%90%E8%A1%8C%E6%97%B6%E8%A1%A8%E9%9D%9EBladeX%E9%81%97%E7%95%99%29%2BL1%E5%A4%8D%E6%A0%B8142%3D135ELIGIBLE%2B7%E6%9A%82%E7%BC%93%E4%B8%8E%E6%89%8B%E5%86%8C%E4%B8%80%E8%87%B4 completed_at=2026-08-26T14:11:12%2B08:00 updated_at=2026-08-26T14:11:12%2B08:00 completion_turn_key=local_completion_d7fc248b8f27d00bf23f1387045bc73b -->
- [x] [P2] T0.4 Legacy Compatibility Registry初版：ng1a legacy-compatibility-map升格docs/architecture/legacy-compatibility-registry.v1.md(KEEP/REDEFINE/DEPRECATE/REMOVE四态)
  <!-- loopx:todo todo_id=todo_2176e93fa2ba status=done task_class=advancement_task action_kind=write_legacy_registry claimed_by=opencode-refactor-01 completion_continuation=active_goal evidence=docs%2Farchitecture%2Flegacy-compatibility-registry.v1.md%20%E5%AE%9A%E7%A8%BF:%20%E5%9B%9B%E6%80%81%E8%A3%81%E5%86%B3%E5%85%A8%E9%87%8F%E5%8D%87%E6%A0%BC%28%E6%9D%83%E9%99%9012%E9%A1%B9%2B%E7%A7%9F%E6%88%B73%E9%A1%B9%2B%E9%A2%86%E5%9F%9F%E6%95%B0%E6%8D%AE10%E9%A1%B9%2B%E4%B8%8D%E5%A4%8D%E5%88%B6%E6%B8%85%E5%8D%954%E7%BB%84%29%3B%20%E5%90%ABv1%E4%BF%AE%E6%AD%A3%E9%A1%B9%20blade_visual8%E8%A1%A8%20DEPRECATE%E6%9B%B4%E6%AD%A3%E4%B8%BAKEEP%28ng1b%E6%BA%AF%E6%BA%90%29%3B%20%E5%85%BC%E5%AE%B9%E7%AD%96%E7%95%A5%E7%BB%91%E5%AE%9A%E8%A1%8C%E4%B8%BA%E7%89%B9%E5%BE%81%E8%80%83%E5%8D%B730%E6%9D%A1%E4%B8%BA%E7%AD%89%E4%BB%B7%E5%88%A4%E6%8D%AE completed_at=2026-08-26T14:18:42%2B08:00 updated_at=2026-08-26T14:18:42%2B08:00 completion_turn_key=local_completion_6e16ab9233880e3b840a89af863f1a28 -->

## Next Action

## Recent User Feedback

- Initialized by `loopx bootstrap`.

## Progress Ledger

- Created the initial goal state and registry connection.
