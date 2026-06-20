// DKEE Pattern 查询接口 + SQL Server 实现 + 内存实现

import {
  DictSourceRecord, DecisionTableSourceRecord, StateMachineSourceRecord,
  PatternType, AnyPattern, IndustryType
} from './PatternTypes';

// =====================================================
// DKEE 查询接口
// =====================================================
export interface IDKEEQueries {
  // 源数据查询(从 SA 表拉)
  fetchHighQualityDictRecords(industry: IndustryType, projectId?: number): Promise<DictSourceRecord[]>;
  fetchHighQualityDecisionTableRecords(industry: IndustryType, projectId?: number): Promise<DecisionTableSourceRecord[]>;
  fetchHighQualityStateMachineRecords(industry: IndustryType, projectId?: number): Promise<StateMachineSourceRecord[]>;

  // Pattern CRUD
  fetchExistingPatterns(industry: IndustryType, type: PatternType): Promise<any[]>;
  savePattern(pattern: AnyPattern, score: number, sourceProjects: number[]): Promise<{ id: number }>;
  updatePatternScore(patternId: number, score: number, usageDelta: number, successDelta: number): Promise<void>;

  // 使用日志
  logPatternUsage(patternId: number, projectId: number, isSuccess: boolean, context: string): Promise<void>;
}

// =====================================================
// SQL Server 实现骨架(用户需根据实际 ORM 调整)
// =====================================================
export class SqlServerDKEEQueries implements IDKEEQueries {
  constructor(private connection: any) {}  // mssql.ConnectionPool

  /** 转义 LIKE 通配符，防止注入绕过 */
  private escapeLike(value: string): string {
    return value.replace(/[%_\[\]]/g, '[$&]');
  }

  async fetchHighQualityDictRecords(industry: IndustryType, projectId?: number): Promise<DictSourceRecord[]> {
    // SQL Server 用 tags LIKE 匹配 industry
    const projectFilter = projectId ? 'AND d.project_id = @projectId' : '';
    const query = `
      SELECT d.id, d.project_id, d.elements, d.data_flows, d.data_stores, d.tags, d.pattern_tags
      FROM sa_data_dictionary d
      INNER JOIN sa_scope s ON s.project_id = d.project_id
      WHERE d.validation_status = 'PASS'
        AND d.human_confirmed = 1
        AND d.is_pattern_source = 1
        AND d.is_deleted = 0
        AND d.is_current = 1
        AND s.is_current = 1
        AND s.tags LIKE @industryPattern
        ${projectFilter}
      ORDER BY d.project_id, d.id
    `;
    const result = await this.connection.request()
      .input('industryPattern', `%${this.escapeLike(industry)}%`)
      .input('projectId', projectId || 0)
      .query(query);
    return result.recordset;
  }

  async fetchHighQualityDecisionTableRecords(industry: IndustryType, projectId?: number): Promise<DecisionTableSourceRecord[]> {
    const projectFilter = projectId ? 'AND d.project_id = @projectId' : '';
    const query = `
      SELECT d.id, d.project_id, d.tables, d.cross_event_consistency
      FROM sa_decision_table d
      INNER JOIN sa_scope s ON s.project_id = d.project_id
      WHERE d.validation_status = 'PASS'
        AND d.human_confirmed = 1
        AND d.is_pattern_source = 1
        AND d.cross_event_consistency = 1
        AND d.is_deleted = 0
        AND d.is_current = 1
        AND s.tags LIKE @industryPattern
        ${projectFilter}
      ORDER BY d.project_id, d.id
    `;
    const result = await this.connection.request()
      .input('industryPattern', `%${this.escapeLike(industry)}%`)
      .input('projectId', projectId || 0)
      .query(query);
    return result.recordset;
  }

  async fetchHighQualityStateMachineRecords(industry: IndustryType, projectId?: number): Promise<StateMachineSourceRecord[]> {
    const projectFilter = projectId ? 'AND d.project_id = @projectId' : '';
    const query = `
      SELECT d.id, d.project_id, d.state_machines, d.states_in_dict
      FROM sa_state_machine d
      INNER JOIN sa_scope s ON s.project_id = d.project_id
      WHERE d.validation_status = 'PASS'
        AND d.human_confirmed = 1
        AND d.is_pattern_source = 1
        AND d.states_in_dict = 1
        AND d.is_deleted = 0
        AND d.is_current = 1
        AND s.tags LIKE @industryPattern
        ${projectFilter}
      ORDER BY d.project_id, d.id
    `;
    const result = await this.connection.request()
      .input('industryPattern', `%${this.escapeLike(industry)}%`)
      .input('projectId', projectId || 0)
      .query(query);
    return result.recordset;
  }

