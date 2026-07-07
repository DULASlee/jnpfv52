/**
 * SqlServerSADatabase — SA 九步产出持久化到 SQL Server
 *
 * 环境变量：
 *   SA_DB_CONNECTION_STRING — ADO 连接串（与 JNPF ConnectionStrings 相同 Server/Database）
 *   SA_DB_BACKEND=sqlserver   — 启用（server.ts createDatabase）
 */

import sql from 'mssql';
import {
  ISADatabase, SAContext, ScopeOutput, DFDOutput, BPMOutput, DictOutput,
  PSpecOutput, DecisionTableOutput, EROutput, StateMachineOutput, UIOutput,
  ValidationLogRecord, KGPattern, DomainModelContext,
} from './orchestrator-types';

export class SqlServerSADatabase implements ISADatabase {
  private pool: sql.ConnectionPool;
  private connecting: Promise<void> | null = null;

  constructor(connectionString: string) {
    this.pool = new sql.ConnectionPool(connectionString);
  }

  private async ensureConnected(): Promise<void> {
    if (this.pool.connected) return;
    if (!this.connecting) {
      this.connecting = this.pool.connect().then(() => undefined);
    }
    await this.connecting;
  }

  /** 三元组落库参数（tenant_id + project_id + pipeline_id） */
  private bindTriple(req: sql.Request, ctx: SAContext): sql.Request {
    const pipelineId = ctx.pipelineId ?? ctx.projectId;
    return req
      .input('tenant_id', sql.NVarChar(50), ctx.tenantId)
      .input('project_id', sql.BigInt, ctx.projectId)
      .input('pipeline_id', sql.BigInt, pipelineId);
  }

