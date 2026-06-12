# Pulse 3: 工作流审批跨平台（web-vue3 + jnpf-app-vue3）

> 诊断日期: 2026-06-08
> 诊断方法: 逐文件追踪数据流，对比 web-vue3 与 app-vue3 审批全链路
> 诊断范围: web-vue3 (FlowParser + flowBefore/todo/detail) + app-vue3 (flowBefore/form + flowTodo + operate)

---

## 一、跨平台架构对比

```
                    ┌─────────────────────────┐
                    │      Backend API         │
                    │  /api/workflow/Engine/   │
                    │  FlowBefore/{action}     │
                    │  FlowTask (app专用)       │
                    └──────────┬──────────────┘
                               │
            ┌──────────────────┴──────────────────┐
            │                                     │
    ┌───────┴───────┐                   ┌─────────┴─────────┐
    │   web-vue3    │                   │   jnpf-app-vue3   │
    │  (PC Browser) │                   │  (UniApp Mobile)  │
    └───────┬───────┘                   └─────────┬─────────┘
            │                                     │
    ┌───────┴───────┐                   ┌─────────┴─────────┐
    │  FlowParser   │                   │   flowBefore      │
    │  (全屏Popup)   │                   │   (原生全屏页面)    │
    │               │                   │                   │
    │ ┌───────────┐ │                   │ ┌───────────────┐ │
    │ │ a-tabs    │ │                   │ │ scroll 区      │ │
    │ │ 1.表单信息 │ │                   │ │ ├─ 表单        │ │
    │ │ 2.流程信息 │ │                   │ │ ├─ RecordList  │ │
    │ │ 3.流转记录 │ │                   │ │ └─ 评论列表     │ │
    │ │ 4.审批汇总 │ │                   │ └───────────────┘ │
    │ │ 5.流程评论 │ │                   │ ┌───────────────┐ │
    │ └───────────┘ │                   │ │ flowBtn (底部) │ │
    │               │                   │ └───────────────┘ │
    │ ┌───────────┐ │                   │                   │
    │ │ 6个子Modal │ │                   │ cross-page nav:   │
    │ │ + 3个Popup │ │                   │ operate/index    │
    │ └───────────┘ │                   │ comment/index    │
    └───────────────┘                   └───────────────────┘
```

---

## 二、web-vue3 审批流程详细分析

### 2.1 入口路由

工作流审批有三个核心入口:

| 入口 | 路由 | 组件 | opType |
|---|---|---|---|
| 待办列表 | `/workFlow/flowTodo` | FlowParser (popup) | opType=1 |
| 流程详情 | `/workFlow/workFlowDetail?config=...` | FlowParser (popup) | config 中指定 |
| 流程监控 | `/workFlow/flowMonitor` | FlowParser (popup) | opType=4 |

### 2.2 FlowParser 状态机 (opType 驱动)

```typescript
// opType 定义 (FlowParser.vue:274-284)
'-1' → 我发起的新建/编辑  — 可编辑表单，提交按钮
'0'  → 我发起的详情       — 只读表单，催办/撤回
'1'  → 待办事宜           — 只读表单，通过/退回按钮
'2'  → 已办事宜           — 只读，仅撤回
'3'  → 抄送事宜           — 只读
'4'  → 流程监控           — 管理员视图，指派/变更/挂起/复活
```

### 2.3 数据加载流程 (FlowParser.vue:267-367)

