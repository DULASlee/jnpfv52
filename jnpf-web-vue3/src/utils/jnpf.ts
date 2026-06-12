import { defHttp } from '/@/utils/http/axios';
import { getToken } from '/@/utils/auth';
import { isString, isNumber } from '/@/utils/is';
import { useMessage } from '/@/hooks/web/useMessage';
import { useUserStoreWithOut } from '/@/store/modules/user';
import type { UserInfo } from '/#/store';
import { router } from '/@/router';

interface OnlineUserInfo extends UserInfo {
  token?: string;
}

export function toDecimal(num = 0) {
  const sign = num == (num = Math.abs(num));
  num = Math.floor(num * 100 + 0.50000000001);
  const cents = num % 100;
  let value: string = Math.floor(num / 100).toString();
  const centsStr: string = cents < 10 ? '0' + cents : cents.toString();
  for (let i = 0; i < Math.floor((value.length - (1 + i)) / 3); i++)
    value = value.substring(0, value.length - (4 * i + 3)) + '' + value.substring(value.length - (4 * i + 3));
  return (sign ? '' : '-') + value + '.' + centsStr;
}
export function toFileSize(size) {
  if (size == null || size == '') return '';
  if (size < 1024) return toDecimal(size) + ' 字节';
  else if (size >= 1024 && size < 1048576) return toDecimal(size / 1024) + ' KB';
  else if (size >= 1048576 && size < 1073741824) return toDecimal(size / 1024 / 1024) + ' MB';
  else if (size >= 1073741824) return toDecimal(size / 1024 / 1024 / 1024) + ' GB';
}
export function toDateText(dateTimeStamp) {
  if (!dateTimeStamp) return '';
  let result = '';
  const minute = 1000 * 60; //把分，时，天，周，半个月，一个月用毫秒表示
  const hour = minute * 60;
  const day = hour * 24;
  const week = day * 7;
  // let halfAMonth = day * 15;
  const month = day * 30;
  const now = new Date().getTime(); //获取当前时间毫秒
  const diffValue = now - dateTimeStamp; //时间差
  if (diffValue < 0) return '刚刚';
  const minC = diffValue / minute; //计算时间差的分，时，天，周，月
  const hourC = diffValue / hour;
  const dayC = diffValue / day;
  const weekC = diffValue / week;
  const monthC = diffValue / month;
  if (monthC >= 1 && monthC <= 3) {
    result = ' ' + parseInt(monthC) + '月前';
  } else if (weekC >= 1 && weekC <= 3) {
    result = ' ' + parseInt(weekC) + '周前';
  } else if (dayC >= 1 && dayC <= 6) {
    result = ' ' + parseInt(dayC) + '天前';
  } else if (hourC >= 1 && hourC <= 23) {
    result = ' ' + parseInt(hourC) + '小时前';
  } else if (minC >= 1 && minC <= 59) {
    result = ' ' + parseInt(minC) + '分钟前';
  } else if (diffValue >= 0 && diffValue <= minute) {
    result = '刚刚';
  } else {
    const datetime = new Date();
    datetime.setTime(dateTimeStamp);
    const nYear = datetime.getFullYear();
    const nMonth = datetime.getMonth() + 1 < 10 ? '0' + (datetime.getMonth() + 1) : datetime.getMonth() + 1;
    const nDate = datetime.getDate() < 10 ? '0' + datetime.getDate() : datetime.getDate();
    // const nHour = datetime.getHours() < 10 ? '0' + datetime.getHours() : datetime.getHours();
    // const nMinute = datetime.getMinutes() < 10 ? '0' + datetime.getMinutes() : datetime.getMinutes();
    // const nSecond = datetime.getSeconds() < 10 ? '0' + datetime.getSeconds() : datetime.getSeconds();
    result = nYear + '-' + nMonth + '-' + nDate;
  }
  return result;
}
export function toDateValue(dateTimeStamp) {
  if (!dateTimeStamp) return '';
  let result = '';
  const datetime = new Date();
  const nowYear = datetime.getFullYear();
  datetime.setTime(dateTimeStamp);
  const nYear = datetime.getFullYear();
  const nMonth = datetime.getMonth() + 1 < 10 ? '0' + (datetime.getMonth() + 1) : datetime.getMonth() + 1;
  const nDate = datetime.getDate() < 10 ? '0' + datetime.getDate() : datetime.getDate();
  const nHour = datetime.getHours() < 10 ? '0' + datetime.getHours() : datetime.getHours();
  const nMinute = datetime.getMinutes() < 10 ? '0' + datetime.getMinutes() : datetime.getMinutes();
  if (nYear == nowYear) {
    result = nMonth + '-' + nDate + ' ' + nHour + ':' + nMinute;
  } else {
    result = nYear + '-' + nMonth + '-' + nDate + ' ' + nHour + ':' + nMinute;
  }
  return result;
}
// 数据类型转换
export function getDataTypeText(val) {
  let text = val;
  switch (val) {
    case 'varchar':
      text = '字符串';
      break;
    case 'int':
      text = '整型';
      break;
    case 'datetime':
      text = '日期时间';
      break;
    case 'decimal':
      text = '浮点';
      break;
    case 'bigint':
      text = '长整型';
      break;
    case 'text':
      text = '文本';
      break;
    default:
      text = val;
      break;
  }
  return text;
}
// 白名单数据处理函数（替代 eval）
const DATA_PROCESSING_FUNCTIONS: Record<string, (data: unknown, ...args: unknown[]) => unknown> = {
  formatDate: (data: unknown, fmt?: string) => {
    if (!data) return '';
    const d = new Date(data as string);
    if (isNaN(d.getTime())) return data;
    const pad = (n: number) => String(n).padStart(2, '0');
    const map: Record<string, string> = {
      yyyy: String(d.getFullYear()),
      MM: pad(d.getMonth() + 1),
      dd: pad(d.getDate()),
      HH: pad(d.getHours()),
      mm: pad(d.getMinutes()),
      ss: pad(d.getSeconds()),
    };
    let result = fmt || 'yyyy-MM-dd';
    for (const [k, v] of Object.entries(map)) {
      result = result.replace(k, v);
    }
    return result;
  },
  formatMoney: (data: unknown) => {
    const num = Number(data);
    if (isNaN(num)) return data;
    return num.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  },
  toFixed: (data: unknown, n?: unknown) => {
    const num = Number(data);
    if (isNaN(num)) return data;
    return num.toFixed(Number(n) || 2);
  },
  toUpperCase: (data: unknown) => String(data).toUpperCase(),
  toLowerCase: (data: unknown) => String(data).toLowerCase(),
  trim: (data: unknown) => String(data).trim(),
  maskPhone: (data: unknown) => {
    const s = String(data);
    return s.length >= 11 ? s.replace(/(\d{3})\d{4}(\d{4})/, '$1****$2') : s;
  },
  maskIdCard: (data: unknown) => {
    const s = String(data);
    return s.length >= 18 ? s.replace(/(\d{6})\d{8}(\d{4})/, '$1********$2') : s;
  },
  round: (data: unknown, n?: unknown) => {
    const num = Number(data);
    if (isNaN(num)) return data;
    const factor = Math.pow(10, Number(n) || 0);
    return Math.round(num * factor) / factor;
  },
};

