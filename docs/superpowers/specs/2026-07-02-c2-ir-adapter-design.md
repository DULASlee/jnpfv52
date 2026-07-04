# C2 — IR → CodeGenService 适配方案

> **日期**：2026-07-02
> **方案**：A — 动态创建临时 VisualDevEntity
> **状态**：待确认

---

## 1. 问题陈述

传统 `CodeGenService.DownloadCode(id, DownloadCodeFormInput)` 依赖：
- `VisualDevEntity`（数据库记录，含 `FormData` JSON、`Tables` JSON）
- `DownloadCodeFormInput`（`module` 字典 ID、`className`、`subClassName`）
- `IViewEngine`（.vm 模板引擎）

AI 流水线只有 `F_FinalIR` JSON 字符串，没有 `VisualDevEntity` 数据库记录。

## 2. 方案 A：动态创建临时 VisualDevEntity（推荐）

### 流程

```
F_FinalIR JSON
  │
  ▼
IrToVisualDevAdapter.Convert(irJson)
  → 构造 VisualDevEntity（临时，落库）
  → 构造 DownloadCodeFormInput（从 IR 模块/类名提取）
  │
  ▼
CodeGenService.DownloadCode(tempEntityId, input)
  → 走完整 .vm 模板生成路径
  → 产物落盘到 StudioWorkspace/{tenantId}/{pipelineId}/generated/
  │
  ▼
清理临时 VisualDevEntity 记录（异步）
```

### 需要构造的 VisualDevEntity 字段

| 字段 | 来源 | 说明 |
|------|------|------|
| `Id` | `Guid.NewGuid()` | 临时主键 |
| `FullName` | IR → moduleName | 项目名称 |
| `FormData` | IR → formData 序列化 | 表单定义 JSON |
| `Tables` | IR → tables 序列化 | 表关系 JSON |
| `WebType` | 固定 `1` | Web 类型 |
| `Type` | 固定 `1` | 模板类型 |
| `ColumnData` | IR → columnData | 列配置（可选） |
| `DeleteMark` | `null` | 软删除标记 |

### IR → FormData 映射（核心）

`VisualDevEntity.FormData` 格式参考现有记录：
```json
{
  "className": ["Student"],
  "areasName": "学生管理",
  "formField": [
    { "fieldName": "name", "fieldType": "string", "fieldLength": 50 },
    { "fieldName": "student_no", "fieldType": "string", "fieldLength": 20 }
  ]
}
```

C2 适配层需要将 IR 的 `tables[].requiredFields[]` 展开为 `formField[]`。

### 优点
- 复用现有 CodeGenService 完整链路（模板、权限、菜单）
- `.vm` 模板不改动
- AI 生成代码与手工平台产物同构

### 缺点
- 需要深入理解 VisualDevEntity 结构
- 临时记录需异步清理

### 工期
1 人日（适配器）+ 0.5 人日（联调验证）

---

## 3. 方案 B：绕过 VisualDevEntity，直接调 .vm 引擎

提取 `CodeGenService` 核心生成逻辑为 `GenerateFromModel(CodeGenModel)` 方法，接受内存对象而非数据库 ID。

### 优点
- 不污染 VisualDev 表
- AI 与手工路径解耦

### 缺点
- 需要拆解 CodeGenService（~500 行），改动大
- 破坏现有代码结构，测试面广

### 工期
2-3 人日

---

## 4. 方案 C：AI 直接生成代码

AI 根据 IR + 规范直接输出 Entity/Service/Controller/Vue，完全不用 CodeGenService。

### 优点
- 完全独立，零耦合

### 缺点
- 代码质量一致性无法保证
- 与手工平台产物不同构，违反双轨同构原则
- 每次生成结果不确定

---

## 5. 推荐

**方案 A**。最小改动、复用现有生成器、AI 产物与手工产物同构。临时 VisualDevEntity 异步清理即可。
