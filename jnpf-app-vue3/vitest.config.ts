import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    // 编译器测试不需要 uni-app 运行时
    environment: "node",
  },
});
