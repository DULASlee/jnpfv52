// PatternExtractor - DKEE 核心提炼逻辑
// 4 个 Pattern 类型的提炼算法

import {
  DictSourceRecord, DecisionTableSourceRecord, StateMachineSourceRecord,
  FieldNamingPattern, DecisionRulePattern, StateMachinePattern, ProcessPattern,
  AnyPattern, IndustryType, ExtractionResult, PatternSource
} from './PatternTypes';
import { IDKEEQueries } from './PatternQueries';
import { PatternScorer } from './PatternScorer';

// =====================================================
// 提炼阈值(至少出现 N 个项目才算 Pattern)
// =====================================================
const DEFAULT_FREQUENCY_THRESHOLD = 2;  // 至少 2 个项目

// =====================================================
// PatternExtractor
// =====================================================
export class PatternExtractor {
  private scorer = new PatternScorer();

  constructor(
    private queries: IDKEEQueries,
    private frequencyThreshold: number = DEFAULT_FREQUENCY_THRESHOLD
  ) {}

  // ============================================================
  // 主入口:提炼所有 4 种 Pattern
  // ============================================================
  async extractAll(industry: IndustryType, source: PatternSource = 'human-created'): Promise<ExtractionResult> {
    const startTime = Date.now();
    const newPatterns: AnyPattern[] = [];
    const updatedPatterns: AnyPattern[] = [];

    // 1. 字段命名 Pattern
    const dictResult = await this.extractFieldNamingPatterns(industry, source);
    newPatterns.push(...dictResult.newPatterns);
    updatedPatterns.push(...dictResult.updatedPatterns);

    // 2. 业务规则 Pattern
    const dtResult = await this.extractDecisionRulePatterns(industry, source);
    newPatterns.push(...dtResult.newPatterns);
    updatedPatterns.push(...dtResult.updatedPatterns);

    // 3. 状态机 Pattern
    const smResult = await this.extractStateMachinePatterns(industry, source);
    newPatterns.push(...smResult.newPatterns);
    updatedPatterns.push(...smResult.updatedPatterns);

    const duration = Date.now() - startTime;
    return {
      industry,
      patternsExtracted: newPatterns.length + updatedPatterns.length,
      patternsSaved: newPatterns.length,
      patternsUpdated: updatedPatterns.length,
      newPatterns,
      updatedPatterns,
      durationMs: duration,
    };
  }

  // ============================================================
  // 1. 字段命名 Pattern
  //    跨项目聚合:找出现频率 >= threshold 的字段名
  // ============================================================
  async extractFieldNamingPatterns(industry: IndustryType, source: PatternSource): Promise<{
    newPatterns: FieldNamingPattern[];
    updatedPatterns: FieldNamingPattern[];
  }> {
    const records = await this.queries.fetchHighQualityDictRecords(industry);
    if (records.length === 0) return { newPatterns: [], updatedPatterns: [] };

    // Step 1: 统计每个字段名在多少个项目里出现
    const fieldStats = new Map<string, {
      type: string;
      count: number;
      projects: Set<number>;
      isFK: boolean;
      isRequired: boolean;
      refEntity?: string;
    }>();

    records.forEach(record => {
      record.elements.forEach(elem => {
        if (!fieldStats.has(elem.name)) {
          fieldStats.set(elem.name, {
            type: elem.type,
            count: 0,
            projects: new Set(),
            isFK: elem.isFK || false,
            isRequired: elem.isRequired || false,
            refEntity: elem.refEntity,
          });
        }
        const stat = fieldStats.get(elem.name)!;
        stat.count++;
        stat.projects.add(record.project_id);
      });
    });

    // Step 2: 过滤高频字段(>= threshold 个项目)
    const commonFields = Array.from(fieldStats.entries())
      .filter(([_, stat]) => stat.projects.size >= this.frequencyThreshold)
      .map(([name, stat]) => ({
        name,
        type: stat.type,
        frequency: stat.projects.size,
        isFK: stat.isFK,
        isRequired: stat.isRequired,
        refEntity: stat.refEntity,
      }))
      .sort((a, b) => b.frequency - a.frequency);

    if (commonFields.length === 0) return { newPatterns: [], updatedPatterns: [] };

    // Step 3: 构造 Pattern
    const sourceProjects = Array.from(new Set(records.map(r => r.project_id)));
    const sourceRecords = records.map(r => ({
      saTable: 'sa_data_dictionary',
      recordId: r.id,
      version: 1,
    }));

    const pattern: FieldNamingPattern = {
      type: 'field_naming',
      industry,
      source,
      sourceProjects,
      sourceRecords,
      commonFields,
      fieldCount: commonFields.length,
      minOccurrenceThreshold: this.frequencyThreshold,
      patternTags: [`${industry}-字段标准`],
    };

    // Step 4: 评分 + 保存
    const score = this.scorer.score({
      usageCount: sourceProjects.length,
      successRate: 1.0,  // 初始为 1.0,使用后根据 is_success 更新
      source,
      crossIndustryCount: 0,
      recencyScore: 1.0,
    });

    // 检查是否已存在
    const existing = await this.queries.fetchExistingPatterns(industry, 'field_naming');
    if (existing.length > 0) {
      // 更新已有的
      const id = existing[0].id;
      await this.queries.updatePatternScore(id, score, sourceProjects.length, sourceProjects.length);
      return { newPatterns: [], updatedPatterns: [pattern] };
    }

    // 新建
    const { id } = await this.queries.savePattern(pattern, score, sourceProjects);
    return { newPatterns: [{ ...pattern, id }], updatedPatterns: [] };
  }