```
init(data)
  → getBeforeInfo(config)
    → GET /api/workflow/Engine/FlowBefore/{id}
      params: { taskNodeId, taskOperatorId, flowId }
    ← 响应结构:
      {
        flowFormInfo:      { enCode, type, propertyJson, urlAddress },
        flowTaskInfo:      { fullName, status, thisStep, flowUrgent, completion, endTime },
        flowTemplateInfo:  { fullName, type, flowTemplateJson },
        flowTaskNodeList:  [{ nodeCode, type, userName, ... }],
        flowTaskOperatorRecordList: [{ handleStatus, userName, ... }],
        approversProperties: { hasAuditBtn, hasRejectBtn, hasSign, hasOpinion, ... },
        formOperates:      [{ write, read, ... }],
        draftData:         null | {},
        formData:          {},
        noOperateAuth:     false,
      }

  → 解析 flowTemplateJson (JSON.parse)
  → 遍历 flowTaskNodeList，匹配节点状态:
      type=0 → 'state-past'   (已通过)
      type=1 → 'state-curr'   (当前节点)
  → 设置节点 user 名称到 flowchart
  → 懒加载表单组件:
      formType == 2  → 'workFlow/workFlowForm/dynamicForm'
      有 urlAddress  → 自定义地址
      否则           → `workFlow/workFlowForm/${enCode}`
      defineAsyncComponent(() => importViewsFile(formUrl))
```

**关键: 动态表单组件加载 (line 362)**
```typescript
state.currentView = markRaw(
  defineAsyncComponent(() => importViewsFile(formUrl))
);
```
- `importViewsFile` 内部使用 `import.meta.glob` 或 `import()` 动态导入
- 表单组件路径由后端数据（enCode/urlAddress）决定
- 不同流程类型加载不同组件，所有组件都通过 `init(config)` 初始化

### 2.4 审批操作链路 (FlowParser.vue:397-604)

```
用户点击"通过"按钮
  │
  ├── eventLauncher('audit')
  │     └── formRef.value.dataFormSubmit('audit', flowUrgent)
  │           └── 子表单组件收集 formData
  │                 └── emit('eventReceiver', formData, 'audit')
  │
  ├── eventReceiver(formData, 'audit')
  │     ├── 保存 formData + flowId
  │     ├── getCandidates(taskId, formData)
  │     │     └── POST /api/workflow/Engine/FlowBefore/Candidates/{id}
  │     │         返回: { type: 1|2|3, list: [...], countersignOver }
  │     │           type=1: 分支流程 (branchList + candidateList)
  │     │           type=2: 候选人 (candidateList)
  │     │           type=3: 无需选择候选人 → 直接确认弹窗
  │     │
  │     ├── type==3 && 无签章/意见/自定义抄送
  │     │     └── 直接 confirm 弹窗 → handleApproval()
  │     │
  │     └── 否则
  │           └── openApprovalModal(true, { branchList, candidateList, ... })
  │
  ├── ApprovalModal 用户确认
  │     └── emit('confirm', approvalData)
  │           └── approvalReceiver(approvalData)
  │
  ├── handleApproval()
  │     └── audit(taskId, { candidateType, ...approvalData, ...formData })
  │           └── POST /api/workflow/Engine/FlowBefore/Audit/{id}
  │
  └── 响应处理 (handleError)
        ├── errorData 非空数组 → openErrorModal (异常处理弹窗)
        └── 否则 → success → closeAll → reload
```

**退回操作类似，多一步获取可退回节点：**
```
eventLauncher('reject')
  → getRejectList(taskId)  → { isLastAppro, list }
  → openApprovalModal (含 rejectList + showReject)
  → reject(taskId, query)
```

### 2.5 流程节点可视化 (FlowProcessMain)

```html
<FlowProcessMain
  :conf="state.flowTemplateJson"
  :isPreview="true"
  :isEnd="state.flowTaskInfo.completion == 100"
  @viewSubFlow="viewSubFlow" />
```

- `flowTemplateJson` 是后端存储的流程定义 JSON (BPMN-like)
- 包含节点树: `{ nodeId, state, content, childNode, conditionNodes[] }`
- FlowProcessMain 递归渲染节点，用 state class 区分颜色（已通过=绿色，当前=蓝色，未来=灰色）
- 子流程通过 `viewSubFlow` 事件触发 SubFlowParser 组件

### 2.6 流转记录 (RecordList)

