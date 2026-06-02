// P0-3: 拆分 barrel — 各组件独立导出，避免 monaco/codemirror 被拖入 entry chunk
// 消费方请直接引用子组件路径：
//   MonacoEditor  → '/@/components/CodeEditor/src/MonacoEditor'
//   CodeEditor    → '/@/components/CodeEditor/src/CodeEditorWrapper'
//   JsonPreview   → '/@/components/CodeEditor/src/JsonPreviewWrapper'
//   MODE          → '/@/components/CodeEditor/src/typing'

export { CodeEditor } from './src/CodeEditorWrapper';
export { MonacoEditor } from './src/MonacoEditor';
export { JsonPreview } from './src/JsonPreviewWrapper';
export * from './src/typing';
