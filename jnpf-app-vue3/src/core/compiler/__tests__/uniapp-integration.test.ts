/**
 * UniApp 多实体端到端集成测试
 *
 * 验证：多实体编译 → 文件无冲突 → pages.json合并正确
 */

import { describe, it, expect } from "vitest";
import { UniAppCompiler } from "../uniapp/compiler";
import { mergePagesJson } from "../uniapp/pages-json-merger";
import type { FormPageIR } from "../uniapp/types";

// ─── 测试实体：学生 ───

const studentIR: FormPageIR = {
  type: "form",
  id: "student",
  name: "学生管理",
  fields: [
    {
      id: "s1",
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
  ],
  expressions: [],
};

// ─── 测试实体：订单 ───

const orderIR: FormPageIR = {
  type: "form",
  id: "order",
  name: "订单管理",
  fields: [
    {
      id: "o1",
      model: "orderNo",
      label: "订单号",
      component: {
        jnpfKey: "JnpfInput",
        pc: "a-input",
        app: "uni-easyinput",
        legacyApp: "uni-easyinput",
      },
      config: {
        required: true,
        defaultValue: "",
        placeholder: "请输入订单号",
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
      id: "o2",
      model: "amount",
      label: "金额",
      component: {
        jnpfKey: "JnpfInputNumber",
        pc: "a-input-number",
        app: "uni-easyinput",
        legacyApp: "uni-easyinput",
      },
      config: {
        required: true,
        defaultValue: 0,
        placeholder: "请输入金额",
        disabled: false,
        readonly: false,
        hidden: false,
        span: 12,
        labelWidth: null,
        maxlength: null,
        showWordLimit: false,
        clearable: true,
        min: 0,
        max: null,
        precision: 2,
        step: null,
        multiple: false,
        options: [],
        dictType: null,
        style: {},
      },
    },
  ],
  expressions: [],
};

// ============================================================
// 集成测试
// ============================================================

describe("多实体端到端", () => {
  it("两个实体分别编译无文件路径冲突", () => {
    const c1 = new UniAppCompiler({ entity: "student" }).compile(studentIR);
    const c2 = new UniAppCompiler({ entity: "order" }).compile(orderIR);

    expect(c1.project.has("pages/student/list.vue")).toBe(true);
    expect(c2.project.has("pages/order/list.vue")).toBe(true);

    // 文件路径不冲突
    for (const key of c1.project.keys()) {
      expect(c2.project.has(key)).toBe(false, `路径冲突: ${key}`);
    }
  });

  it("pages.json合并正确（2实体 → 6页面）", () => {
    const c1 = new UniAppCompiler({ entity: "student" }).compile(studentIR);
    const c2 = new UniAppCompiler({ entity: "order" }).compile(orderIR);

    const fragment1 = c1.project.get("pages-student.json")!;
    const fragment2 = c2.project.get("pages-order.json")!;

    const merged = mergePagesJson([fragment1, fragment2]);
    const parsed = JSON.parse(merged);

    expect(parsed.pages.length).toBe(6); // 3 + 3
    expect(parsed.globalStyle.navigationBarBackgroundColor).toBe("#0083ff");
    expect(parsed.globalStyle.navigationBarTextStyle).toBe("white");
  });

  it("合并后无路由冲突（所有page路径唯一）", () => {
    const c1 = new UniAppCompiler({ entity: "student" }).compile(studentIR);
    const c2 = new UniAppCompiler({ entity: "order" }).compile(orderIR);

    const merged = mergePagesJson([
      c1.project.get("pages-student.json")!,
      c2.project.get("pages-order.json")!,
    ]);

    const pages: Array<{ path: string }> = JSON.parse(merged).pages;
    const paths = pages.map((p) => p.path);
    expect(new Set(paths).size).toBe(paths.length);
  });

  it("异常JSON片段被跳过不崩溃", () => {
    const merged = mergePagesJson([
      "invalid json{{{",
      '{"pages": [{"path": "pages/test/list"}]}',
    ]);
    const parsed = JSON.parse(merged);
    expect(parsed.pages.length).toBe(1);
    expect(parsed.pages[0].path).toBe("pages/test/list");
  });

  it("三实体合并", () => {
    const schemas = [
      { entity: "student", ir: studentIR },
      { entity: "order", ir: orderIR },
      {
        entity: "product",
        ir: {
          ...studentIR,
          id: "product",
          name: "产品管理",
        },
      },
    ];

    const fragments: string[] = [];
    for (const { entity, ir } of schemas) {
      const c = new UniAppCompiler({ entity }).compile(ir);
      fragments.push(c.project.get(`pages-${entity}.json`)!);
    }

    const merged = mergePagesJson(fragments);
    const parsed = JSON.parse(merged);
    expect(parsed.pages.length).toBe(9); // 3 × 3
    // 验证所有路径唯一
    const paths: string[] = parsed.pages.map((p: { path: string }) => p.path);
    expect(new Set(paths).size).toBe(9);
  });
});

// ============================================================
// 多平台集成
// ============================================================

describe("UniAppCompiler (多平台)", () => {
  it("mp-alipay平台标识正确", () => {
    const compiler = new UniAppCompiler({ entity: "student" }, "mp-alipay");
    const result = compiler.compile(studentIR);
    const list = result.project.get("pages/student/list.vue")!;
    expect(list).toContain("platform=mp-alipay");
  });

  it("mp-douyin平台标识正确", () => {
    const compiler = new UniAppCompiler({ entity: "student" }, "mp-douyin");
    const result = compiler.compile(studentIR);
    const list = result.project.get("pages/student/list.vue")!;
    expect(list).toContain("platform=mp-douyin");
  });
});