```
RecordList 组件 (RecordList.vue)
  ├── 传入: flowTaskOperatorRecordList (反转后), endTime, flowId, opType
  ├── 渲染: BasicTable (无分页, 静态数据)
  │     ├── 节点名称 (点击查看详情, 仅 opType==4)
  │     ├── 操作人员
  │     ├── 接收时间 / 操作时间
  │     ├── 执行动作 (彩色圆点 + 状态文字)
  │     │     14种状态: 退回/同意/发起/撤回/终止/指派/后加签/转办/变更/复活/前加签/挂起/恢复/转向
  │     ├── 签名 (Image 组件, 点击预览)
  │     ├── 附件 (jnpf-upload-file)
  │     ├── 备注 (handleOpinion)
  │     └── 事件日志 (点击查看操作日志)
  └── viewNodeLog → LogList popup (详细操作日志)
```

---

## 三、jnpf-app-vue3 审批流程详细分析

### 3.1 入口与传递方式

```javascript
// app-vue3 使用 base64 编码的 JSON 配置跨页面传递
onLoad(option) {
  this.config = JSON.parse(this.jnpf.base64.decode(option.config))
  // config 结构:
  // {
  //   id, flowId, opType, taskId, operatorId, 
  //   category (可选), status (可选), 
  //   delegateUser (委托), hideCancelBtn, isProcessing
  // }
}
```

**对比 web-vue3:** web-vue3 使用 `usePopup` 的 `dataTransferRef` 传递对象，app-vue3 使用 URL 参数 base64 JSON。URL 参数有长度限制（UniApp H5 约 2KB），复杂数据可能截断。

### 3.2 数据加载 (flowBefore/index.vue:481-585)

```
getBeforeInfo()
  → FlowTask(taskId || id || 0, { flowId, opType, operatorId })
    → GET /api/workflow/Engine/FlowTask/{id}
    ← 响应 (与 web-vue3 结构相同但字段名不同):
      {
        flowInfo:       { fullName, type, flowId, flowNodes, ... },
        formInfo:       { enCode, type, formData, ... },
        taskInfo:       { fullName, status, thisStep, flowUrgent, ... },
        btnInfo:        { hasSubmitBtn, hasAuditBtn, hasRejectBtn, ... },
        progressList:   [...],
        nodeList:       [...],
        recordList:     [...],
        nodeProperties: { hasSign, hasOpinion, ... },
        formOperates:   [...],
        draftData:      null | {},
        formData:       {},
      }
```

**对比 web-vue3 API:**
- web-vue3: `GET /api/workflow/Engine/FlowBefore/{id}` + `{ taskNodeId, taskOperatorId, flowId }`
- app-vue3: `GET /api/workflow/Engine/FlowTask/{id}` + `{ flowId, opType, operatorId }`
- 两个不同的后端端点，返回数据结构略有差异（字段命名不一致）

### 3.3 按钮状态机 (flowBefore/index.vue:650-777)

```javascript
// 右侧主按钮 (rightBtnList) — 显示在底部栏右侧
// opType=-1 (新建): 委托发起, 提交
// opType=0  (发起): 催办, 撤回, 撤销, 暂存
// opType=1  (待办): 通过, 减签
// opType=2  (待办列表): 签收/退签, 开始办理
// opType=3  (在办): 通过, 退回, 加签, 转审, 暂存, 催办, 协办, 撤回

// 左侧操作列表 (actionList) — 显示在底部栏左侧"更多"
// 自定义按钮 → 评论 → 查看发起表单 → 催办 → 暂存 → 退回 → 加签 → 转审 → 协办 → 撤销
```

**对比 web-vue3:**
- web-vue3: 全部按钮在顶部 header，通过 computed 控制显示（`v-if`）
- app-vue3: 主按钮在底部固定栏，次要按钮在"更多"操作列表
- app-vue3 按钮状态更细粒度（18 种 status 状态映射 vs web-vue3 的 6 种）

### 3.4 审批操作 — 跨页面导航模式

```
eventReceiver(formData, 'audit')
  │
  ├── getCandidates(operatorId)
  │     → POST /api/workflow/Engine/FlowBefore/Candidates/{id}
  │
  └── operate()  → 跳转到 operate/index 页面
        └── uni.navigateTo({
              url: '/pages/workFlow/operate/index?config=' + encodeURIComponent(JSON.stringify(config))
            })
        
operate/index 页面:
  ├── 审批意见 (HandleOpinion 组件)
  ├── 候选人选择 (candidateList → CommonList)
  ├── 抄送人选择
  ├── 签章
  └── 提交 → uni.$emit('operate', { eventType: 'audit', ...data })
                → flowBefore 页面的 uni.$on('operate', callback)
                  → auditHandle(data) → handleApproval(data)
```