  async saveScope(scope: ScopeOutput, ctx: SAContext): Promise<{ id: number }> {
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('system_boundary', sql.NVarChar(sql.MAX), JSON.stringify(scope.systemBoundary ?? {}))
      .input('external_entities', sql.NVarChar(sql.MAX), JSON.stringify(scope.externalEntities ?? []))
      .input('business_events', sql.NVarChar(sql.MAX), JSON.stringify(scope.businessEvents ?? []))
      .input('event_count', sql.Int, scope.eventCount ?? 0)
      .input('created_by', sql.NVarChar(50), ctx.userId ?? 'sa-service')
      .query(`
        INSERT INTO sa_scope (tenant_id, project_id, pipeline_id, asset_level, system_boundary, external_entities, business_events, event_count, created_by)
        OUTPUT INSERTED.id
        VALUES (@tenant_id, @project_id, @pipeline_id, 'PROJECT', @system_boundary, @external_entities, @business_events, @event_count, @created_by)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async saveDFD(dfd: DFDOutput, ctx: SAContext, scopeId: number): Promise<{ id: number }> {
    await this.assertScope(scopeId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('scope_id', sql.BigInt, scopeId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(dfd))
      .query(`
        INSERT INTO sa_dfd (tenant_id, project_id, pipeline_id, scope_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @scope_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async saveBPM(bpm: BPMOutput, ctx: SAContext, dfdId: number): Promise<{ id: number }> {
    await this.assertDfd(dfdId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('dfd_id', sql.BigInt, dfdId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(bpm))
      .query(`
        INSERT INTO sa_business_process (tenant_id, project_id, pipeline_id, dfd_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @dfd_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async saveDict(dict: DictOutput, ctx: SAContext, dfdId: number, bpmId: number): Promise<{ id: number }> {
    await this.assertDfd(dfdId);
    await this.assertBpm(bpmId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('dfd_id', sql.BigInt, dfdId)
      .input('bpm_id', sql.BigInt, bpmId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(dict))
      .query(`
        INSERT INTO sa_data_dictionary (tenant_id, project_id, pipeline_id, dfd_id, bpm_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @dfd_id, @bpm_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async savePSpec(pspec: PSpecOutput, ctx: SAContext, dictId: number, bpmId: number): Promise<{ id: number }> {
    await this.assertDict(dictId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('event_id', sql.BigInt, ctx.currentEventId ?? null)
      .input('dict_id', sql.BigInt, dictId)
      .input('bpm_id', sql.BigInt, bpmId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(pspec))
      .query(`
        INSERT INTO sa_pspec (tenant_id, project_id, pipeline_id, event_id, dict_id, bpm_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @event_id, @dict_id, @bpm_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async saveDecisionTable(dt: DecisionTableOutput, ctx: SAContext, pspecId: number, dictId: number): Promise<{ id: number }> {
    await this.assertDict(dictId);
    if (pspecId !== 0) await this.assertPspec(pspecId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('event_id', sql.BigInt, ctx.currentEventId ?? null)
      .input('pspec_id', sql.BigInt, pspecId === 0 ? null : pspecId)
      .input('dict_id', sql.BigInt, dictId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(dt))
      .query(`
        INSERT INTO sa_decision_table (tenant_id, project_id, pipeline_id, event_id, pspec_id, dict_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @event_id, @pspec_id, @dict_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async saveER(er: EROutput, ctx: SAContext, dictId: number): Promise<{ id: number }> {
    await this.assertDict(dictId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('dict_id', sql.BigInt, dictId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(er))
      .query(`
        INSERT INTO sa_er (tenant_id, project_id, pipeline_id, dict_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @dict_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async saveStateMachine(sm: StateMachineOutput, ctx: SAContext, dictId: number, bpmId: number): Promise<{ id: number }> {
    await this.assertDict(dictId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('event_id', sql.BigInt, ctx.currentEventId ?? null)
      .input('dict_id', sql.BigInt, dictId)
      .input('bpm_id', sql.BigInt, bpmId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(sm))
      .query(`
        INSERT INTO sa_state_machine (tenant_id, project_id, pipeline_id, event_id, dict_id, bpm_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @event_id, @dict_id, @bpm_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async saveUI(ui: UIOutput, ctx: SAContext, bpmId: number, dictId: number): Promise<{ id: number }> {
    await this.assertBpm(bpmId);
    await this.assertDict(dictId);
    await this.ensureConnected();
    const result = await this.bindTriple(this.pool.request(), ctx)
      .input('event_id', sql.BigInt, ctx.currentEventId ?? null)
      .input('bpm_id', sql.BigInt, bpmId)
      .input('dict_id', sql.BigInt, dictId)
      .input('payload_json', sql.NVarChar(sql.MAX), JSON.stringify(ui))
      .query(`
        INSERT INTO sa_ui (tenant_id, project_id, pipeline_id, event_id, bpm_id, dict_id, payload_json)
        OUTPUT INSERTED.id VALUES (@tenant_id, @project_id, @pipeline_id, @event_id, @bpm_id, @dict_id, @payload_json)
      `);
    return { id: Number(result.recordset[0].id) };
  }

  async logValidation(record: ValidationLogRecord): Promise<void> {
    await this.ensureConnected();
    const pipelineId = record.pipelineId ?? record.projectId;
    await this.pool.request()
      .input('tenant_id', sql.NVarChar(50), record.tenantId)
      .input('project_id', sql.BigInt, record.projectId)
      .input('pipeline_id', sql.BigInt, pipelineId)
      .input('sa_table_name', sql.NVarChar(100), record.saTableName)
      .input('sa_record_id', sql.BigInt, record.saRecordId ?? null)
      .input('validator_name', sql.NVarChar(100), record.validatorName)
      .input('retry_count', sql.Int, record.retryCount)
      .input('validation_status', sql.NVarChar(20), record.validationStatus)
      .input('errors_json', sql.NVarChar(sql.MAX), JSON.stringify(record.errors ?? []))
      .input('duration_ms', sql.Int, record.durationMs)
      .query(`
        INSERT INTO sa_validate_log (tenant_id, project_id, pipeline_id, sa_table_name, sa_record_id, validator_name, retry_count, validation_status, errors_json, duration_ms)
        VALUES (@tenant_id, @project_id, @pipeline_id, @sa_table_name, @sa_record_id, @validator_name, @retry_count, @validation_status, @errors_json, @duration_ms)
      `);
  }

  async getProjectKGPatterns(_projectId: number, _limit = 5): Promise<KGPattern[]> {
    return [];
  }

  async getDomainModel(industry: string): Promise<DomainModelContext> {
    return { industry, standardFields: [], standardEntities: [], standardProcesses: [] };
  }

  async getAllDecisionTablesInProject(projectId: number): Promise<any[]> {
    await this.ensureConnected();
    const result = await this.pool.request()
      .input('project_id', sql.BigInt, projectId)
      .query(`SELECT payload_json FROM sa_decision_table WHERE project_id = @project_id AND is_deleted = 0`);
    return result.recordset.map((r: any) => JSON.parse(r.payload_json));
  }

  private async assertScope(id: number): Promise<void> {
    await this.ensureConnected();
    const r = await this.pool.request().input('id', sql.BigInt, id)
      .query('SELECT 1 AS ok FROM sa_scope WHERE id = @id');
    if (!r.recordset.length) throw new Error(`强外键违规: sa_scope(${id}) 不存在`);
  }

  private async assertDfd(id: number): Promise<void> {
    await this.ensureConnected();
    const r = await this.pool.request().input('id', sql.BigInt, id)
      .query('SELECT 1 AS ok FROM sa_dfd WHERE id = @id');
    if (!r.recordset.length) throw new Error(`强外键违规: sa_dfd(${id}) 不存在`);
  }

  private async assertBpm(id: number): Promise<void> {
    await this.ensureConnected();
    const r = await this.pool.request().input('id', sql.BigInt, id)
      .query('SELECT 1 AS ok FROM sa_business_process WHERE id = @id');
    if (!r.recordset.length) throw new Error(`强外键违规: sa_bpm(${id}) 不存在`);
  }

  private async assertDict(id: number): Promise<void> {
    await this.ensureConnected();
    const r = await this.pool.request().input('id', sql.BigInt, id)
      .query('SELECT 1 AS ok FROM sa_data_dictionary WHERE id = @id');
    if (!r.recordset.length) throw new Error(`强外键违规: sa_dict(${id}) 不存在`);
  }

  private async assertPspec(id: number): Promise<void> {
    await this.ensureConnected();
    const r = await this.pool.request().input('id', sql.BigInt, id)
      .query('SELECT 1 AS ok FROM sa_pspec WHERE id = @id');
    if (!r.recordset.length) throw new Error(`强外键违规: sa_pspec(${id}) 不存在`);
  }
}
