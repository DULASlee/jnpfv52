# ADR-017: 新旧代码生成器共存策略

**状态:** Final
**日期:** 2026-06-12
**阶段:** Sprint 0-A Day 4

---

## 背景

JNPF 当前有且仅有一套代码生成路径：

```
在线设计器 → .vm 模板 (Apache Velocity) → 代码字符串 → 客户下载
```

这套路径（"在线 .vm 模式"）经 368 个模板文件验证，是当前业务的唯一真源。

Phase 0 (F-4/F-6a) 引入了新的路径：

```
IR (FormPageIR) → TypeScript 编译器 → 标准 Vue 3 项目 → pnpm dev 独立运行
```

两套路径的**输入相同**（在线设计器导出的 JSON Schema），但**输出不同**：
- 在线 .vm：生成 JNPF 平台依赖的代码（jnpfParser / BasicForm）
- TS 编译器：生成独立运行的 Vue 3 项目（ant-design-vue / axios）

---

## 决策内容

**两套代码生成器长期共存，不互相替代。**

- 在线 `.vm` 模式：继续服务现有客户，保持 368 个模板的兼容性
- TS 编译器模式：服务新需求（独立部署、AI 加持、私有化）的客户

### 共存策略

| 维度 | 在线 .vm | TS 编译器 |
|------|---------|----------|
| 触发方式 | 在线设计器 → 点击生成 | CLI: `pnpm generate --schema <path>` |
| 输入 | VisualDev FormData JSON | FormPageIR (from Schema Cleaner) |
| 输出 | JNPF 平台耦合代码 | 独立 Vue 3 项目 |
| 运行时依赖 | jnpfParser + BasicForm + BasicTable | ant-design-vue + axios |
| 生成标记 | 无 (Velocity 模板无标记能力) | `@jnpf-generated` 头部 |
| diff 支持 | 无 | `@jnpf-gen:insert-point` 占位符 |
| 目标客户 | 现有 SaaS 客户 | 私有化部署 / AI 增强客户 |

### diff 机制

TS 编译器在所有生成文件中注入 `@jnpf-gen:insert-point` 占位符：

```
// @jnpf-gen:insert-point=custom-imports
// @jnpf-gen:end-insert-point=custom-imports
```

用户可在占位符之间添加自定义代码，重新生成时占位符外区域被覆盖，占位符内区域保留。

`scripts/diff-codegen.ts` 实现此 diff 逻辑并输出报告。

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|------|------|------|----------|
| 完全替换 .vm 为 TS 编译器 | 技术栈统一 | 368 个模板需迁移，业务中断风险极高 | 不可接受 |
| 只保留 .vm，不做 TS 编译器 | 无维护成本 | 失去独立部署/AI 能力 | 战略放弃 |
| **共存（本决策）** | 兼容 + 创新双轨 | 双倍维护成本 | ✅ 选择 |

---

## 后果

**正面:** 现有客户不受影响；新客户可选独立部署模式；IR 作为中间层统一两套输出。

**负面:** 维护两套代码生成器（368 .vm + 2 TS 编译器）；组件注册表需覆盖两套链路。

**缓解:** IR 作为唯一中间表示，两套编译器共享 IR 类型系统；.vm 模板仅在新功能需求时改动。

---

## 附录: diff 脚本使用

```bash
# 执行 diff: 对比生成代码与基准代码
cd jnpf-web-vue3
pnpm diff:codegen

# 输出: docs/adr-017-diff-report.md
# 报告内容:
#   - 新增文件列表
#   - 修改文件列表（标记 insert-point 内/外）
#   - 生成器版本号
```