function safeParseLiteralArgs(argsStr: string): unknown[] {
  if (!argsStr.trim()) return [];
  const args: unknown[] = [];
  const tokenPattern = /'([^']*)'|"([^"]*)"|(\d+\.?\d*)|(true|false|null|undefined)/g;
  let match;
  while ((match = tokenPattern.exec(argsStr)) !== null) {
    if (match[1] !== undefined) args.push(match[1]);
    else if (match[2] !== undefined) args.push(match[2]);
    else if (match[3] !== undefined) args.push(Number(match[3]));
    else if (match[4] === 'true') args.push(true);
    else if (match[4] === 'false') args.push(false);
    else if (match[4] === 'null') args.push(null);
    else if (match[5] !== undefined) args.push(undefined);
  }
  return args;
}

function resolvePath(obj: unknown, path: string): unknown {
  return path.split('.').reduce((o, k) => (o as Record<string, unknown>)?.[k], obj);
}

function safeDataProcessing(str: string, data: unknown): unknown {
  if (!str || !str.trim()) return data;

  const trimmed = str.trim();

  // 模式 1：简单函数调用 —— "funcName(data)" 或 "funcName(data, 'arg1', 123)"
  const callMatch = trimmed.match(/^(\w+)\((.*)\)$/s);
  if (callMatch) {
    const fnName = callMatch[1];
    const fn = DATA_PROCESSING_FUNCTIONS[fnName];
    if (fn) {
      try {
        const args = safeParseLiteralArgs(callMatch[2].replace(/^data\s*,?\s*/, ''));
        return fn(data, ...args);
      } catch (e) {
        console.warn(`[dataProcessing] 执行失败: ${str}`, e);
        return data;
      }
    }
    console.warn(`[dataProcessing] 未知函数: ${fnName}，跳过执行`);
    return data;
  }

  // 模式 2：简单属性访问 —— "data.fieldName"
  const pathMatch = trimmed.match(/^data\.([\w.]+)$/);
  if (pathMatch) {
    return resolvePath(data, pathMatch[1]);
  }

  // 模式 3：不支持的表达式 → 安全降级
  console.warn(`[dataProcessing] 不支持的表达式格式: ${str}，跳过执行`);
  return data;
}

