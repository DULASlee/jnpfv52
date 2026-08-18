const fs = require('fs');
const path = require('path');
const ev = 'D:/JNPF-v52/.claude/evidence/backend-quality-check';
fs.mkdirSync(ev, { recursive: true });

const severe = [
  { cc: 138, cognitive: 834, name: 'ImportDataAssemble', file: 'modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs' },
  { cc: 130, cognitive: 778, name: 'ImportDataAssemble', file: 'modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs' },
  { cc: 97, cognitive: 710, name: 'GetKeyData', file: 'modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs' },
  { cc: 94, cognitive: 435, name: 'GetListQuerySql', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 91, cognitive: 267, name: 'TemplatesDataAggregation', file: 'modularity/codegen/JNPF.CodeGen/CodeGenService.cs' },
  { cc: 84, cognitive: 432, name: 'FuncToMenu', file: 'modularity/visualdev/JNPF.VisualDev/VisualDevService.cs' },
  { cc: 80, cognitive: 255, name: 'GetSelector', file: 'modularity/system/JNPF.Systems/Permission/OrganizeAdministratorService.cs' },
  { cc: 72, cognitive: 320, name: 'GetIntegrateNodeList', file: 'modularity/inteAssistant/JNPF.InteAssistant.Engine/InteAssistantRun.cs' },
  { cc: 65, cognitive: 301, name: 'GetCDataList', file: 'modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs' },
  { cc: 64, cognitive: 302, name: 'GetCDataList', file: 'modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs' },
  { cc: 63, cognitive: 259, name: 'ImportData', file: 'modularity/system/JNPF.Systems/System/ModuleService.cs' },
  { cc: 61, cognitive: 237, name: 'TemplateControlsDataConversion', file: 'modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs' },
  { cc: 54, cognitive: 158, name: 'SaveDataToDataByFId', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 53, cognitive: 126, name: 'GetListResult', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 53, cognitive: 191, name: 'SingleTableFrontEnd', file: 'modularity/engine/JNPF.VisualDev.Engine/CodeGen/CodeGenWay.cs' },
  { cc: 48, cognitive: 219, name: 'StreamLlmResponseAsync', file: 'modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs' },
  { cc: 45, cognitive: 279, name: 'GetConditionAsync', file: 'modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs' },
  { cc: 45, cognitive: 279, name: 'GetDataConditionAsync', file: 'modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs' },
  { cc: 45, cognitive: 299, name: 'GenerateFeilds', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 44, cognitive: 170, name: 'GetVisualDevCaCheData', file: 'modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs' },
  { cc: 43, cognitive: 150, name: 'Login', file: 'modularity/oauth/JNPF.OAuth/OAuthService.cs' },
  { cc: 41, cognitive: 149, name: 'ExportMemoryStream', file: 'modularity/common/JNPF.Common/Security/ExcelExportHelper.cs' },
  { cc: 40, cognitive: 171, name: 'ImportFirstVerify', file: 'modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs' },
  { cc: 38, cognitive: 209, name: 'GetCodeGenAuthorizeModuleResource', file: 'modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs' },
  { cc: 37, cognitive: 193, name: 'GetCondition', file: 'modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs' },
  { cc: 37, cognitive: 166, name: 'FieldBindDefaultValue', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 37, cognitive: 159, name: 'ImportFirstVerify', file: 'modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs' },
  { cc: 36, cognitive: 130, name: 'ImportPreview', file: 'modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs' },
  { cc: 36, cognitive: 103, name: 'GetCurrentUser', file: 'modularity/oauth/JNPF.OAuth/OAuthService.cs' },
  { cc: 36, cognitive: 131, name: 'GetImportPreviewData', file: 'modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs' },
  { cc: 35, cognitive: 131, name: 'GetItemRule', file: 'modularity/engine/JNPF.VisualDev.Engine/Security/BackEnd/CodeGenControlsAttributeHelper.cs' },
  { cc: 34, cognitive: 136, name: 'GetCreateFirstColumnsHeader', file: 'modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs' },
  { cc: 34, cognitive: 132, name: 'GetTargetForm', file: 'modularity/inteAssistant/JNPF.InteAssistant.Engine/InteAssistantRun.cs' },
  { cc: 33, cognitive: 177, name: 'GetParsDataByList', file: 'modularity/common/JNPF.Common.CodeGen/DataParsing/ControlParsing.cs' },
  { cc: 32, cognitive: 78, name: 'GetListChildTable', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 31, cognitive: 54, name: 'ImportUserData', file: 'modularity/system/JNPF.Systems/Permission/UsersService.cs' },
  { cc: 31, cognitive: 72, name: 'SyncPortal', file: 'modularity/visualdev/JNPF.VisualDev/PortalService.cs' },
  { cc: 30, cognitive: 146, name: 'GetSuperQueryInput', file: 'modularity/common/JNPF.Common/Security/SuperQueryHelper.cs' },
  { cc: 30, cognitive: 61, name: 'GetCreateSqlByTemplate', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 30, cognitive: 150, name: 'GetSuperQueryInput', file: 'modularity/visualdev/JNPF.VisualDev/RunService.cs' },
  { cc: 30, cognitive: 104, name: 'GetCreateFirstColumnsHeader', file: 'modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs' },
];

