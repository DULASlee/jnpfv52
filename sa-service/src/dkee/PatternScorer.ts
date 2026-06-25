// PatternScorer - DKEE 评分公式实现
// score = w1*usage + w2*success + w3*source + w4*crossIndustry + w5*recency

import { PatternSource } from './PatternTypes';

export interface ScoreInput {
  usageCount: number;
  successRate: number;
  source: PatternSource;
  crossIndustryCount: number;
  recencyScore: number;
}

export class PatternScorer {
  // 权重(可配置)
  private weights = {
    usage: 0.30,
    success: 0.25,
    source: 0.20,
    crossIndustry: 0.15,
    recency: 0.10,
  };

  // 来源加权
  private sourceWeights: Record<PatternSource, number> = {
    'human-created': 1.0,
    'ai-discovered': 0.8,
    'self-play': 0.6,
  };

  score(input: ScoreInput): number {
    const usageScore = Math.min(Math.log(1 + input.usageCount) / Math.log(11), 1.0);  // 对数增长，10 个项目 ≈ 满分
    const successScore = input.successRate;
    const sourceScore = this.sourceWeights[input.source] || 0.5;
    const crossIndustryScore = Math.min(input.crossIndustryCount / 3, 1.0);  // 3 个行业 = 满分

    const raw =
      this.weights.usage * usageScore +
      this.weights.success * successScore +
      this.weights.source * sourceScore +
      this.weights.crossIndustry * crossIndustryScore +
      this.weights.recency * input.recencyScore;

    return Math.round(raw * 100) / 100;  // 保留 2 位小数
  }

  recencyScore(createdAt: Date): number {
    const daysSinceCreation = (Date.now() - createdAt.getTime()) / (1000 * 60 * 60 * 24);
    // 半衰期 90 天
    return Math.pow(0.5, daysSinceCreation / 180);
  }
}
