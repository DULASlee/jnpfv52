# SA 业务规则配置中心 - 前端

人类专家批改 AI 生成的 SA 资产,DKEE 自动学习的可视化界面。

## 技术栈

- React 18 + TypeScript
- Vite 5 (构建)
- React Router 6 (路由)
- TanStack Query 5 (数据获取)
- Axios (HTTP)
- Tailwind CSS (样式)

## 文件结构

```
sa-frontend/
├── package.json
├── vite.config.ts
├── tsconfig.json
├── tailwind.config.js
├── index.html
└── src/
    ├── main.tsx                 # React 入口
    ├── App.tsx                  # 路由配置
    ├── index.css                # Tailwind + 全局样式
    ├── types/sa.ts              # TypeScript 类型
    ├── api/
    │   ├── client.ts            # axios 客户端
    │   └── saApi.ts             # SA 资产 API
    ├── utils/
    │   └── changeTracker.ts     # 变更追踪(DKEE 入口)
    ├── components/
    │   ├── Layout.tsx           # 主布局
    │   ├── ValidationBadge.tsx  # 状态徽章
    │   └── DecisionTableEditor.tsx  # ★ 判定表编辑器(核心)
    └── pages/
        ├── DashboardPage.tsx        # 项目看板
        ├── ProjectDetailPage.tsx    # 项目详情(9 步导航)
        ├── DecisionTableEditPage.tsx # 判定表编辑
        └── DKEEReviewPage.tsx       # 知识图谱审查
```

## 快速开始

```bash
cd sa-frontend
npm install
npm run dev
```

打开 http://localhost:5173

## 核心功能

### 1. 项目看板(DashboardPage)

- 列出所有需求分析项目
- 显示每个项目的 9 步验证状态
- 一键新建需求分析

### 2. 项目详情(ProjectDetailPage)

- 9 步 SA 流水线导航
- 显示数据字典 / 判定表 / 状态机摘要
- 一键触发 DKEE 提炼

### 3. 判定表编辑器(★ 核心)

可视化编辑业务规则:
- 条件作为列,规则作为行
- 单元格绿色=AI 生成,黄色=人类修改
- 实时 diff 预览
- 保存时自动通知 DKEE 学习

### 4. 知识图谱审查(DKEEReviewPage)

- 按行业查看所有提炼出的 Pattern
- 显示评分(0-1,基于使用次数/成功率/时效)
- 评分条直观显示 Pattern 质量

## DKEE 自动学习流程

```
用户在编辑器修改判定表
    ↓
ChangeTracker.record(table, field, before, after)
    ↓
用户点击"保存"
    ↓
API 写入 sa_decision_table
ChangeTracker.commit() 记录修改
    ↓
后端 DKEE 服务读取 change_log
    ↓
跨项目聚合 → 提炼 Pattern
    ↓
写入 kg_pattern,评分入库
    ↓
下次跑 SA 时,Top Pattern 注入 LLM context
```

## 与后端的对接

后端 SDK 在 `localhost:3000`,前端通过 Vite proxy 转发 `/api` 请求。

后端需实现以下 endpoint:
- `GET  /api/projects`
- `GET  /api/projects/:id`
- `POST /api/projects`
- `GET  /api/projects/:id/dictionary`
- `PUT  /api/projects/:id/dictionary`
- `GET  /api/projects/:id/decision-tables`
- `PUT  /api/projects/:id/decision-tables/:tableId`
- `POST /api/changes` (DKEE 学习入口)
- `GET  /api/dkee/patterns?industry=...`

## 给前端同学的 3 步上手

```bash
# 1. 装依赖
cd sa-frontend
npm install

# 2. 启动 dev server
npm run dev
# → http://localhost:5173

# 3. (可选)生产构建
npm run build
npm run preview
```

## 视觉规范

- **绿色 (cell-ai)**: AI 生成,未修改
- **黄色 (cell-human)**: 人类修改过
- **红色 (cell-failed)**: Validator 失败
- **灰色 (cell-pending)**: 待校验

## 关键设计点

1. **单元格级别追踪**:`modifiedCells` Set 精确追踪每个被改的单元格
2. **Diff 预览**:保存前显示"哪几行哪几列"被改了,防止误操作
3. **ChangeTracker**:每次修改都进 buffer,commit 时批量提交,失败可 discard
4. **DKEE 自动触发**:保存 = 自动学习,无需人工干预
5. **评分门禁**:`score >= 0.6` 的 Pattern 才进 KG context

## 扩展方向

- **StateMachineEditor**: 拖拽式状态机编辑(用 react-flow)
- **DataDictionaryEditor**: 表格化字段编辑(类似 Airtable)
- **ERDiagramViewer**: ER 图可视化(用 react-flow)
- **BPMN Viewer**: 业务流程图渲染
- **实时协作**: 多人同时编辑一个判定表(用 Yjs)
- **版本对比**: 显示本次修改与上一版本的 diff
- **Pattern 反馈**: 用户对注入的 Pattern 打分,反向训练
