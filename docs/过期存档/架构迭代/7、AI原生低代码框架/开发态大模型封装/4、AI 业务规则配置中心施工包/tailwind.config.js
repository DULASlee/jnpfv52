/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        // 状态色:绿(AI 生成)/ 黄(人类修改)/ 红(Validator 失败)
        ai: { DEFAULT: '#10b981', light: '#d1fae5' },
        human: { DEFAULT: '#f59e0b', light: '#fef3c7' },
        failed: { DEFAULT: '#ef4444', light: '#fee2e2' },
        pending: { DEFAULT: '#6b7280', light: '#f3f4f6' },
      },
    },
  },
  plugins: [],
};
