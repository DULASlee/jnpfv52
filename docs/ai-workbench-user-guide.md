# JNPF AI 对话工作台 — 用户指南

> v5.2 Phase 5 | 2026-06-14

## 一、快速开始

### 1.1 配置 API Key

创建 `.env.local` 文件：

```env
VITE_DEEPSEEK_API_KEY=sk-your-deepseek-key
VITE_DEEPSEEK_V4_API_KEY=sk-your-v4-key
VITE_TONGYI_API_KEY=sk-your-dashscope-key
VITE_OPENAI_API_KEY=sk-your-openai-key
VITE_MIMO_API_KEY=your-mimo-key
VITE_MIMO_BASE_URL=https://mimo-api.example.com/v1
# Ollama 本地无需 Key
```

### 1.2 启动工作台

```bash
cd jnpf-web-vue3
pnpm run dev
# 访问 http://localhost:3100
# 导航到 /ai/workbench
```

---

## 二、五阶段流程

工作台按顺序引导用户完成5个阶段：

### 阶段1：需求分析
- **做什么**：用自然语言描述业务需求
- **AI做什么**：识别业务实体、关系、规则，生成领域模型和用户故事
- **你需要做什么**：回答AI的追问、确认领域模型
- **产出**：结构化需求分析文档

### 阶段2：架构设计
- **AI做什么**：模块划分、数据库表设计、API端点设计
- **自动保证**：所有表自动注入 TENANT_ID + 审计字段 + IS_DELETED
- **你需要做什么**：确认模块划分和API设计
- **产出**：架构设计文档

### 阶段3：UI/DB设计
- **AI做什么**：生成页面IR（表单/列表/大屏）+ 完整数据库DDL
- **你需要做什么**：确认页面布局和交互方案
- **产出**：页面IR + 迁移SQL

### 阶段4：代码生成
- **做什么**：选择编译目标 → 点击"编译"
- **支持目标**：Vue3 Web、微信/支付宝/抖音小程序、H5、大屏、工作流
- **产出**：可独立运行的项目代码

### 阶段5：交付
- **做什么**：点击"下载ZIP"获取完整项目包
- **包含**：Vue组件、TypeScript类型、API层、package.json、README

---

## 三、编译目标选择

编译目标下拉框支持多选：

| 目标 | 说明 | VIP |
|:---|:---|:---|
| Vue3 Web | 标准 Vue3 + Ant Design Vue | - |
| 微信小程序 | UniApp 微信小程序 | - |
| 支付宝小程序 | UniApp 支付宝小程序 | - |
| 抖音小程序 | UniApp 抖音小程序 | - |
| H5 移动端 | UniApp H5 | - |
| 大屏 | ECharts + 3D | 3D子功能 |
| 工作流 | FlowIR → 可部署配置 | - |

---

## 四、专家模式

当AI服务不可用或置信度不足时，系统自动降级为专家模式：

- 领域模型画板：拖拽式实体关系设计
- 架构图计算器：从EAB快照选择组件
- 决策表编辑器：手动编写业务规则

**关键**：专家模式产出的IR结构与AI模式完全一致，可随时切换。

---

## 五、常见问题

**Q: AI生成的表结构是否安全？**
A: 架构师智能体会自动注入 TENANT_ID、CREATE_USER_ID、CREATE_TIME 等审计字段，不会遗漏。

**Q: 如何验证AI生成的代码质量？**
A: 编译网关内置IR验证器，不合格的IR会在编译阶段报错。Evals基准测试保证准确率≥80%。

**Q: API Key如何保证安全？**
A: 从环境变量读取，不硬编码，不提交到Git。未配置Key时仅在控制台warn，不会崩溃。
