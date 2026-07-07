/**
 * ADR-005 交互式澄清问答状态管理
 *
 * 用法：在 AiChatPanel 内 `const clarification = useClarification(pipelineId)`，
 *   - SSE 收到 `clarification_requested` 时调 `setActive(set)`
 *   - ClarificationCard @answered 时调 `onAnswered(setId, triggerNextRound)`
 *   - `triggerNextRound` 为 true 时由调用方重新发起 sa-gate 评估
 */
import { ref } from 'vue';
import { type ClarificationSet, type AnswerClarificationResult, answerClarification } from '/@/views/studio/api/studio/skills';

export function useClarification(pipelineId: () => number) {
  // 当前活跃的提问集合（同一时刻最多一个）
  const activeSet = ref<ClarificationSet | null>(null);
  const submitting = ref(false);
  const lastError = ref('');

  function setActive(set: ClarificationSet | null) {
    activeSet.value = set;
    lastError.value = '';
  }

  async function submit(
    setId: string,
    answers: ClarificationSet['questions'],
    payload: { skipAll?: boolean; skipNonCritical?: boolean },
  ): Promise<AnswerClarificationResult | null> {
    submitting.value = true;
    lastError.value = '';
    try {
      const res = await answerClarification(pipelineId(), {
        setId,
        answers: payload.skipAll
          ? []
          : answers.map(q => ({
              questionId: q.id,
              optionIds: [],
              freeText: undefined,
            })),
        skippedQuestionIds: payload.skipNonCritical ? answers.filter(q => !q.required).map(q => q.id) : [],
        skipAll: payload.skipAll,
      });
      activeSet.value = null;
      return res;
    } catch (e: unknown) {
      lastError.value = e instanceof Error ? e.message : '提交失败';
      return null;
    } finally {
      submitting.value = false;
    }
  }

  return {
    activeSet,
    submitting,
    lastError,
    setActive,
    submit,
  };
}