  async fetchExistingPatterns(industry: IndustryType, type: PatternType): Promise<any[]> {
    const query = `
      SELECT id, pattern_type, pattern_content, source_projects, score, usage_count
      FROM kg_pattern
      WHERE industry = @industry
        AND pattern_type = @type
        AND is_active = 1
        AND deprecated_at IS NULL
    `;
    const result = await this.connection.request()
      .input('industry', industry)
      .input('type', type)
      .query(query);
    return result.recordset;
  }

  async savePattern(pattern: AnyPattern, score: number, sourceProjects: number[]): Promise<{ id: number }> {
    const query = `
      INSERT INTO kg_pattern (pattern_type, industry, pattern_content, pattern_tags, score, source_projects, source)
      OUTPUT INSERTED.id
      VALUES (@type, @industry, @content, @tags, @score, @projects, @source)
    `;
    const result = await this.connection.request()
      .input('type', pattern.type)
      .input('industry', pattern.industry)
      .input('content', JSON.stringify(pattern))
      .input('tags', JSON.stringify(pattern.patternTags || []))
      .input('score', score)
      .input('projects', JSON.stringify(sourceProjects))
      .input('source', pattern.source)
      .query(query);
    return { id: result.recordset[0].id };
  }

  async updatePatternScore(patternId: number, score: number, usageDelta: number, successDelta: number): Promise<void> {
    const query = `
      UPDATE kg_pattern
      SET score = @score,
          usage_count = usage_count + @usageDelta,
          success_count = success_count + @successDelta,
          last_score_at = GETDATE()
      WHERE id = @id
    `;
    await this.connection.request()
      .input('id', patternId)
      .input('score', score)
      .input('usageDelta', usageDelta)
      .input('successDelta', successDelta)
      .query(query);
  }

  async logPatternUsage(patternId: number, projectId: number, isSuccess: boolean, context: string): Promise<void> {
    const query = `
      INSERT INTO kg_pattern_usage (pattern_id, project_id, is_success, context_info)
      VALUES (@patternId, @projectId, @isSuccess, @context)
    `;
    await this.connection.request()
      .input('patternId', patternId)
      .input('projectId', projectId)
      .input('isSuccess', isSuccess ? 1 : 0)
      .input('context', context)
      .query(query);
  }
}


// =====================================================
// InMemory 实现(用于测试)
// =====================================================
export class InMemoryDKEEQueries implements IDKEEQueries {
  private dictRecords: DictSourceRecord[] = [];
  private decisionTableRecords: DecisionTableSourceRecord[] = [];
  private stateMachineRecords: StateMachineSourceRecord[] = [];
  private patterns: any[] = [];
  private usageLogs: any[] = [];
  private nextPatternId = 1;

  // 注入测试数据
  injectDictRecords(records: DictSourceRecord[]): void {
    this.dictRecords = records;
  }
  injectDecisionTableRecords(records: DecisionTableSourceRecord[]): void {
    this.decisionTableRecords = records;
  }
  injectStateMachineRecords(records: StateMachineSourceRecord[]): void {
    this.stateMachineRecords = records;
  }

  async fetchHighQualityDictRecords(industry: IndustryType, projectId?: number): Promise<DictSourceRecord[]> {
    return this.dictRecords.filter(r => !projectId || r.project_id === projectId);
  }

  async fetchHighQualityDecisionTableRecords(industry: IndustryType, projectId?: number): Promise<DecisionTableSourceRecord[]> {
    return this.decisionTableRecords.filter(r => !projectId || r.project_id === projectId);
  }

  async fetchHighQualityStateMachineRecords(industry: IndustryType, projectId?: number): Promise<StateMachineSourceRecord[]> {
    return this.stateMachineRecords.filter(r => !projectId || r.project_id === projectId);
  }

  async fetchExistingPatterns(industry: IndustryType, type: PatternType): Promise<any[]> {
    return this.patterns.filter(p => p.industry === industry && p.pattern_type === type);
  }

  async savePattern(pattern: AnyPattern, score: number, sourceProjects: number[]): Promise<{ id: number }> {
    const id = this.nextPatternId++;
    this.patterns.push({
      id,
      pattern_type: pattern.type,
      industry: pattern.industry,
      pattern_content: JSON.stringify(pattern),
      score,
      source_projects: JSON.stringify(sourceProjects),
      usage_count: 0,
      success_count: 0,
    });
    return { id };
  }

  async updatePatternScore(patternId: number, score: number, usageDelta: number, successDelta: number): Promise<void> {
    const p = this.patterns.find(p => p.id === patternId);
    if (p) {
      p.score = score;
      p.usage_count += usageDelta;
      p.success_count += successDelta;
    }
  }

  async logPatternUsage(patternId: number, projectId: number, isSuccess: boolean, context: string): Promise<void> {
    this.usageLogs.push({ patternId, projectId, isSuccess, context, at: new Date() });
  }

  // 测试辅助
  getAllPatterns(): any[] {
    return this.patterns;
  }
  getAllUsageLogs(): any[] {
    return this.usageLogs;
  }
}
