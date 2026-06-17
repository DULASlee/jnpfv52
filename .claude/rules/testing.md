# Testing Discipline

> 测试纪律：没有跑过测试，不准说"完成"。触发条件：代码修改完成 / 准备声称"完成"/"通过"/"修复"。

---

## 铁律

```
NO TASK IS COMPLETE WITHOUT RUNNING THE ACTUAL TEST COMMAND
```

## Gate Function — 宣布任务完成前，逐项打勾

```
✅ 测试自检清单：
- [ ] 我跑了 dotnet build（后端）或 vue-tsc --noEmit（前端），输出 0 errors
- [ ] 我跑了实际服务（dotnet run / pnpm run dev）或相关测试命令
- [ ] 我读了完整输出，不是扫一眼就信
- [ ] 如果是 bug 修复，我复现了原始症状并确认消失
- [ ] 我没有用"应该通过"代替实际运行
```

**全部打勾才能声称完成。任何一项空白 = 任务未完成。**

## 强制执行协议

**开始测试验证前，输出：**
```
🧪 Testing Protocol 启动
- 验证目标：[本次变更要验证什么]
- 验证命令：[具体命令]
- 预期结果：[0 errors / 测试全部通过 / 症状消失]
```

**验证完成后，输出：**
```
🧪 验证结果：
- 命令：[实际执行的命令]
- 输出摘要：[关键行，含 exit code]
- 结论：PASS / FAIL
```

---

## 测试流程

| 任务类型 | 测试要求 |
|---|---|
| 逻辑代码 | 写测试 → 实现 → 跑测试 → 通过 |
| CRUD 业务 | 必须跑通端到端主流程 |
| Bug 修复 | 必须复现原始症状 → 修复 → 确认症状消失 |

**原则：**
- 绝不 mock 掉失败。测试红了？修代码或修测试，NEVER 跳过。
- 优先实际服务：dotnet run / pnpm run dev > 假设"应该能跑"。

## 子代理验证

子代理报告"成功"后，MUST 独立检查 VCS diff + 实际验证变更，不信任报告本身。

> **红旗词与"合理化借口 → 真相"表格：** 见 `.claude/rules/engineering-laws.md` Law 2。测试场景下的典型借口（"改动很小"、"上次跑过了"、"CI 会跑的"）均已被 Law 2 覆盖，不在此重复。

---

## 项目健康验证

> 补充架构红线 R5，仅验证已启用且被修改的项目。

每次代码修改后，被修改的项目 MUST 能编译通过：

| 项目 | 日常验证（快速） | 发布前验证（完整） | 何时触发 |
|---|---|---|---|
| 后端（JNPF.API.Entry） | `dotnet build` | `dotnet build -c Release` | 修改 .cs 文件后 |
| 前端（jnpf-web-vue3） | `vue-tsc --noEmit` | `pnpm run build` | 修改 .vue/.ts 文件后 |
| DataV（jnpf-web-datascreen） | `vue-tsc --noEmit` | `pnpm run build` | 仅当被修改时 |
| UniApp（jnpf-app-vue3） | `vue-tsc --noEmit` | `pnpm run build` | 仅当被修改时 |

**日常开发用快速验证（type-check / build），发布前跑完整 build。**

**不验证的项目（与 R5 一致）：**
- OA（禁用）
- IoT/MES（未创建）
