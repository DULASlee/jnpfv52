// API 客户端 - 与后端 SA SDK 对接
import axios from 'axios';

const client = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

// 请求拦截器:注入 userId
client.interceptors.request.use((config) => {
  const userId = localStorage.getItem('userId') || 'anonymous';
  config.headers['X-User-Id'] = userId;
  return config;
});

// 响应拦截器:统一错误处理
client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default client;
