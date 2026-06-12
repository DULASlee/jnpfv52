/**
 * 白名单函数库
 * 所有函数必须遵守纯函数约束：
 *   1. 不访问 this
 *   2. 不访问闭包外变量
 *   3. 不调用 eval / new Function
 *   4. 不访问 window / document / globalThis
 *   5. 只使用参数 + 本地变量
 */

export const WHITELIST_FUNCTIONS: Record<string, (...args: unknown[]) => unknown> = {
  // 字符串
  LEN: (v: unknown) => String(v ?? '').length,
  UPPER: (v: unknown) => String(v ?? '').toUpperCase(),
  LOWER: (v: unknown) => String(v ?? '').toLowerCase(),
  TRIM: (v: unknown) => String(v ?? '').trim(),
  CONTAINS: (v: unknown, sub: unknown) => String(v ?? '').includes(String(sub)),
  REPLACE: (v: unknown, old: unknown, rep: unknown) => String(v ?? '').replace(String(old), String(rep)),

  // 数学
  ROUND: (v: unknown, n?: unknown) => {
    const num = Number(v);
    if (isNaN(num)) return v;
    const factor = Math.pow(10, Number(n) || 0);
    return Math.round(num * factor) / factor;
  },
  CEIL: (v: unknown) => Math.ceil(Number(v)),
  FLOOR: (v: unknown) => Math.floor(Number(v)),
  ABS: (v: unknown) => Math.abs(Number(v)),
  MAX: (...args: unknown[]) => Math.max(...args.map(Number)),
  MIN: (...args: unknown[]) => Math.min(...args.map(Number)),

  // 条件
  IF: (cond: unknown, thenVal: unknown, elseVal: unknown) => (cond ? thenVal : elseVal),
  IF_EMPTY: (v: unknown, fallback: unknown) => (v === '' || v === null || v === undefined ? fallback : v),
  IF_NULL: (v: unknown, fallback: unknown) => (v === null || v === undefined ? fallback : v),

  // 日期
  TODAY: () => new Date().toISOString().slice(0, 10),
  NOW: () => new Date().toISOString(),
  FORMAT_DATE: (v: unknown, fmt?: unknown) => {
    if (!v) return '';
    const d = new Date(v as string);
    if (isNaN(d.getTime())) return String(v);
    const pad = (n: number) => String(n).padStart(2, '0');
    return String(fmt || 'yyyy-MM-dd')
      .replace('yyyy', String(d.getFullYear()))
      .replace('MM', pad(d.getMonth() + 1))
      .replace('dd', pad(d.getDate()))
      .replace('HH', pad(d.getHours()))
      .replace('mm', pad(d.getMinutes()))
      .replace('ss', pad(d.getSeconds()));
  },

  // 格式化
  FORMAT_MONEY: (v: unknown) => {
    const num = Number(v);
    if (isNaN(num)) return v;
    return num.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  },
  MASK_PHONE: (v: unknown) => {
    const s = String(v);
    return s.length >= 11 ? s.replace(/(\d{3})\d{4}(\d{4})/, '$1****$2') : s;
  },
  MASK_ID_CARD: (v: unknown) => {
    const s = String(v);
    return s.length >= 18 ? s.replace(/(\d{6})\d{8}(\d{4})/, '$1********$2') : s;
  },
};