  // ============================================================
  // 2. 业务规则 Pattern
  //    跨项目聚合:找出一致条件的判定规则
  // ============================================================
  async extractDecisionRulePatterns(industry: IndustryType, source: PatternSource): Promise<{
    newPatterns: DecisionRulePattern[];
    updatedPatterns: DecisionRulePattern[];
  }> {
    const records = await this.queries.fetchHighQualityDecisionTableRecords(industry);
    if (records.length === 0) return { newPatterns: [], updatedPatterns: [] };

    // Step 1: 聚合所有 (condition, operator, value, action) 元组
    const ruleStats = new Map<string, {
      condition: string;
      operator: string;
      value: any;
      action: string;
      count: number;
      projects: Set<number>;
    }>();

    records.forEach(record => {
      record.tables.forEach(table => {
        // 把每条 rule 拆成 (条件 → 动作) 元组
        table.rules?.forEach((rule: any, ruleIdx: number) => {
          const action = table.actions[rule.actionIndex];
          if (!action) return;
          table.conditions.forEach((cond, condIdx) => {
            if (rule.conditionMask[condIdx]) {
              const key = `${cond.name}|${cond.operator}|${JSON.stringify(cond.value)}|${action.name}`;
              if (!ruleStats.has(key)) {
                ruleStats.set(key, {
                  condition: cond.name,
                  operator: cond.operator,
                  value: cond.value,
                  action: action.name,
                  count: 0,
                  projects: new Set(),
                });
              }
              const stat = ruleStats.get(key)!;
              stat.count++;
              stat.projects.add(record.project_id);
            }
          });
        });
      });
    });

    // Step 2: 过滤高频规则
    const ruleSet = Array.from(ruleStats.values())
      .filter(stat => stat.projects.size >= this.frequencyThreshold)
      .sort((a, b) => b.count - a.count)
      .map(stat => ({
        condition: stat.condition,
        operator: stat.operator,
        threshold: stat.value,
        action: stat.action,
        frequency: stat.projects.size,
      }));

    if (ruleSet.length === 0) return { newPatterns: [], updatedPatterns: [] };

    // Step 3: 检查是否有兜底规则
    const hasDefaultRule = records.some(r =>
      r.tables.some(t => t.rules?.some((rule: any) =>
        rule.conditionMask.every((m: boolean) => !m)  // 所有条件都为假 → 默认规则
      ))
    );

    const sourceProjects = Array.from(new Set(records.map(r => r.project_id)));
    const sourceRecords = records.map(r => ({
      saTable: 'sa_decision_table',
      recordId: r.id,
      version: 1,
    }));

    const pattern: DecisionRulePattern = {
      type: 'decision_rule',
      industry,
      source,
      sourceProjects,
      sourceRecords,
      ruleSet,
      hasDefaultRule,
      ruleCount: ruleSet.length,
      patternTags: [`${industry}-业务规则`],
    };

    const score = this.scorer.score({
      usageCount: sourceProjects.length,
      successRate: 1.0,
      source,
      crossIndustryCount: 0,
      recencyScore: 1.0,
    });

    const existing = await this.queries.fetchExistingPatterns(industry, 'decision_rule');
    if (existing.length > 0) {
      const id = existing[0].id;
      await this.queries.updatePatternScore(id, score, sourceProjects.length, sourceProjects.length);
      return { newPatterns: [], updatedPatterns: [pattern] };
    }

    const { id } = await this.queries.savePattern(pattern, score, sourceProjects);
    return { newPatterns: [{ ...pattern, id }], updatedPatterns: [] };
  }