**对比 web-vue3:**
- web-vue3: 子 Modal（ApprovalModal）→ 同页面处理
- app-vue3: 跳转新页面 → uni.$emit 回传数据
- app-vue3 的跨页面事件通信有丢失风险（页面未挂载时 $emit 无效）

### 3.5 流程状态映射 (flowBefore/index.vue:610-649)

```javascript
// app-vue3 使用了 18+ 种状态码的映射
getFlowStatus():
  category='0' (待签):     signfor
  category='1/2' (待办/在办): circulation | back | assist | transfer | recall | revoking | addSign | assign | transfer2
  category='' (发起):      draft | doing | adopt | reject | cancel | pause | revoking | revoke | back | recall
  category='3' (已办):     transfer | agree | refuse | addSign | return | transfer2
  category='4' (抄送):     doing | adopt | reject | back
```

**web-vue3 的 FlowParser 没有 category 概念**，只有 5 种基本 status（1=进行中, 2=通过, 3=退回, 4=撤回, 5=终止）。

### 3.6 列表页 (flowTodo/index.vue)

```
flowTodo (UniApp 页面)
  ├── u-search (关键词搜索, 300ms 防抖)
  ├── u-tabs (待签/待办/在办/已办/抄送 — 5个分类)
  │     每个分类有子状态筛选 (u-subsection)
  ├── mescroll-body (下拉刷新 + 上拉加载更多)
  │     分页参数通过 mescroll 管理
  └── flowList 组件 (卡片列表, 支持左滑操作)
```

---

## 四、跨平台差异矩阵

| 维度 | web-vue3 | jnpf-app-vue3 | 风险等级 |
|---|---|---|---|
| **容器** | BasicPopup (全屏popup) | 原生 UniApp 页面 | 🟡 行为差异 |
| **布局** | a-tabs 多标签页 | 纵向 scroll 单页 | 🟢 设计合理 |
| **表单加载** | `defineAsyncComponent` + `importViewsFile` | 子组件 `form.vue` `$refs.form.init()` | 🟡 加载机制不同 |
| **状态传递** | dataTransferRef (共享 reactive) | URL base64 JSON | 🔴 app 受 URL 长度限制 |
| **操作交互** | 6个子Modal + 3个Popup | navigateTo 新页面 | 🟡 跨页面事件风险 |
| **流程可视化** | FlowProcessMain (SVG/Canvas) | flowStep 组件 (简化版) | 🟢 功能差异合理 |
| **审批历史** | RecordList (a-table) | RecordTimeList (时间线UI) | 🟢 平台适配 |
| **按钮布局** | 顶部 header 固定位置 | 底部固定栏 | 🟢 平台适配 |
| **后端 API** | `/FlowBefore/{id}` + `/FlowBefore/Candidates/{id}` | `/FlowTask/{id}` + `/FlowBefore/Candidates/{id}` | 🔴 API 不一致 |
| **状态码** | 6 种基本状态 | 18+ 种状态码 + category | 🔴 状态体系不同 |
| **分页** | BasicTable usePagination (pageSize 切换) | mescroll (无限滚动) | 🟢 平台适配 |
| **搜索** | BasicForm schemas (无防抖) | u-search (300ms 防抖) | 🟢 app 体验更好 |
| **事件通信** | Vue emit / reactive store | uni.$on/$emit 全局事件 | 🔴 事件丢失风险 |

---

## 五、发现汇总

### P0 严重问题

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| WF-1 | web-vue3 与 app-vue3 使用不同后端 API | FlowBefore vs FlowTask | 数据字段不一致，维护双倍成本 |
| WF-2 | app-vue3 状态码体系与 web-vue3 不一致 | flowBefore/index.vue:610-649 | 同一流程在两平台显示不同状态 |
| WF-3 | app-vue3 URL 传递 base64 配置可能超长截断 | flowBefore/index.vue:220 | 复杂流程数据丢失 |