export function getScriptFunc(str) {
  if (!str || !str.trim()) return false;
  try {
    // 返回安全包装器：使用白名单评估器替代 eval
    return (contextData: unknown) => safeDataProcessing(str, contextData);
  } catch (_) {
    return false;
  }
}
export function handleRequest(url: string, method: string, data) {
  return defHttp[method ? method.toLowerCase() : 'get']({ url, data });
}
export function interfaceDataHandler(data): any {
  if (!data.dataProcessing) return data.data;
  const dataHandler: any = getScriptFunc(data.dataProcessing);
  if (!dataHandler) return data.data;
  return dataHandler(data.data);
}
// 代码生成器数据匹配
export function dynamicText(value, options) {
  if (!value) return '';
  if (Array.isArray(value)) {
    if (!options || !Array.isArray(options)) return value.join();
    const textList: any[] = [];
    for (let i = 0; i < value.length; i++) {
      const item = options.filter(o => o.id == value[i])[0];
      if (!item || !item.fullName) {
        textList.push(value[i]);
      } else {
        textList.push(item.fullName);
      }
    }
    return textList.join();
  }
  if (!options || !Array.isArray(options)) return value;
  const item = options.filter(o => o.id == value)[0];
  if (!item || !item.fullName) return value;
  return item.fullName;
}

export function dynamicTreeText(value, options) {
  if (!value) return '';

  function transfer(data) {
    const textList: any[] = [];

    function loop(data, id) {
      for (let i = 0; i < data.length; i++) {
        if (data[i].id === id) {
          textList.push(data[i].fullName);
          break;
        }
        if (data[i].children) loop(data[i].children, id);
      }
    }
    for (let i = 0; i < data.length; i++) {
      loop(options, data[i]);
    }
    return textList.join();
  }
  if (!options || !Array.isArray(options)) return value.join();
  if (Array.isArray(value)) {
    const text = transfer(value);
    return text;
  } else {
    if (!options || !Array.isArray(options)) return value;
    const list = value.split();
    const text = transfer(list);
    return text;
  }
}
/**
 * 金额转中文
 * 思路：
 *                              个
 *      十     百      千       万
 *      十万   百万    千万     亿
 *      十亿   百亿    千亿
 *
 *                              1
 *      2      3       4        5
 *      6      7       8        9
 *      10
 *
 * 计算步骤
 * 1. 获取当前数值大小
 * 2. 排除个位后 数值按个，十，百，千有规律的重复 所以计算其和4的余数 pos % 4
 * 3. pos = 0 ~ 3 没有最大单位
 *    pos = 4 ~ 7 最大单位是万
 *    pos = 8 ~ 11 最大单位是亿
 * pos / 4 的整数就是最大单位
 *
 */
