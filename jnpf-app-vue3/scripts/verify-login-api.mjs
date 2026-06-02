/**
 * 联调自检：模拟 App 登录请求（MD5 + AES），不依赖 HBuilderX
 * 用法: node scripts/verify-login-api.mjs
 */
import CryptoJS from 'crypto-js';

const API = process.env.JNPF_API_TARGET || 'http://localhost:5000';
const CIPHER_KEY = 'EY8WePvjM5GGwQzn';

function aesEncrypt(str) {
  const encryptedData = CryptoJS.AES.encrypt(str, CryptoJS.enc.Utf8.parse(CIPHER_KEY), {
    mode: CryptoJS.mode.ECB,
    padding: CryptoJS.pad.Pkcs7,
  }).toString();
  return CryptoJS.enc.Hex.stringify(CryptoJS.enc.Base64.parse(encryptedData));
}

const md5 = CryptoJS.MD5('123456').toString();
const password = aesEncrypt(md5);
const body = new URLSearchParams({
  account: 'admin',
  password,
  code: '',
  timestamp: '',
  origin: 'password',
  grant_type: 'password',
});

const res = await fetch(`${API}/api/oauth/Login`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/x-www-form-urlencoded',
    'jnpf-origin': 'app',
  },
  body: body.toString(),
});
const json = await res.json();
console.log('HTTP', res.status, 'code', json.code, json.msg || '');
if (json.code === 200) {
  console.log('OK: token present =', Boolean(json.data?.token));
  process.exit(0);
}
process.exit(1);