const bands = { cc_gt_29: 41, cc_20_29: 29, cc_10_19: 171, cc_lt_10: 7441, totalMethods: 7682 };
const complexity = {
  generatedAt: new Date().toISOString(),
  source: 'Codebase-Memory MCP query_graph project=jnpf-v52 Method.complexity',
  gateStatus: 'NOT_IMPLEMENTED',
  gateNote: 'ComplexityAnalyzer + complexity-baseline.json still pending (W0 design only)',
  bands,
  severeCount: severe.length,
  severe,
};
fs.writeFileSync(path.join(ev, 'check02-complexity-inventory.json'), JSON.stringify(complexity, null, 2));

let securitySummary = { generatedAt: new Date().toISOString(), tool: 'security-scan 5.6.7', status: 'completed' };
const sarifPath = path.join(ev, 'security-scan.sarif');
if (fs.existsSync(sarifPath)) {
  const s = JSON.parse(fs.readFileSync(sarifPath, 'utf8'));
  const findings = [];
  const byRule = {};
  for (const r of s.runs || []) {
    for (const res of r.results || []) {
      const id = res.ruleId || '?';
      byRule[id] = (byRule[id] || 0) + 1;
      const loc = (res.locations && res.locations[0] && res.locations[0].physicalLocation) || {};
      findings.push({
        ruleId: id,
        level: res.level || null,
        message: (res.message && res.message.text) || null,
        uri: loc.artifactLocation && loc.artifactLocation.uri,
        line: loc.region && loc.region.startLine,
      });
    }
  }
  securitySummary = { ...securitySummary, findingCount: findings.length, byRule, findings, sarif: sarifPath };
} else {
  securitySummary = { ...securitySummary, findingCount: 1, note: 'sarif missing; log said 1 warning SCS0006', findings: [] };
}
fs.writeFileSync(path.join(ev, 'check04-security-scan-summary.json'), JSON.stringify(securitySummary, null, 2));

function readJson(p) {
  try {
    return JSON.parse(fs.readFileSync(p, 'utf8'));
  } catch {
    return null;
  }
}
const archFw = readJson(path.join(ev, 'arch01-jnpf-framework.json'));
const archCc = readJson(path.join(ev, 'arch01-common-core.json'));
const archPr = readJson(path.join(ev, 'arch01-project-references.json'));
const arch = {
  generatedAt: new Date().toISOString(),
  tool: 'NetArchTest.Rules 1.3.2 + ProjectReference scan',
  project: 'backend/tests/JNPF.Tests.Architecture',
  dotnetTest: '3 passed / 0 failed',
  ARCH01_framework: archFw,
  ARCH01_commonCore_inventory: archCc,
  ARCH01_projectReferences: archPr,
};
fs.writeFileSync(path.join(ev, 'check01-architecture-summary.json'), JSON.stringify(arch, null, 2));

