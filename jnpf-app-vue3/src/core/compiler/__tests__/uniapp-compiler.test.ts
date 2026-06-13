/**
 * UniAppCompiler 单元测试
 *
 * 验证：7 类文件生成 + 零 eval/Function + @jnpf-generated 标记
 */

import { describe, it, expect } from "vitest";
import { UniAppCompiler } from "../uniapp/compiler";
import type { FormPageIR } from "../uniapp/types";

// ============================================================
// 最小测试 Schema
// ============================================================

const studentIR: FormPageIR = {
  type: "form",
  id: "student",
  name: "学生管理",
  fields: [
    {
      id: "f1",
      model: "name",
      label: "姓名",
      component: {
        jnpfKey: "JnpfInput",
        pc: "a-input",
        app: "uni-easyinput",
        legacyApp: "uni-easyinput",
      },
      config: {
        required: true,
        defaultValue: "",
        placeholder: "请输入姓名",
        disabled: false,
        readonly: false,
        hidden: false,
        span: 24,
        labelWidth: null,
        maxlength: 50,
        showWordLimit: false,
        clearable: true,
        min: null,
        max: null,
        precision: null,
        step: null,
        multiple: false,
        options: [],
        dictType: null,
        style: {},
      },
    },
    {
      id: "f2",
      model: "age",
      label: "年龄",
      component: {
        jnpfKey: "JnpfInputNumber",
        pc: "a-input-number",
        app: "uni-easyinput",
        legacyApp: "uni-easyinput",
      },
      config: {
        required: false,
        defaultValue: 0,
        placeholder: "请输入年龄",
        disabled: false,
        readonly: false,
        hidden: false,
        span: 12,
        labelWidth: null,
        maxlength: null,
        showWordLimit: false,
        clearable: true,
        min: 1,
        max: 200,
        precision: 0,
        step: 1,
        multiple: false,
        options: [],
        dictType: null,
        style: {},
      },
    },
    {
      id: "f3",
      model: "gender",
      label: "性别",
      component: {
        jnpfKey: "JnpfSelect",
        pc: "a-select",
        app: "uni-data-select",
        legacyApp: "uni-data-select",
      },
      config: {
        required: true,
        defaultValue: "",
        placeholder: "请选择性别",
        disabled: false,
        readonly: false,
        hidden: false,
        span: 12,
        labelWidth: null,
        maxlength: null,
        showWordLimit: false,
        clearable: true,
        min: null,
        max: null,
        precision: null,
        step: null,
        multiple: false,
        options: [
          { label: "男", value: "male" },
          { label: "女", value: "female" },
        ],
        dictType: "gender",
        style: {},
      },
    },
    {
      id: "f4",
      model: "active",
      label: "启用状态",
      component: {
        jnpfKey: "JnpfSwitch",
        pc: "a-switch",
        app: "switch",
        legacyApp: "switch",
      },
      config: {
        required: false,
        defaultValue: true,
        placeholder: "",
        disabled: false,
        readonly: false,
        hidden: false,
        span: 12,
        labelWidth: null,
        maxlength: null,
        showWordLimit: false,
        clearable: false,
        min: null,
        max: null,
        precision: null,
        step: null,
        multiple: false,
        options: [],
        dictType: null,
        style: {},
      },
    },
    {
      id: "f5",
      model: "birthDate",
      label: "出生日期",
      component: {
        jnpfKey: "JnpfDatePicker",
        pc: "a-date-picker",
        app: "uni-datetime-picker",
        legacyApp: "uni-datetime-picker",
      },
      config: {
        required: false,
        defaultValue: "",
        placeholder: "请选择出生日期",
        disabled: false,
        readonly: false,
        hidden: false,
        span: 12,
        labelWidth: null,
        maxlength: null,
        showWordLimit: false,
        clearable: true,
        min: null,
        max: null,
        precision: null,
        step: null,
        multiple: false,
        options: [],
        dictType: null,
        style: {},
      },
    },
  ],
  expressions: [
    {
      id: "expr_simple",
      name: "simpleCalc",
      type: "computed",
      params: [],
      body: "return 1 + 1",
      level: "simple",
      isAsync: false,
      originalCode: "return 1 + 1",
    },
    {
      id: "expr_complex",
      name: "complexFn",
      type: "validation",
      params: ["data"],
      body: "const results = []; for (let i = 0; i < data.length; i++) { if (data[i].score < 60) results.push(data[i]); } return results;",
      level: "complex",
      isAsync: false,
      originalCode:
        "const results = []; for (let i = 0; i < data.length; i++) { if (data[i].score < 60) results.push(data[i]); } return results;",
    },
  ],
  listConfig: {
    searchFields: [
      { field: "name", label: "姓名", component: "JnpfInput" },
      { field: "gender", label: "性别", component: "JnpfSelect" },
    ],
  },
};

