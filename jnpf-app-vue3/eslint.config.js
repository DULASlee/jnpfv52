/**
 * UniApp 项目 ESLint 基础配置 (Flat Config, ESLint 10.x)
 *
 * jnpf-app-vue3 是传统 uni-app 项目（JS），新增 TypeScript 文件（src/）
 */
import tseslint from "typescript-eslint";

export default tseslint.config(
  // 基础推荐规则（JS 文件）
  {
    languageOptions: {
      ecmaVersion: "latest",
      sourceType: "module",
      globals: {
        uni: "readonly",
        getApp: "readonly",
        getCurrentPages: "readonly",
      },
    },
    rules: {
      "no-unused-vars": "warn",
      "no-undef": "warn",
      "no-console": "off",
      "no-debugger": "warn",
    },
  },
  // TypeScript 文件专用配置
  {
    files: ["src/**/*.ts"],
    extends: [tseslint.configs.recommended],
    rules: {
      "@typescript-eslint/no-explicit-any": "off",
      "@typescript-eslint/no-unused-vars": "off",
    },
  },
  // 忽略目录
  {
    ignores: [
      "node_modules/",
      "dist/",
      "unpackage/",
      "libs/",
      "uni_modules/",
      "**/*.cjs",
    ],
  },
);
