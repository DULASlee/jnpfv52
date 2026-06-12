import { createApp } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import App from './App.vue';
import StudentList from './views/student/index.vue';

const routes = [
  { path: '/', redirect: '/student' },
  { path: '/student', name: 'StudentList', component: StudentList },
];

const router = createRouter({ history: createWebHistory(), routes });
const app = createApp(App);
app.use(router);
app.mount('#app');