// ============================================================
// 测试套件
// ============================================================

describe("UniAppCompiler (mp-weixin)", () => {
  const compiler = new UniAppCompiler(
    { entity: "student", entityLabel: "学生" },
    "mp-weixin",
  );
  const result = compiler.compile(studentIR);

  // ------ 测试 1: 生成列表页 ------

  it("生成列表页(.vue)", () => {
    const key = "pages/student/list.vue";
    expect(result.project.has(key)).toBe(true);

    const list = result.project.get(key)!;
    expect(list).toContain("wd-cell");
    expect(list).toContain("@jnpf-generated");
    expect(list).toContain("platform=mp-weixin");
    expect(list).toContain("onPullDownRefresh");
    expect(list).toContain("onReachBottom");
    expect(list).toContain("useStudentStore");
    // insert-point
    expect(list).toContain("@jnpf-gen:insert-point=custom-list-logic");
    expect(list).toContain("@jnpf-gen:end-insert-point=custom-list-logic");
  });

  // ------ 测试 2: 生成表单页 ------

  it("生成表单页(.vue)", () => {
    const key = "pages/student/form.vue";
    expect(result.project.has(key)).toBe(true);

    const form = result.project.get(key)!;
    expect(form).toContain("wd-form");
    expect(form).toContain("wd-input");
    expect(form).toContain("wd-select-picker");
    expect(form).toContain("wd-switch");
    expect(form).toContain("wd-datetime-picker");
    expect(form).toContain("@jnpf-generated");
    expect(form).toContain("platform=mp-weixin");
    expect(form).toContain("isEdit");
    expect(form).toContain("Object.assign");
    // insert-point
    expect(form).toContain("@jnpf-gen:insert-point=custom-form-fields");
    expect(form).toContain("@jnpf-gen:end-insert-point=custom-form-fields");
    expect(form).toContain("@jnpf-gen:insert-point=custom-form-logic");
  });

  // ------ 测试 3: 生成详情页 ------

  it("生成详情页(.vue)", () => {
    const key = "pages/student/detail.vue";
    expect(result.project.has(key)).toBe(true);

    const detail = result.project.get(key)!;
    expect(detail).toContain("wd-cell-group");
    expect(detail).toContain("学生 详情");
    expect(detail).toContain("@jnpf-generated");
    expect(detail).toContain("handleEdit");
    // insert-point
    expect(detail).toContain("@jnpf-gen:insert-point=custom-detail-fields");
    expect(detail).toContain("@jnpf-gen:end-insert-point=custom-detail-fields");
  });

  // ------ 测试 4: 生成 API ------

  it("生成API(Alova)", () => {
    const key = "api/student.ts";
    expect(result.project.has(key)).toBe(true);

    const api = result.project.get(key)!;
    expect(api).toContain("createEntityApi");
    expect(api).toContain("StudentEntity");
    expect(api).toContain("getStudentList");
    expect(api).toContain("getStudentDetail");
    expect(api).toContain("createStudent");
    expect(api).toContain("updateStudent");
    expect(api).toContain("deleteStudent");
    expect(api).toContain("batchDeleteStudent");
    expect(api).toContain("@jnpf-generated");
    expect(api).toContain("from '@/api/request'");
  });

  // ------ 测试 5: 生成 Store ------

  it("生成Store(Pinia)", () => {
    const key = "stores/student.ts";
    expect(result.project.has(key)).toBe(true);

    const store = result.project.get(key)!;
    expect(store).toContain("defineStore");
    expect(store).toContain("useStudentStore");
    expect(store).toContain("loadList");
    expect(store).toContain("loadDetail");
    expect(store).toContain("@jnpf-generated");
    expect(store).toContain("ref");
    expect(store).toContain("reactive");
  });

  // ------ 测试 6: 生成 types ------

  it("生成types", () => {
    const key = "types/student.ts";
    expect(result.project.has(key)).toBe(true);

    const types = result.project.get(key)!;
    expect(types).toContain("export interface StudentEntity");
    // name 字段 — required=true，不加 ?
    expect(types).toContain("name: string");
    // age 字段 — required=false，加 ?
    expect(types).toContain("age?: number");
    // gender 字段 — JnpfSelect, required=true → 不加 ?
    expect(types).toContain('gender: string');
    // active 字段 — JnpfSwitch → boolean
    expect(types).toContain("active?: boolean");
    // birthDate 字段 — JnpfDatePicker → string
    expect(types).toContain("birthDate?: string");
    expect(types).toContain("@jnpf-generated");
  });

  // ------ 测试 7: 生成 pages.json ------

  it("生成pages.json片段", () => {
    const key = "pages-student.json";
    expect(result.project.has(key)).toBe(true);

    const json = result.project.get(key)!;
    const parsed = JSON.parse(json);
    expect(parsed.pages).toHaveLength(3);
    expect(parsed.pages[0].path).toBe("pages/student/list");
    expect(parsed.pages[1].path).toBe("pages/student/form");
    expect(parsed.pages[2].path).toBe("pages/student/detail");
    // enablePullDownRefresh 仅在 list 页
    expect(parsed.pages[0].style.enablePullDownRefresh).toBe(true);
    expect(parsed.pages[1].style.enablePullDownRefresh).toBeUndefined();
    expect(parsed.pages[2].style.enablePullDownRefresh).toBeUndefined();
  });

  // ------ 测试 8: 零 eval/Function ------

  it("零eval/Function", () => {
    for (const [path, content] of result.project) {
      expect(content, `文件 ${path} 包含 eval`).not.toMatch(/\beval\b/);
      expect(content, `文件 ${path} 包含 new Function`).not.toMatch(
        /new\s+Function/,
      );
    }
  });

  // ------ 测试 9: 所有文件含 @jnpf-generated ------

  it("所有生成文件含@jnpf-generated标记", () => {
    for (const [path, content] of result.project) {
      // pages.json 使用 _comment 字段，不适用 .vue/.ts 检查
      if (path.endsWith(".ts") || path.endsWith(".vue")) {
        expect(content, `文件 ${path} 缺少 @jnpf-generated`).toContain(
          "@jnpf-generated",
        );
      }
    }
  });

  // ------ 测试 10: 复杂表达式检测 ------

  it("检测并记录复杂表达式", () => {
    expect(result.complexExpressions).toHaveLength(1);
    expect(result.complexExpressions[0]).toContain("expr_complex");
    expect(result.warnings).toHaveLength(1);
    expect(result.warnings[0]).toContain("复杂级别");
  });

  // ------ 测试 11: platform 标识正确 ------

  it("所有文件包含正确的platform标识", () => {
    for (const [path, content] of result.project) {
      if (path.endsWith(".ts") || path.endsWith(".vue")) {
        expect(content).toContain("platform=mp-weixin");
      }
    }
  });

  // ------ 测试 12: h5 平台 ------

  it("h5平台生成不同的platform标识", () => {
    const h5Compiler = new UniAppCompiler(
      { entity: "student", entityLabel: "学生" },
      "h5",
    );
    const h5Result = h5Compiler.compile(studentIR);
    const list = h5Result.project.get("pages/student/list.vue")!;
    expect(list).toContain("platform=h5");
  });
});
