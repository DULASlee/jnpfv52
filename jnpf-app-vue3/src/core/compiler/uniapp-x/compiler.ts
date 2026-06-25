/**
 * UniApp X 代码生成器 (uvue + uts, 目标: App 端)
 *
 * v5.0 创始人裁定：暂缓实现，保留接口。
 * 待 uvue 生态成熟后再启 PoC-A。
 *
 * 复用：API层、Store层、类型层与标准 UniApp 完全相同。
 * 差异：
 *   - 文件扩展名 .vue → .uvue
 *   - 脚本 TypeScript → UTS
 *   - wot-design-uni → uvue 原生组件
 *   - @dcloudio/uni-app → 原生 API
 *
 * @jnpf-generated v5.2.0 type=compiler-x platform=uniapp-x
 */

import type { FormPageIR } from "../../ir/types";
import type {
  CompileResult,
  CompilerConfig,
  GeneratedProject,
} from "../vue3/types";

export class UniAppXCompiler {
  private config: CompilerConfig;

  constructor(config: Partial<CompilerConfig> & { entity: string }) {
    this.config = {
      entity: config.entity,
      entityLabel: config.entityLabel ?? config.entity,
      apiBasePath: config.apiBasePath ?? `/api/${config.entity}`,
      generatorVersion: config.generatorVersion ?? "1.0.0",
    };
  }

  /**
   * v5.0 暂缓 — 未实现
   *
   * 编译器接口与 UniAppCompiler 兼容。
   * 待 uvue 生态成熟后实现：
   *   1. pages/{entity}/list.vue → pages/{entity}/list.uvue
   *   2. <script lang="ts"> → <script lang="uts">
   *   3. wot-design-uni → uvue 原生组件
   *   4. @dcloudio/uni-app → 原生 API
   */
  compile(_ir: FormPageIR): CompileResult {
    throw new Error("UniAppXCompiler: v5.0 暂缓，待 uvue 生态成熟");
  }
}
