// DKEE - 完整导出
// 4 个核心组件:PatternTypes / PatternQueries / PatternExtractor / PatternRankingService

export {
  PatternType, PatternSource, IndustryType,
  BasePattern, FieldNamingPattern, DecisionRulePattern, StateMachinePattern, ProcessPattern, AnyPattern,
  ExtractionResult,
  DictSourceRecord, DecisionTableSourceRecord, StateMachineSourceRecord
} from './PatternTypes';

export { IDKEEQueries, SqlServerDKEEQueries, InMemoryDKEEQueries } from './PatternQueries';
export { PatternExtractor } from './PatternExtractor';
export { PatternScorer } from './PatternScorer';
export { PatternRankingService } from './PatternRankingService';

// =====================================================
// DKEEFacade - 统一入口(给 SAOrchestrator 调用)
// =====================================================
import { ISADatabase } from '../types';
import { IDKEEQueries, InMemoryDKEEQueries } from './PatternQueries';
import { PatternExtractor } from './PatternExtractor';
import { PatternRankingService } from './PatternRankingService';
import { IndustryType, ExtractionResult } from './PatternTypes';

export class DKEEFacade {
  public queries: IDKEEQueries;
  public extractor: PatternExtractor;
  public ranker: PatternRankingService;

  constructor(queries?: IDKEEQueries) {
    this.queries = queries || new InMemoryDKEEQueries();
    this.extractor = new PatternExtractor(this.queries);
    this.ranker = new PatternRankingService(this.queries);
  }

  /**
   * 在 SA 流水线跑完后调用
   */
  async extractAndScore(industry: IndustryType): Promise<ExtractionResult> {
    return await this.extractor.extractAll(industry);
  }

  /**
   * 给下个 SA 流水线注入 Top Pattern
   */
  async getTopPatternsForContext(industry: IndustryType, topN: number = 5): Promise<any[]> {
    return await this.ranker.getTopPatternsForContext(industry, ['field_naming', 'decision_rule', 'state_machine', 'process_pattern'], topN);
  }
}
