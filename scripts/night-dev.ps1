# ============================================================
# JNPF 夜间无人值守开发脚本
# 用法: powershell -ExecutionPolicy Bypass -File scripts/night-dev.ps1
# 前置条件: dev server 已启动，Claude Code 已配置完毕
# ============================================================

$ErrorActionPreference = "Continue"
Set-Location $PSScriptRoot\..

$logDir = "dev-logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$logFile = "$logDir\dev-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"

function Log($msg) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

Log "========================================"
Log "JNPF 夜间开发启动"
Log "项目: $PWD"
Log "========================================"

# ============================================================
# 前置检查
# ============================================================

# 检查蓝图文件存在
if (-not (Test-Path "docs\frontend\jnpf-taste-blueprint.md")) {
    Log "错误: docs/frontend/jnpf-taste-blueprint.md 不存在，终止执行"
    exit 1
}
Log "蓝图文件: OK"

# ============================================================
# 任务清单
# 修改下方任务为你今晚真正要开发的页面
# 每个任务必须包含：先读蓝图 → 读参照页面 → 开发 → 自检
# ============================================================

$tasks = @(
    @"
【任务A：开发自定义列表页】

第一步：Read docs/frontend/jnpf-taste-blueprint.md
第二步：Read jnpf-web-vue3/src/views/system/billRule/index.vue 作为列表页参照
第三步：在 jnpf-web-vue3/src/views/extend/ 目录下开发一个"系统通知管理"列表页

页面要求：
- 骨架：jnpf-content-wrapper → center → content
- 表格：BasicTable + useSearchForm + formConfig.schemas
- 搜索条件：通知标题(Input)、通知类型(Select，字典通过 baseStore.getDictionaryData('noticeType') 加载)、状态(Select)、发送时间(DateRange)
- 列：通知标题、通知类型、发送人、发送时间、状态(a-tag 颜色区分已读/未读)
- 操作列：#bodyCell + TableAction，包含"查看"(打开详情抽屉) 和"删除"(popConfirm 二次确认)
- 支持 rowSelection + 批量标记已读按钮 + 批量删除按钮
- 批量删除用 Modal.confirm 二次确认

约束：
- 禁止 a-card 包裹
- 禁止自造 CSS 类名
- 禁止 el-table/el-form/el-button
- 禁止 dictCode 属性
- 路径别名用 /@/
- 样式 lang="less" scoped 或纯 WindiCSS 工具类

第四步：代码完成后自检清单：
1. 骨架 jnpf-content-wrapper 三层嵌套
2. BasicTable + useSearchForm
3. #bodyCell + TableAction
4. 导入 from '/@/components/Table'
5. 无 a-card
6. 无自造类名
7. 字典用 baseStore.getDictionaryData
8. 删除有二次确认
"@,

    @"
【任务B：开发弹窗表单】

前提：假设任务A已生成列表页

第一步：Read docs/frontend/jnpf-taste-blueprint.md
第二步：Read jnpf-web-vue3/src/views/permission/user/Form.vue 作为弹窗表单参照
第三步：在任务A生成的列表页同目录下新增 Form.vue 弹窗表单组件

页面要求：
- 使用 BasicPopup 或 BasicModal + BasicForm(FormSchema)
- 表单字段：通知标题(Input, required)、通知类型(Select, dict加载, required)、通知内容(Textarea, required)、接收范围(jnpf-organize-select)
- 在列表页 index.vue 中注册该表单组件，新增按钮打开弹窗
- 编辑模式下回填数据

约束：
- 禁止 a-card 包裹表单
- BasicForm 用 FormSchema 数组定义字段
- 路径别名 /@/
- 样式 lang="less" scoped

第四步：自检
1. 弹窗组件使用 BasicPopup 或 BasicModal
2. 表单使用 BasicForm + FormSchema
3. 无 a-card
4. 无 dictCode
5. 导入路径正确
"@,

    @"
【任务C：开发全页表单】

第一步：Read docs/frontend/jnpf-taste-blueprint.md
第二步：Read jnpf-web-vue3/src/views/extend/formDemo/fieldForm1/index.vue 作为全页表单参照
第三步：在 jnpf-web-vue3/src/views/extend/ 下开发一个"工作流模板配置"全页表单

页面要求：
- 骨架：jnpf-content-wrapper jnpf-content-wrapper-form
- 表单分区用 a-divider orientation="left"（不用 a-card）
- 第一区"基础配置"：模板名称(Input)、模板编码(Input)、适用范围(Select)、启用状态(Switch)
- 第二区"流程节点"：可编辑 a-table（节点名称、节点类型、审批人、超时时间），底部添加行按钮
- 第三区"高级设置"：超时策略(Select)、超时时间(InputNumber)、通知方式(Select)
- 页头右侧：取消按钮 + 保存按钮(loading)
- labelCol: { style: { width: '110px' } }

约束：
- 禁止 a-card 分区
- 表单分区只用 a-divider
- 路径别名 /@/
- 样式 lang="less" scoped
- 禁止 el-* 组件

第四步：自检
1. 骨架 jnpf-content-wrapper-form
2. 分区用 a-divider 不用 a-card
3. ScrollContainer 包裹表单内容
4. 可编辑表格用 a-table
5. 无 el-* 组件
6. 导入路径正确
"@
)

# ============================================================
# 执行任务
# ============================================================
$totalTasks = $tasks.Count
$successCount = 0

for ($i = 0; $i -lt $totalTasks; $i++) {
    $taskNum = $i + 1
    $task = $tasks[$i]

    Log "----------------------------------------"
    Log "任务 $taskNum / $totalTasks 启动"
    Log "----------------------------------------"

    claude --print $task --dangerously-skip-permissions --max-turns 50 2>&1 | Tee-Object -FilePath $logFile -Append

    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
        Log "任务 $taskNum 完成 (success)"
        $successCount++
    } else {
        Log "任务 $taskNum 异常 (exit: $exitCode)"
    }
}

Log "========================================"
Log "全部任务完成: $successCount / $totalTasks 成功"
Log "日志文件: $logFile"
Log "========================================"
