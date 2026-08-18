// PatternRankingService - Pattern 评分动态更新服务
// 当一个 Pattern 被使用时,根据 Validator 结果动态更新评分

import { IDKEEQueries } from './PatternQueries';
import { PatternScorer } from './PatternScorer';
import { AnyPattern, IndustryType, PatternType } from './PatternTypes';

export class PatternRankingService {
  private scorer = new PatternScorer();

  constructor(private queries: IDKEEQueries) {}

  /**
   * 一次 SA 流水线跑完后,更新所有用过的 Pattern 的评分
   */
  async updateScoresAfterUsage(usageLogs: Array<{
    patternId: number;
    projectId: number;
    isSuccess: boolean;
  }>): Promise<void> {
    // 聚合每个 pattern 的使用统计
    const stats = new Map<number, { total: number; success: number }>();
    usageLogs.forEach(log => {
      if (!stats.has(log.patternId)) stats.set(log.patternId, { total: 0, success: 0 });
      const s = stats.get(log.patternId)!;
      s.total++;
      if (log.isSuccess) s.success++;
    });

    // 更新每个 pattern
    for (const [patternId, stat] of stats) {
      const successRate = stat.success / stat.total;
      const existing = await this.fetchPattern(patternId);
      if (!existing) continue;

      const newScore = this.scorer.score({
        usageCount: (existing.usage_count || 0) + stat.total,
        successRate,
        source: existing.source,
        crossIndustryCount: existing.cross_industry_count || 0,
        recencyScore: this.scorer.recencyScore(new Date(existing.created_at)),
      });

      await this.queries.updatePatternScore(patternId, newScore, stat.total, stat.success);

      // 半衰期过期的 pattern 标记 deprecated
      if (newScore < 0.3) {
        console.warn(`[DKEE] Pattern ${patternId} 评分降到 ${newScore},建议人工 review`);
      }
    }
  }

  /**
   * 给 LLM context 注入时,选 Top N Pattern
   */
  async getTopPatternsForContext(
    industry: IndustryType,
    types: PatternType[],
    topN: number = 5
  ): Promise<AnyPattern[]> {
    const allPatterns: any[] = [];
    for (const type of types) {
      const patterns = await this.queries.fetchExistingPatterns(industry, type);
      allPatterns.push(...patterns);
    }

    // 按 score 排序
    return allPatterns
      .filter(p => p.score >= 0.6)  // 评分门禁
      .sort((a, b) => b.score - a.score)
      .slice(0, topN)
      .map(p => JSON.parse(p.pattern_content));
  }

  private async fetchPattern(id: number): Promise<any> {
    // 简化:实际实现用单独 query
    return { id, usage_count: 0, source: 'human-created', cross_industry_count: 0, created_at: new Date() };
  }
}
