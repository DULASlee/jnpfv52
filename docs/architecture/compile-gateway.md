# 统一编译网关架构

> Phase 4 核心交付 | 2026-06-14

---

## 架构概览

```
JNPF Platform Schema
       │
       ▼
  cleanSchema()         ← 双层 JSON 解包 + 字段映射
       │
       ▼
  FormPageIR / DashboardIR    ← 平台无关 IR
       │
       ▼
  validateIR()          ← 结构校验（7 条规则）
       │
       ▼
  compileGateway()      ← 目标路由
       │
       ├── vue3-web ───── Vue3Compiler
       ├── dashboard ──── DashboardCompiler
       ├── dashboard-3d ── DashboardCompiler (+3D)
       ├── uniapp-weixin ─ UniAppCompiler (mp-weixin)
       ├── uniapp-alipay ─ UniAppCompiler (mp-alipay)
       ├── uniapp-douyin ─ UniAppCompiler (mp-douyin)
       ├── uniapp-h5 ───── UniAppCompiler (h5)
       └── uniapp-x-app ── UniAppXCompiler (v5.0 暂缓)
              │
              ▼
         CompileResult
         { project, warnings, complexExpressions }
              │
         ┌────┴────┐
         ▼         ▼
    bundleToZip   formIRToSchema
    downloadZip   (回写平台)
```

## 编译目标（8 个）

| ID | 名称 | IR 类型 | VIP | 状态 |
|----|------|---------|-----|------|
| `vue3-web` | Vue3 Web 应用 | form | ❌ | ✅ 已实现 |
| `dashboard` | 数字大屏 | dashboard | ❌ | ✅ 已实现 |
| `dashboard-3d` | 3D 数字孪生 | dashboard | ✅ | ✅ 已实现 |
| `uniapp-weixin` | 微信小程序 | form | ❌ | ✅ 已实现 |
| `uniapp-alipay` | 支付宝小程序 | form | ❌ | ✅ 已实现 |
| `uniapp-douyin` | 抖音小程序 | form | ❌ | ✅ 已实现 |
| `uniapp-h5` | H5 移动端 | form | ❌ | ✅ 已实现 |
| `uniapp-x-app` | 原生 App | form | ✅ | ⏸ v5.0 暂缓 |

## 核心接口

### compileGateway(request) → CompileResponse

```typescript
interface CompileRequest {
  schema: unknown;       // 原始平台 Schema
  target: CompileTarget; // 编译目标
  config: { entity: string; ... };
}

interface CompileResponse {
  success: boolean;
  project?: Map<string, string>;
  issues?: ValidationIssue[];
  warnings?: string[];
  error?: string;
  duration?: number;
}
```

### IR ↔ Schema 双向通道

```
formIRToSchema(FormPageIR) → PlatformSchema  // 前向
schemaToFormIR(PlatformSchema) → FormPageIR   // 回读
```

### 下载能力

```
compileExport(schema, target) → Blob → downloadZip()
```

## VIP 隔离

- `dashboard-3d` 和 `uniapp-x-app` 标记为 VIP
- 网关层仅做标记，鉴权由上层处理
- 非 VIP 调用 VIP 目标 → 返回成功（网关不拦截）

## 扩展指南

新增编译目标：

1. 在 `targets.ts` 的 `COMPILE_TARGETS` 中添加元数据
2. 在 `gateway.ts` 的 `switch` 中添加编译分支
3. 创建对应编译器（实现 `compile(ir) → CompileResult` 接口）
4. 编写网关测试用例