export function getAmountChinese(val) {
  if (!val && val !== 0) return '';
  if (val == 0) return '零元整';
  const regExp = /[a-zA-Z]/;
  if (regExp.test(val)) return '数字较大溢出';
  let amount = +val;
  if (isNaN(amount)) return '';
  if (amount < 0) amount = Number(amount.toString().split('-')[1]);
  const NUMBER = ['零', '壹', '贰', '叁', '肆', '伍', '陆', '柒', '捌', '玖'];
  const N_UNIT1 = ['', '拾', '佰', '仟'];
  const N_UNIT2 = ['', '万', '亿', '兆'];
  const D_UNIT = ['角', '分', '厘', '毫'];
  const [integer, decimal] = amount.toString().split('.');
  if (integer && (integer.length > 15 || integer.indexOf('e') > -1)) return '数字较大溢出';
  let res = '';
  // 整数部分
  if (integer) {
    let zeroCount = 0;
    for (let i = 0, len = integer.length; i < len; i++) {
      const num = integer.charAt(i);
      const pos = len - i - 1; // 排除个位后 所处的索引位置
      const q = pos / 4;
      const m = pos % 4;
      if (num === '0') {
        zeroCount++;
      } else {
        if (zeroCount > 0 && m !== 3) res += NUMBER[0];
        zeroCount = 0;
        res += NUMBER[parseInt(num)] + N_UNIT1[m];
      }
      if (m == 0 && zeroCount < 4) res += N_UNIT2[Math.floor(q)];
    }
  }
  if (Number(integer) != 0) res += '元';
  // 小数部分
  if (parseInt(decimal)) {
    for (let i = 0; i < 4; i++) {
      const num = decimal.charAt(i);
      if (parseInt(num)) res += NUMBER[num] + D_UNIT[i];
    }
  } else {
    res += '整';
  }
  if (val < 0) res = '负数' + res;
  return res;
}
// 转千位分隔
export function thousandsFormat(num) {
  if (num === 0) return '0';
  if (!num) return '';
  const numArr = num.toString().split('.');
  numArr[0] = numArr[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  return numArr.join('.');
}
export function getDateTimeUnit(format) {
  if (format == 'YYYY' || format == 'yyyy') return 'year';
  if (format == 'YYYY-MM' || format == 'yyyy-MM') return 'month';
  if (format == 'YYYY-MM-DD' || format == 'yyyy-MM-dd') return 'day';
  if (format == 'YYYY-MM-DD HH:mm' || format == 'yyyy-MM-dd HH:mm') return 'minute';
  if (format == 'YYYY-MM-DD HH:mm:ss' || format == 'yyyy-MM-dd HH:mm:ss') return 'second';
  return 'day';
}
export function getTimeUnit(key) {
  if (key == 1) return 'year';
  if (key == 2) return 'month';
  if (key == 3) return 'day';
  if (key == 4) return 'hour';
  if (key == 5) return 'minute';
  if (key == 6) return 'second';
  return 'day';
}
export function getDateFormat(format) {
  if (!format) return 'YYYY-MM-DD HH:mm:ss';
  const formatObj = {
    yyyy: 'YYYY',
    'yyyy-MM': 'YYYY-MM',
    'yyyy-MM-dd': 'YYYY-MM-DD',
    'yyyy-MM-dd HH:mm': 'YYYY-MM-DD HH:mm',
    'yyyy-MM-dd HH:mm:ss': 'YYYY-MM-DD HH:mm:ss',
    YYYY: 'YYYY',
    'YYYY-MM': 'YYYY-MM',
    'YYYY-MM-DD': 'YYYY-MM-DD',
    'YYYY-MM-DD HH:mm': 'YYYY-MM-DD HH:mm',
    'YYYY-MM-DD HH:mm:ss': 'YYYY-MM-DD HH:mm:ss',
  };
  return formatObj[format] || 'YYYY-MM-DD HH:mm:ss';
}
export function getJnpfAppId() {
  let appId = '';
  if (window.location.pathname?.startsWith('/JNPF_APP_')) {
    const list = window.location.pathname.split('/');
    appId = list[1];
  }
  return appId;
}
export const onlineUtils = {
  // 获取用户信息
  getUserInfo() {
    const userStore = useUserStoreWithOut();
    const userInfo: OnlineUserInfo = userStore.getUserInfo;
    userInfo.token = getToken() as string;
    return userInfo;
  },
  // 获取设备信息
  getDeviceInfo() {
    const deviceInfo = { vueVersion: '3', origin: 'pc' };
    return deviceInfo;
  },
  // 请求
  request(url: string, method: string, data = {}, headers = {}) {
    return defHttp[method ? method.toLowerCase() : 'get']({ url, data, headers });
  },
  // 路由跳转
  route(url: string) {
    if (!url) return;
    router.push(url);
  },
  // 消息提示
  toast(message: string | number, type = 'info', duration = 3000) {
    const { createMessage } = useMessage();
    if (!isString(message) && !isNumber(message)) return;
    const newDuration = duration / 1000;
    const config = { content: message, type, duration: newDuration };
    createMessage[type] && createMessage[type](config);
  },
};
export function getParamList(templateJson, data?) {
  if (!templateJson?.length) return [];
  for (let i = 0; i < templateJson.length; i++) {
    const e = templateJson[i];
    if (e.sourceType == 1 && data) {
      e.defaultValue = data[e.relationField] || data[e.relationField] == 0 || data[e.relationField] == false ? data[e.relationField] : '';
    }
    if (e.sourceType == 4 && e.relationField == '@formId') e.defaultValue = data.id || '';
  }
  return templateJson;
}