const report = {
  generatedAt: new Date().toISOString(),
  title: 'Backend quality checks 1-2-4',
  verdict: {
    check1_architecture:
      archCc && archCc.isSuccessful === false
        ? 'INVENTORY_FAIL_EXPECTED'
        : archFw && archFw.isSuccessful
          ? 'PARTIAL'
          : 'UNKNOWN',
    check2_complexity: 'INVENTORY_OK_GATE_MISSING',
    check4_security:
      securitySummary.findingCount === 0
        ? 'CLEAN'
        : securitySummary.findingCount === 1
          ? 'LOW_FINDINGS'
          : 'NEEDS_REVIEW',
  },
  highlights: {
    frameworkNoInteAssistant: !!(archFw && archFw.isSuccessful),
    commonCoreDependsInteAssistant: !!(archCc && archCc.isSuccessful === false),
    projectRefHits: archPr && archPr.count,
    commonCoreFailingTypeCount: archCc && archCc.failingTypeCount,
    severeMethodsCcGt29: 41,
    securityFindings: securitySummary.findingCount,
    securityTop: securitySummary.findings,
  },
  evidenceDir: ev,
};
fs.writeFileSync(path.join(ev, 'checks-1-2-4-summary.json'), JSON.stringify(report, null, 2));

const findingsMd = (securitySummary.findings || [])
  .map((f) => `- ${f.ruleId} @ ${f.uri || '?'}:${f.line || '?'} — ${f.message || ''}`)
  .join('\n');

const md = `# 后端质量检查 1-2-4 汇总

> 生成时间：${report.generatedAt}
> 证据目录：\`.claude/evidence/backend-quality-check/\`

## 结论（老板一眼）

| # | 项 | 结果 | 含义 |
|---|----|------|------|
| 1 | 架构（NetArchTest ARCH-01） | **框架层通过；Common.Core 清单失败（预期）** | 核心框架未依赖 InteAssistant；公共层仍挂着 InteAssistant 引用，待拆 Contracts |
| 2 | 复杂度 | **盘点完成；硬门未上** | CC>29 共 **41** 个方法；Analyzer+baseline 尚未落地 |
| 4 | 安全扫描 | **${securitySummary.findingCount} 条警告** | Security Code Scan：见下方明细 |

## 1. 架构检查

- 工具：NetArchTest + csproj ProjectReference 扫描
- 命令：\`dotnet test backend/tests/JNPF.Tests.Architecture\` → **3/3 通过**（Common.Core 为清单模式，不阻断）
- \`JNPF\` 框架程序集 → InteAssistant*：**无依赖（PASS）**
- \`JNPF.Common.Core\` → InteAssistant*：**有依赖（INVENTORY FAIL，预期）**；失败类型样本数：${(archCc && archCc.failingTypeCount) || '?'}
- 非 InteAssistant 工程中含 InteAssistant 字样的 csproj：**${(archPr && archPr.count) || '?'}** 个

## 2. 复杂度检查

- 数据源：Codebase-Memory \`jnpf-v52\` Method.complexity
- 分档：CC>29 = **41**；20–29 = **29**；10–19 = **171**；<10 = **7441**（方法总数 7682）
- 最高：\`ImportDataAssemble\` CC=138 / 认知=834（VisualDevModelDataService）
- 硬门状态：**未实现**（设计见 backend-quality-remediation W0）
- 明细：\`check02-complexity-inventory.json\`；业务排序见 \`docs/architecture/v52/design-quality-hotspot-top20.md\`

## 4. 安全扫描

- 工具：\`security-scan\` 5.6.7（Security Code Scan）
- 范围：\`backend/zx_lowcode_netcore.sln\`（排除 tests/tools）
- 结果：**${securitySummary.findingCount}** 条

${findingsMd}

## 产物清单

- \`check01-architecture-summary.json\`
- \`arch01-jnpf-framework.json\` / \`arch01-common-core.json\` / \`arch01-project-references.json\`
- \`check02-complexity-inventory.json\`
- \`check04-security-scan-summary.json\` / \`security-scan.sarif\` / \`security-scan.log\`
- \`checks-1-2-4-summary.json\` / \`checks-1-2-4-report.md\`

## 建议下一步（不自动开干）

1. W0：落地 ComplexityAnalyzer + baseline（只统计/冻结，不立刻 error）
2. ARCH-01：把 Common.Core→InteAssistant.Entitys 抽到 Contracts 后，把清单改成硬失败
3. SCS0006：评估是否可迁到 SHA-256；若为兼容遗留，记豁免理由
`;
fs.writeFileSync(path.join(ev, 'checks-1-2-4-report.md'), md);
console.log(
  JSON.stringify(
    {
      ok: true,
      archTests: '3/3',
      fwOk: archFw && archFw.isSuccessful,
      commonCoreOk: archCc && archCc.isSuccessful,
      failingTypeCount: archCc && archCc.failingTypeCount,
      prHits: archPr && archPr.count,
      security: securitySummary.findingCount,
    },
    null,
    2,
  ),
);
