import { createRouter, createWebHistory } from 'vue-router'

// Auto-import all .vue files from views/ directory
const modules = import.meta.glob('/src/views/**/*.vue')

const routes = Object.keys(modules).map((path) => {
  // /src/views/Foo/Bar.vue → /foo/bar
  const routePath = path
    .replace('/src/views', '')
    .replace(/\.vue$/, '')
    .replace(/\/index$/, '')
    .toLowerCase()
    .replace(/\[(\w+)\]/g, ':$1') // [id] → :id
  return {
    path: routePath || '/',
    component: modules[path],
  }
})

// Add catch-all for any unmatched routes
routes.push({
  path: '/:pathMatch(.*)*',
  component: () => import('/src/views/Index.vue').catch(() => ({
    template: '<div style="padding:2rem;font-family:sans-serif"><h2>JNPF Studio Preview</h2><p>No generated pages found. Generate code first, then preview.</p></div>'
  })),
})

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