  // ============================================================
  // 3. 状态机 Pattern
  // ============================================================
  async extractStateMachinePatterns(industry: IndustryType, source: PatternSource): Promise<{
    newPatterns: StateMachinePattern[];
    updatedPatterns: StateMachinePattern[];
  }> {
    const records = await this.queries.fetchHighQualityStateMachineRecords(industry);
    if (records.length === 0) return { newPatterns: [], updatedPatterns: [] };

    // Step 1: 按 entity 分组
    const entityStats = new Map<string, {
      states: Set<string>;
      transitions: Map<string, { from: string; to: string; trigger: string; frequency: number }>;
      projects: Set<number>;
    }>();

    records.forEach(record => {
      record.state_machines.forEach(sm => {
        if (!entityStats.has(sm.entity)) {
          entityStats.set(sm.entity, {
            states: new Set(),
            transitions: new Map(),
            projects: new Set(),
          });
        }
        const stat = entityStats.get(sm.entity)!;
        sm.states.forEach(s => stat.states.add(s));
        sm.transitions.forEach(t => {
          const key = `${t.from}→${t.to}@${t.trigger}`;
          if (!stat.transitions.has(key)) {
            stat.transitions.set(key, { from: t.from, to: t.to, trigger: t.trigger, frequency: 0 });
          }
          stat.transitions.get(key)!.frequency++;
        });
        stat.projects.add(record.project_id);
      });
    });

    // Step 2: 只保留跨项目的稳定状态机
    const newPatterns: StateMachinePattern[] = [];

    for (const [entity, stat] of entityStats) {
      if (stat.projects.size < this.frequencyThreshold) continue;

      const standardTransitions = Array.from(stat.transitions.values())
        .filter(t => t.frequency >= this.frequencyThreshold)
        .sort((a, b) => b.frequency - a.frequency);

      const sourceProjects = Array.from(stat.projects);
      const sourceRecords = records
        .filter(r => r.state_machines.some(sm => sm.entity === entity))
        .map(r => ({ saTable: 'sa_state_machine', recordId: r.id, version: 1 }));

      const pattern: StateMachinePattern = {
        type: 'state_machine',
        industry,
        source,
        sourceProjects,
        sourceRecords,
        entity,
        standardStates: Array.from(stat.states),
        standardTransitions,
        patternTags: [`${industry}-状态机`, entity],
      };

      const score = this.scorer.score({
        usageCount: sourceProjects.length,
        successRate: 1.0,
        source,
        crossIndustryCount: 0,
        recencyScore: 1.0,
      });

      const existing = await this.queries.fetchExistingPatterns(industry, 'state_machine');
      if (existing.length > 0) {
        const id = existing[0].id;
        await this.queries.updatePatternScore(id, score, sourceProjects.length, sourceProjects.length);
      } else {
        const { id } = await this.queries.savePattern(pattern, score, sourceProjects);
        newPatterns.push({ ...pattern, id });
      }
    }

    return { newPatterns, updatedPatterns: [] };
  }

  // ============================================================
  // 使用 Pattern 后,根据 Validator 结果更新评分
  // ============================================================
  async recordUsage(patternId: number, projectId: number, isSuccess: boolean, context: string): Promise<void> {
    await this.queries.logPatternUsage(patternId, projectId, isSuccess, context);
  }
}
