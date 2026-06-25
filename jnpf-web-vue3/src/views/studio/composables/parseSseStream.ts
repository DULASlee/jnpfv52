// 文件：src/views/studio/composables/parseSseStream.ts
// 职责：SSE 规范解析器（架构组件，任何 SSE 场景可复用）

/**
 * 严格遵循 W3C SSE 规范的流解析器
 *
 * 支持：
 *   - 多行 data 事件（用 \n 连接）
 *   - event: 字段（事件类型）
 *   - : 注释行（忽略）
 *   - \r\n 和 \n 两种换行
 *
 * 不支持（暂不实现）：
 *   - id: 字段（Last-Event-ID 恢复）
 *   - retry: 字段（重连间隔）
 */
export function parseSseStream(
  reader: ReadableStreamDefaultReader<Uint8Array>,
  signal: AbortSignal,
  onEvent: (eventType: string, data: string) => void,
): Promise<void> {
  const decoder = new TextDecoder();
  let buffer = '';
  let currentEvent = 'message';
  const dataLines: string[] = [];

  const dispatch = () => {
    if (dataLines.length > 0) {
      onEvent(currentEvent, dataLines.join('\n'));
    }
    currentEvent = 'message';
    dataLines.length = 0;
  };

  return (async () => {
    while (true) {
      if (signal.aborted) {
        dispatch();
        return;
      }

      const { done, value } = await reader.read();
      if (done) {
        dispatch();
        return;
      }

      buffer += decoder.decode(value, { stream: true });

      let newLineIdx: number;
      while ((newLineIdx = buffer.indexOf('\n')) >= 0) {
        const line = buffer.slice(0, newLineIdx).replace(/\r$/, '');
        buffer = buffer.slice(newLineIdx + 1);

        if (line === '') {
          dispatch();
          continue;
        }

        if (line.startsWith(':')) continue;

        const colonIdx = line.indexOf(':');
        const field = colonIdx >= 0 ? line.slice(0, colonIdx) : line;
        const valueStr = colonIdx >= 0 ? line.slice(colonIdx + 1).replace(/^ /, '') : '';

        if (field === 'event') {
          currentEvent = valueStr;
        } else if (field === 'data') {
          dataLines.push(valueStr);
        }
      }
    }
  })();
}
