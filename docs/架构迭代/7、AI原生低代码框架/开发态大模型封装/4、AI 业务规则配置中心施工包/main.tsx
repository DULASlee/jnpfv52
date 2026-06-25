import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './index.css';

// 默认用户(实际项目从登录态取)
if (!localStorage.getItem('userId')) {
  localStorage.setItem('userId', 'user_' + Math.random().toString(36).slice(2, 8));
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
