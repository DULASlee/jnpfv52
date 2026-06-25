import { validatenull } from '@/echart/util'
export const uuid = () => {
  var s = [];
  var hexDigits = "0123456789abcdef";
  for (var i = 0; i < 36; i++) {
    s[i] = hexDigits.substr(Math.floor(Math.random() * 0x10), 1);
  }
  s[14] = "4"; // bits 12-15 of the time_hi_and_version field to 0010
  s[19] = hexDigits.substr((s[19] & 0x3) | 0x8, 1); // bits 6-7 of the clock_seq_hi_and_reserved to 01
  s[8] = s[13] = s[18] = s[23] = "-";

  var uuid = s.join("");
  return uuid;
}
export const createFile = () => {
  return {
    title: "文件夹",
    name: "文件夹",
    index: uuid(),
    menu: true,
    display: false,
    auto: false,
    lock: false,
    children: []
  }
}
export const compare = (propertyName) => {
  return function (object1, object2) {
    var value1 = object1[propertyName];
    var value2 = object2[propertyName];
    if (value2 < value1) {
      return -1;
    } else if (value2 > value1) {
      return 1;
    } else {
      return 0;
    }
  }
}

// 安全表达式求值器（替代 new Function）
// 支持：属性访问、数学运算、三元表达式、比较运算
function safeResolvePath(path) {
  try {
    // 阻止原型链攻击和危险访问
    if (/__proto__|constructor|prototype|window|document|globalThis|import|require|fetch|eval/i.test(path)) {
      console.warn('[safeResolvePath] 危险路径: ' + path);
      return undefined;
    }
    var parts = path
      .replace(/\[(\d+)]/g, '.$1')
      .replace(/\['([^']+)']/g, '.$1')
      .replace(/\["([^"]+)"]/g, '.$1')
      .split('.');
    var context = (typeof window !== 'undefined' && window.$glob) ? window.$glob.data : {};
    return parts.reduce(function (obj, key) {
      return (obj != null) ? obj[key] : undefined;
    }, context);
  } catch (_) {
    return undefined;
  }
}

function parseExpressionValue(trimmed) {
  // 1. 字面量
  if (/^-?\d+(\.\d+)?$/.test(trimmed)) return Number(trimmed);
  if (trimmed === 'true') return true;
  if (trimmed === 'false') return false;
  if (trimmed === 'null') return null;
  if (trimmed === 'undefined') return undefined;
  if (
    (trimmed.startsWith("'") && trimmed.endsWith("'")) ||
    (trimmed.startsWith('"') && trimmed.endsWith('"'))
  ) {
    return trimmed.slice(1, -1);
  }

  // 2. 安全属性访问：a.b.c 或 data[0].name
  if (/^[\w.[\]]+$/.test(trimmed)) {
    return safeResolvePath(trimmed);
  }

  // 3. 三元表达式：condition ? a : b
  var ternaryMatch = trimmed.match(/^(.+?)\s*\?\s*(.+?)\s*:\s*(.+)$/);
  if (ternaryMatch) {
    var cond = parseExpressionValue(ternaryMatch[1].trim());
    return cond
      ? parseExpressionValue(ternaryMatch[2].trim())
      : parseExpressionValue(ternaryMatch[3].trim());
  }

  // 4. 比较运算
  var compareMatch = trimmed.match(/^(.+?)\s*(===|!==|==|!=|>=|<=|>|<)\s*(.+)$/);
  if (compareMatch) {
    var left = parseExpressionValue(compareMatch[1].trim());
    var right = parseExpressionValue(compareMatch[3].trim());
    switch (compareMatch[2]) {
      case '===': return left === right;
      case '!==': return left !== right;
      case '==': return left == right;  // eslint-disable-line
      case '!=': return left != right;  // eslint-disable-line
      case '>=': return left >= right;
      case '<=': return left <= right;
      case '>':  return left > right;
      case '<':  return left < right;
    }
  }

  // 5. 算术运算
  var mathMatch = trimmed.match(/^(.+?)\s*([+\-*/%])\s*(.+)$/);
  if (mathMatch) {
    var a = Number(parseExpressionValue(mathMatch[1].trim()));
    var b = Number(parseExpressionValue(mathMatch[3].trim()));
    if (!isNaN(a) && !isNaN(b)) {
      switch (mathMatch[2]) {
        case '+': return a + b;
        case '-': return a - b;
        case '*': return a * b;
        case '/': return b !== 0 ? a / b : 0;
        case '%': return b !== 0 ? a % b : 0;
      }
    }
  }

  // 6. 不支持的表达式 → 安全降级（返回原始字符串，由 getFunction try-catch 兜底）
  console.warn('[funEval] 不支持的表达式: ' + trimmed + '，返回原始字符串');
  return trimmed;
}

export var funEval = function (value) {
  if (!value || typeof value !== 'string') return value;
  return parseExpressionValue(value.trim());
};


export var getFunction = function (fun, def) {
  if (!validatenull(fun)) {
    try {
      return funEval(fun);
    } catch (_) {
      return function () {};
    }
  } else if (def) return function () {};
};
export const getJson = (str) => {
  if (validatenull(str)) return {};
  else if (typeof str == "string") {
    try {
      return JSON.parse(str);
    } catch {
      return {}
    }
  } else {
    return str;
  }
}
export const checkUrl = (url) => {
  var reg = /http(s)?:\/\/([\w-.]+)+(:[0-9]+)?.*$/;
  if (!reg.test(url)) {
    return false;
  }
  else {
    return true
  }
}
export const loadScript = (type = 'js', url, dom = "body") => {
  let flag = false;
  return new Promise((resolve) => {
    const head = dom == 'head' ? document.getElementsByTagName('head')[0] : document.body;
    for (let i = 0; i < head.children.length; i++) {
      let ele = head.children[i]
      if ((ele.src || '').indexOf(url) !== -1) {
        flag = true;
        resolve();
      }
    }
    if (flag) return;
    let script;
    if (type === 'js') {
      script = document.createElement('script');
      script.type = 'text/javascript';
      script.src = url;
    } else if (type === 'css') {
      script = document.createElement('link');
      script.rel = 'stylesheet';
      script.type = 'text/css';
      script.href = url;
    }
    head.appendChild(script);
    script.onload = function () {
      resolve();
    };
  });
};

export const dataURLtoFile = (base64, filename) => {
  var arr = base64.split(','),
    mime = arr[0].match(/:(.*?);/)[1],
    bstr = atob(arr[1]),
    n = bstr.length,
    u8arr = new Uint8Array(n);
  while (n--) {
    u8arr[n] = bstr.charCodeAt(n);
  }
  return new File([u8arr], filename, { type: mime });
}

export const getUrlParam = name => {
  let reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
  let r = window.location.search.substr(1).match(reg);
  if (r != null) {
    return unescape(r[2]);
  } else {
    return null;
  }
}
export const computedUrl = (baseUri, imgUri, staticUri = '/DataV') => {
  if (/^(http|https):\/\/([\w.]+\/?)\S*/.test(imgUri)) {
    return imgUri;
  }
  if (/^(data)\S*/.test(imgUri)) {
    return imgUri;
  }
  if (/^\/(DataV)\S*/.test(imgUri)) {
    return imgUri;
  }
  if (/^\/(img)\S*/.test(imgUri)) {
    return staticUri + imgUri;
  }
  if (/^\/(api)\S*/.test(imgUri)) {
    return baseUri + imgUri;
  }
  if (/^\/(dev)\S*/.test(imgUri)) {
    return imgUri;
  }
  return imgUri;
}