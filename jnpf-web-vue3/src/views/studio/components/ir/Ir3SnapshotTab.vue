<template>
  <div class="ir3-snapshot-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">⚙️</span>
      <p>开发 Skill 产出后，IR-3（生成代码 / 测试套件 / 架构报告）将在此展示</p>
    </div>
    <template v-else>
      <div v-if="summaryBanner.visible" class="ir3-summary-banner">
        <a-tag :color="summaryBanner.codegenColor">{{ summaryBanner.codegenLabel }}</a-tag>
        <span class="banner-item">
          sandboxBuild:
          <a-tag size="small" :color="summaryBanner.sandboxPassed ? 'success' : 'default'">
            {{ summaryBanner.sandboxPassed ? 'PASS' : '—' }}
          </a-tag>
        </span>
        <span class="banner-item">TestSuite: {{ summaryBanner.scenarioCount }} 场景</span>
        <span v-if="summaryBanner.archCritical > 0" class="banner-item warn"> Arch Critical: {{ summaryBanner.archCritical }} </span>
      </div>
      <a-tabs v-model:activeKey="activeIr3Tab" size="small" class="ir3-sub-tabs">
        <a-tab-pane v-for="tab in ir3Tabs" :key="tab.key" :tab="tab.label">
          <div v-if="!tab.snapshot" class="tab-empty compact">
            <p>{{ tab.emptyHint }}</p>
          </div>
          <div v-else class="ir3-detail">
            <div class="detail-header">
              <code>{{ tab.snapshot.fragmentId }}</code>
              <a-tag :color="stabilityColor(tab.snapshot.stabilityState)">
                {{ tab.snapshot.stabilityState }}
              </a-tag>
            </div>
            <div class="detail-meta"> {{ tab.snapshot.fragmentType }} · v{{ tab.snapshot.currentVersion }} </div>

            <!-- IR3_GeneratedCode：结构化展示 codegen + sandbox + promote 门禁 -->
            <template v-if="tab.key === 'codegen'">
              <div class="kv-grid">
                <div class="kv">
                  <span class="k">sandboxBuild.passed</span>
                  <a-tag :color="codegenFields.sandboxPassed ? 'success' : 'default'">
                    {{ codegenFields.sandboxPassed ? 'true' : 'false' }}
                  </a-tag>
                </div>
                <div class="kv">
                  <span class="k">nameSpace.className</span>
                  <code class="v">{{ codegenFields.nameSpace }}.{{ codegenFields.className }}</code>
                </div>
                <div class="kv">
                  <span class="k">templateProfileId</span>
                  <code class="v">{{ codegenFields.templateProfileId || '—' }}</code>
                </div>
                <div class="kv">
                  <span class="k">artifactRoot</span>
                  <code class="v">{{ codegenFields.artifactRoot || '—' }}</code>
                </div>
              </div>

              <div v-if="codegenFields.templateVersions.length" class="sub-section">
                <div class="sub-title">templateVersions ({{ codegenFields.templateVersions.length }})</div>
                <div class="tv-list">
                  <div v-for="tv in codegenFields.templateVersions" :key="tv.templateId" class="tv-item">
                    <code class="tv-id">{{ tv.templateId }}</code>
                    <span class="tv-path" :title="tv.renderedPath">{{ tv.renderedPath }}</span>
                    <code class="tv-sha" :title="tv.sha256">{{ tv.sha256?.slice(0, 12) }}…</code>
                  </div>
                </div>
              </div>

              <div v-if="codegenFields.promotionGate" class="sub-section">
                <div class="sub-title">promotionGate</div>
                <div class="kv-grid">
                  <div class="kv">
                    <span class="k">sandboxBuild</span>
                    <a-tag :color="codegenFields.promotionGate.sandboxBuild ? 'success' : 'error'">
                      {{ codegenFields.promotionGate.sandboxBuild }}
                    </a-tag>
                  </div>
                  <div class="kv">
                    <span class="k">archGuardCritical</span>
                    <span class="v">{{ codegenFields.promotionGate.archGuardCritical }}</span>
                  </div>
                  <div class="kv">
                    <span class="k">archGuardWarnings</span>
                    <span class="v">{{ codegenFields.promotionGate.archGuardWarnings }}</span>
                  </div>
                  <div class="kv">
                    <span class="k">sandboxElapsedMs</span>
                    <span class="v">{{ codegenFields.promotionGate.sandboxElapsedMs ?? '—' }}</span>
                  </div>
                </div>
                <div v-if="codegenFields.promotedAt" class="promote-time"> promotedAt: {{ codegenFields.promotedAt }} </div>
              </div>
            </template>

            <!-- IR3_TestSuite：结构化展示场景列表 -->
            <template v-else-if="tab.key === 'testsuite'">
              <div class="kv-grid">
                <div class="kv">
                  <span class="k">scenarioCount</span>
                  <span class="v strong">{{ testSuiteFields.scenarioCount }}</span>
                </div>
                <div class="kv">
                  <span class="k">derivationMode</span>
                  <code class="v">{{ testSuiteFields.derivationMode || '—' }}</code>
                </div>
                <div class="kv">
                  <span class="k">runId</span>
                  <code class="v">{{ testSuiteFields.runId || '—' }}</code>
                </div>
                <div class="kv">
                  <span class="k">derivedAt</span>
                  <span class="v">{{ testSuiteFields.derivedAt || '—' }}</span>
                </div>
              </div>

              <div v-if="testSuiteFields.scenarios.length" class="sub-section">
                <div class="sub-title">scenarios ({{ testSuiteFields.scenarios.length }})</div>
                <div class="scenario-list">
                  <div v-for="sc in testSuiteFields.scenarios" :key="sc.caseId" class="scenario-item">
                    <div class="sc-head">
                      <code class="sc-id">{{ sc.caseId }}</code>
                      <a-tag size="small">{{ sc.kind }}</a-tag>
                      <code v-if="sc.rule" class="sc-rule">{{ sc.rule }}</code>
                    </div>
                    <div class="sc-desc">{{ sc.description }}</div>
                  </div>
                </div>
              </div>

              <div v-if="testSuiteFields.archGuardWarnings.length" class="sub-section">
                <div class="sub-title">archGuardWarnings ({{ testSuiteFields.archGuardWarnings.length }})</div>
                <div class="warn-list">
                  <div v-for="(w, idx) in testSuiteFields.archGuardWarnings" :key="idx" class="warn-item">
                    <code>{{ w.RuleId || w.ruleId }}</code>
                    <span>{{ w.Message || w.message }}</span>
                  </div>
                </div>
              </div>
            </template>

            <!-- IR3_ArchReport：违规列表 -->
            <template v-else-if="tab.key === 'arch'">
              <div class="kv-grid">
                <div class="kv">
                  <span class="k">criticalCount</span>
                  <a-tag :color="archFields.criticalCount > 0 ? 'error' : 'success'">
                    {{ archFields.criticalCount }}
                  </a-tag>
                </div>
                <div class="kv">
                  <span class="k">warningCount</span>
                  <a-tag :color="archFields.warningCount > 0 ? 'warning' : 'default'">
                    {{ archFields.warningCount }}
                  </a-tag>
                </div>
                <div class="kv">
                  <span class="k">checkedAt</span>
                  <span class="v">{{ archFields.checkedAt || '—' }}</span>
                </div>
              </div>

              <div v-if="archFields.violations.length" class="sub-section">
                <div class="sub-title">violations ({{ archFields.violations.length }})</div>
                <div class="violation-list">
                  <div
                    v-for="(v, idx) in archFields.violations"
                    :key="(v.ruleId || '') + idx"
                    class="violation-item"
                    :class="{ critical: (v.severity || '').toLowerCase() === 'critical' }">
                    <div class="v-head">
                      <a-tag :color="(v.severity || '').toLowerCase() === 'critical' ? 'error' : 'warning'">
                        {{ v.severity }}
                      </a-tag>
                      <code>{{ v.ruleId }}</code>
                    </div>
                    <div class="v-msg">{{ v.message }}</div>
                    <code v-if="v.filePath" class="v-path">{{ v.filePath }}</code>
                  </div>
                </div>
              </div>
            </template>

            <!-- 原始 payload 折叠 -->
            <details class="raw-payload">
              <summary>原始 payload (JSON)</summary>
              <pre v-if="tab.snapshot.payload" class="detail-json">{{ formatJson(tab.snapshot.payload) }}</pre>
            </details>
          </div>
        </a-tab-pane>
      </a-tabs>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject, ref } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { IR3_FRAGMENT_TYPES } from '../../types/ir';
  import type { IrFragmentSnapshot } from '../../types/ir';

  // IR-3 片段类型常量（后端 IrFragmentTypes：GeneratedCode/ArchReport/TestSuite）

  interface TemplateVersion {
    templateId: string;
    sha256?: string;
    renderedPath?: string;
  }
  interface PromotionGate {
    sandboxBuild?: boolean;
    sandboxElapsedMs?: number | null;
    archGuardCritical?: number;
    archGuardWarnings?: number;
  }
  interface Scenario {
    caseId: string;
    rule?: string;
    kind?: string;
    description?: string;
  }
  interface ArchWarning {
    RuleId?: string;
    ruleId?: string;
    Message?: string;
    message?: string;
  }
  interface ArchViolation {
    ruleId?: string;
    severity?: string;
    message?: string;
    filePath?: string;
    match?: string;
  }

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const activeIr3Tab = ref('codegen');

  const pipelineId = computed(() => ir.pipelineId.value);

  const ir3Snapshots = computed(() => ir.snapshots.value.filter(s => IR3_FRAGMENT_TYPES.includes(s.fragmentType as (typeof IR3_FRAGMENT_TYPES)[number])));

  function findSnapshot(type: string) {
    return ir3Snapshots.value.find(s => s.fragmentType === type);
  }

  const ir3Tabs = computed(() => [
    {
      key: 'codegen',
      label: 'GeneratedCode',
      snapshot: findSnapshot('IR3_GeneratedCode'),
      emptyHint: '运行 developer-skill 后显示生成代码清单与 sandbox/promote 门禁',
    },
    {
      key: 'testsuite',
      label: 'TestSuite',
      snapshot: findSnapshot('IR3_TestSuite'),
      emptyHint: '代码 promote 后 tester-skill 推导测试场景',
    },
    {
      key: 'arch',
      label: 'ArchReport',
      snapshot: findSnapshot('IR3_ArchReport'),
      emptyHint: '出现 Critical 违规时显示 ArchGuard 报告',
    },
  ]);

  function safeParsePayload(payload: unknown): Record<string, unknown> | null {
    if (payload == null) return null;
    try {
      return (typeof payload === 'string' ? JSON.parse(payload) : payload) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  const summaryBanner = computed(() => {
    const codegenSnap = findSnapshot('IR3_GeneratedCode');
    const testSnap = findSnapshot('IR3_TestSuite');
    const archSnap = findSnapshot('IR3_ArchReport');
    const codegen = safeParsePayload(codegenSnap?.payload);
    const testSuite = safeParsePayload(testSnap?.payload);
    const arch = safeParsePayload(archSnap?.payload);
    const sb = (codegen?.sandboxBuild ?? {}) as Record<string, unknown>;
    const stability = codegenSnap?.stabilityState ?? 'draft';
    const stabilityColorMap: Record<string, string> = {
      draft: 'default',
      'in-progress': 'processing',
      stable: 'success',
      locked: 'warning',
      invalidated: 'error',
    };
    return {
      visible: ir3Snapshots.value.length > 0,
      codegenLabel: codegenSnap ? `GeneratedCode · ${stability}` : 'GeneratedCode · 待生成',
      codegenColor: stabilityColorMap[stability] ?? 'default',
      sandboxPassed: Boolean(sb.passed),
      scenarioCount: Number(testSuite?.scenarioCount ?? 0),
      archCritical: Number(arch?.criticalCount ?? 0),
    };
  });

  // ---- 解析 IR3_GeneratedCode payload ----
  const codegenFields = computed(() => {
    const snap = findSnapshot('IR3_GeneratedCode');
    const empty = {
      sandboxPassed: false,
      nameSpace: '',
      className: '',
      templateProfileId: '',
      artifactRoot: '',
      templateVersions: [] as TemplateVersion[],
      promotionGate: null as PromotionGate | null,
      promotedAt: '',
    };
    if (!snap?.payload) return empty;
    const p = safeParsePayload(snap.payload);
    if (!p) return empty;
    const sb = (p.sandboxBuild ?? {}) as Record<string, unknown>;
    const gate = p.promotionGate ? (p.promotionGate as PromotionGate) : null;
    return {
      sandboxPassed: Boolean(sb.passed),
      nameSpace: String(p.nameSpace ?? ''),
      className: String(p.className ?? ''),
      templateProfileId: String(p.templateProfileId ?? ''),
      artifactRoot: String(p.artifactRoot ?? ''),
      templateVersions: Array.isArray(p.templateVersions) ? (p.templateVersions as TemplateVersion[]) : [],
      promotionGate: gate,
      promotedAt: String(p.promotedAt ?? ''),
    };
  });

  // ---- 解析 IR3_TestSuite payload ----
  const testSuiteFields = computed(() => {
    const snap = findSnapshot('IR3_TestSuite');
    const empty = {
      scenarioCount: 0,
      derivationMode: '',
      runId: '',
      derivedAt: '',
      scenarios: [] as Scenario[],
      archGuardWarnings: [] as ArchWarning[],
    };
    if (!snap?.payload) return empty;
    const p = safeParsePayload(snap.payload);
    if (!p) return empty;
    const meta = (p.metadata ?? {}) as Record<string, unknown>;
    return {
      scenarioCount: Number(p.scenarioCount ?? 0),
      derivationMode: String(p.derivationMode ?? ''),
      runId: String(p.runId ?? ''),
      derivedAt: String(p.derivedAt ?? ''),
      scenarios: Array.isArray(p.scenarios) ? (p.scenarios as Scenario[]) : [],
      archGuardWarnings: Array.isArray(meta.archGuardWarnings) ? (meta.archGuardWarnings as ArchWarning[]) : [],
    };
  });

  // ---- 解析 IR3_ArchReport payload ----
  const archFields = computed(() => {
    const snap = findSnapshot('IR3_ArchReport');
    const empty = { criticalCount: 0, warningCount: 0, checkedAt: '', violations: [] as ArchViolation[] };
    if (!snap?.payload) return empty;
    const p = safeParsePayload(snap.payload);
    if (!p) return empty;
    return {
      criticalCount: Number(p.criticalCount ?? 0),
      warningCount: Number(p.warningCount ?? 0),
      checkedAt: String(p.checkedAt ?? ''),
      violations: Array.isArray(p.violations) ? (p.violations as ArchViolation[]) : [],
    };
  });

  function stabilityColor(state: IrFragmentSnapshot['stabilityState']) {
    const map: Record<string, string> = {
      draft: 'default',
      'in-progress': 'processing',
      stable: 'success',
      locked: 'warning',
      invalidated: 'error',
    };
    return map[state] || 'default';
  }

  function formatJson(payload: unknown) {
    try {
      return JSON.stringify(payload, null, 2);
    } catch {
      return String(payload);
    }
  }
</script>

<style scoped lang="less">
  .ir3-snapshot-tab {
    height: 100%;
    display: flex;
    flex-direction: column;
    overflow: hidden;

    .tab-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #999;
      text-align: center;
      padding: 24px;

      &.compact {
        height: auto;
        padding: 16px;
      }

      .empty-icon {
        font-size: 28px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .ir3-summary-banner {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
      padding: 6px 8px;
      margin-bottom: 6px;
      background: #f6ffed;
      border: 1px solid #b7eb8f;
      border-radius: 4px;
      font-size: 11px;

      .banner-item {
        color: #555;

        &.warn {
          color: #cf1322;
        }
      }
    }

    .ir3-sub-tabs {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;

      :deep(.ant-tabs-content-holder) {
        flex: 1;
        overflow: hidden;
      }

      :deep(.ant-tabs-tabpane) {
        height: 100%;
        overflow-y: auto;
      }
    }

    .ir3-detail {
      padding: 4px 0;

      .detail-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 4px;

        code {
          font-size: 11px;
          background: #f5f5f5;
          padding: 2px 6px;
          border-radius: 3px;
        }
      }

      .detail-meta {
        font-size: 11px;
        color: #666;
        margin-bottom: 8px;
      }

      .kv-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 6px 12px;
        margin-bottom: 10px;
      }

      .kv {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 11px;
        min-width: 0;

        .k {
          color: #888;
          flex-shrink: 0;
        }

        .v {
          font-family: Consolas, monospace;
          color: #333;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;

          &.strong {
            font-weight: 600;
            color: #1890ff;
          }
        }
      }

      .sub-section {
        margin-bottom: 12px;

        .sub-title {
          font-size: 11px;
          color: #888;
          margin-bottom: 4px;
          border-bottom: 1px dashed #e8e8e8;
          padding-bottom: 2px;
        }
      }

      .tv-list {
        .tv-item {
          display: flex;
          align-items: center;
          gap: 8px;
          font-size: 11px;
          padding: 3px 0;
          min-width: 0;

          .tv-id {
            font-family: Consolas, monospace;
            color: #1890ff;
            flex-shrink: 0;
          }

          .tv-path {
            color: #666;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            flex: 1;
            min-width: 0;
          }

          .tv-sha {
            font-family: Consolas, monospace;
            color: #999;
            flex-shrink: 0;
          }
        }
      }

      .promote-time {
        font-size: 10px;
        color: #999;
        margin-top: 4px;
      }

      .scenario-list {
        .scenario-item {
          padding: 6px 8px;
          background: #fafafa;
          border-radius: 4px;
          margin-bottom: 4px;

          .sc-head {
            display: flex;
            align-items: center;
            gap: 6px;
            margin-bottom: 2px;

            .sc-id {
              font-family: Consolas, monospace;
              font-size: 11px;
              color: #1890ff;
            }

            .sc-rule {
              font-size: 10px;
              color: #888;
            }
          }

          .sc-desc {
            font-size: 11px;
            color: #555;
          }
        }
      }

      .warn-list,
      .violation-list {
        .warn-item,
        .violation-item {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 11px;
          padding: 3px 0;
          flex-wrap: wrap;

          code {
            font-family: Consolas, monospace;
            color: #d46b08;
          }
        }

        .violation-item {
          flex-direction: column;
          align-items: flex-start;
          padding: 6px 8px;
          background: #fff2f0;
          border-radius: 4px;
          margin-bottom: 4px;
          gap: 2px;

          .v-head {
            display: flex;
            align-items: center;
            gap: 6px;

            code {
              color: #cf1322;
            }
          }

          .v-msg {
            font-size: 11px;
            color: #555;
          }

          .v-path {
            font-family: Consolas, monospace;
            font-size: 10px;
            color: #999;
          }

          &:not(.critical) {
            background: #fffbe6;
          }
        }
      }

      .raw-payload {
        margin-top: 8px;

        summary {
          font-size: 11px;
          color: #888;
          cursor: pointer;
          user-select: none;
        }
      }

      .detail-json {
        margin: 4px 0 0;
        padding: 8px;
        background: #fafafa;
        border-radius: 4px;
        font-size: 11px;
        max-height: 280px;
        overflow: auto;
        white-space: pre-wrap;
        word-break: break-all;
      }
    }
  }
</style>