### P1 架构问题

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| A-1 | FlowParser 状态机过于复杂（6种opType × 10+按钮） | FlowParser.vue | 单文件 790 行，难以维护 |
| A-2 | app-vue3 uni.$emit 跨页面通信无保障 | flowBefore/index.vue:221-223 | 页面未挂载时事件丢失 |
| A-3 | web-vue3 表单组件懒加载失败无降级 | FlowParser.vue:362 | 组件路径错误→白屏 |
| A-4 | 两个平台审批按钮逻辑独立实现 | FlowParser.vue / flowBefore/index.vue | 一处修bug另一处漏修 |
| A-5 | app-vue3 form.vue 组件被两个不同页面复用 (flowForm.vue + index.vue) | flowBefore/form.vue | 两套数据初始化逻辑 |

### P2 技术债务

| # | 发现 | 位置 |
|---|---|---|
| E-1 | RecordList 14 种颜色硬编码 | RecordList.vue:38-53 |
| E-2 | app-vue3 遗留注释掉的旧 getCandidates 方法 | flowBefore/index.vue:1032-1066 |
| E-3 | handleSubmit 吞异常无提示 (Form.vue:314) | 已在 Pulse 2 报告 |
| E-4 | FlowParser 中 catch {} 空块多处 (lines 364, 480, 543) | FlowParser.vue |
| E-5 | flowTemplateJson JSON.parse 无 try-catch | FlowParser.vue:303 |
| E-6 | app-vue3 SCSS 中硬编码 `#ifndef MP-ALIPAY` 条件编译 | flowBefore/index.vue |
| E-7 | 俩平台 flowUrgent 硬编码列表各自维护 | FlowParser.vue:194-198 / flowBefore:172-194 |

---

## 六、跨平台一致性检查清单

| 检查项 | web-vue3 | app-vue3 | 一致？ |
|---|---|---|---|
| 审批通过 API | `POST /FlowBefore/Audit/{id}` | `POST /FlowBefore/Audit/{id}` | ✅ |
| 审批退回 API | `POST /FlowBefore/Reject/{id}` | `POST /FlowBefore/Reject/{id}` | ✅ |
| 转审 API | `POST /FlowBefore/Transfer/{id}` | `POST /FlowBefore/Transfer/{id}` | ✅ |
| 撤回 API | `POST /FlowLaunch/Revoke/{id}` | `POST /FlowLaunch/Revoke/{id}` | ✅ |
| 催办 API | `POST /FlowLaunch/Press/{id}` | `POST /FlowLaunch/Press/{id}` | ✅ |
| 详情查询 API | `GET /FlowBefore/{id}` | `GET /FlowTask/{id}` | ❌ 不同端点 |
| 候选人查询 | `POST /FlowBefore/Candidates/{id}` | `POST /FlowBefore/Candidates/{id}` | ✅ |
| 状态定义 | 1/2/3/4/5/7 | 0-18+ (18种) | ❌ 完全不一致 |
| 表单组件加载 | `defineAsyncComponent` | `$refs.form.init()` | ❌ 不同机制 |
| 按钮权限判断 | `properties.hasXxxBtn` | `btnInfo.hasXxxBtn` | ❌ 字段名不同 |

---

## 七、改进建议 (未纳入本阶段范围)

1. **统一详情查询 API** — app-vue3 使用 `/FlowBefore/{id}` 替代 `/FlowTask/{id}`，对齐数据结构
2. **统一状态码体系** — 后端定义标准状态码枚举，两个前端共用
3. **提取共享审批逻辑** — 将候选人判断、异常处理等核心逻辑抽取为跨平台 composable
4. **app-vue3 改用 postMessage/dataTransfer** — 替代 URL base64 传递复杂配置
5. **app-vue3 操作页改 component 内 Modal** — 减少跨页面事件通信依赖
6. **FlowParser 拆分** — 按 opType 拆分为独立组件（FormEditor/FormViewer/ApprovalViewer/MonitorViewer）
