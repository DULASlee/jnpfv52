/**
 * 数字大屏编译器
 * DashboardIR → GeneratedProject
 */
import type { DashboardIR } from '../../ir/dashboard-types';
import type { CompileResult, GeneratedProject } from '../vue3/types';

const MARKER = '@jnpf-generated';
const VERSION = '1.0.0';

export class DashboardCompiler {
  private ir: DashboardIR;

  constructor(ir: DashboardIR) {
    this.ir = ir;
  }

  compile(): CompileResult {
    const project: GeneratedProject = new Map();
    const entity = this.ir.id;

    project.set('package.json', this.genPackage());
    project.set('vite.config.ts', this.genViteConfig());
    project.set('index.html', this.genHtml());
    project.set('src/main.ts', this.genMain());
    project.set('src/App.vue', this.genApp());
    project.set(`src/views/${entity}/index.vue`, this.genPage());
    project.set('src/composables/useChartData.ts', this.genChartData());
    project.set('src/composables/useDashboardScale.ts', this.genScale());
    project.set('src/styles/theme.css', this.genTheme());
    project.set(`src/config/${entity}.config.json`, `// ${MARKER} dashboard=${this.ir.id}\n` + JSON.stringify(this.ir, null, 2));

    return { project, warnings: [], complexExpressions: [] };
  }

  // ── generators ──

  private genPackage(): string {
    return `// ${MARKER} v${VERSION} dashboard=${this.ir.id}
{
  "name": "jnpf-dashboard-${this.ir.id}",
  "version": "1.0.0",
  "private": true,
  "scripts": { "dev": "vite", "build": "vite build", "preview": "vite preview" },
  "dependencies": { "vue": "^3.4.0", "echarts": "^5.5.0", "vue-echarts": "^6.7.0", "axios": "^1.7.0" },
  "devDependencies": { "vite": "^5.4.0", "@vitejs/plugin-vue": "^5.1.0", "typescript": "^5.5.0" }
}`;
  }

  private genViteConfig(): string {
    return `// ${MARKER}
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
export default defineConfig({ plugins: [vue()], server: { port: 8100 } })`;
  }

  private genHtml(): string {
    return `<!-- ${MARKER} dashboard=${this.ir.id} -->
<!DOCTYPE html><html lang="zh"><head><meta charset="UTF-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1.0"/>
<title>${this.ir.name}</title></head><body><div id="app"></div>
<script type="module" src="/src/main.ts"></script></body></html>`;
  }

  private genMain(): string {
    return `// ${MARKER}
import { createApp } from 'vue'
import App from './App.vue'
import './styles/theme.css'
createApp(App).mount('#app')`;
  }

  private genApp(): string {
    const pagePath = `./views/${this.ir.id}/index.vue`;
    return `<!-- ${MARKER} -->
<template><${this.ir.id}Page /></template>
<script setup lang="ts">import ${this.ir.id}Page from '${pagePath}'</script>`;
  }

  private genPage(): string {
    const w = this.ir.size.width;
    const h = this.ir.size.height;
    const bg = this.ir.background.type === 'color' ? this.ir.background.value : '#0a0a2e';

    return `<!-- ${MARKER} dashboard=${this.ir.id} -->
<template>
  <div
    ref="containerRef"
    class="dashboard-wrapper"
    :style="{ width: '${w}px', height: '${h}px', background: '${bg}', transform }"
  >
${this.ir.widgets
  .map(
    w => `    <div class="widget" :style="widgetStyle('${w.id}', ${w.position.x}, ${w.position.y}, ${w.position.w}, ${w.position.h}, ${
      w.position.zIndex ?? 1
    })">
      <${w.type} :data="dataStore['${w.dataSourceId || ''}']" v-bind="widgetProps['${w.id}']" />
    </div>`,
  )
  .join('\n')}
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, reactive } from 'vue'
import { useDashboardScale } from '../composables/useDashboardScale'
import { useChartData } from '../composables/useChartData'

const containerRef = ref<HTMLElement>()
const { transform } = useDashboardScale(${w}, ${h})

const widgetProps = reactive<Record<string, any>>({})
const dataStore = reactive<Record<string, Record<string, unknown>>>({})
const dataTimers: number[] = []

function widgetStyle(id: string, x: number, y: number, w: number, h: number, z: number) {
  return { position: 'absolute', left: x + 'px', top: y + 'px', width: w + 'px', height: h + 'px', zIndex: z }
}

onMounted(() => { /* init */ })
onUnmounted(() => dataTimers.forEach(clearInterval))
</script>

<style scoped>
.dashboard-wrapper { position: relative; overflow: hidden; transform-origin: top left; }
.widget { overflow: hidden; }
</style>`;
  }

  private genChartData(): string {
    return `// ${MARKER} dashboard=${this.ir.id}
import { ref, onUnmounted } from 'vue'
import axios from 'axios'

export function useChartData(url?: string, pollInterval?: number) {
  const data = ref<unknown>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  let timer: ReturnType<typeof setInterval> | null = null

  async function fetch() {
    if (!url) return
    loading.value = true
    try { const res = await axios.get(url); data.value = res.data; error.value = null }
    catch (e) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  if (pollInterval && pollInterval > 0) {
    fetch()
    timer = setInterval(fetch, pollInterval)
  }

  onUnmounted(() => { if (timer) clearInterval(timer) })
  return { data, loading, error, fetch }
}`;
  }

  private genScale(): string {
    return `// ${MARKER}
import { ref, onMounted, onUnmounted } from 'vue'

export function useDashboardScale(designW: number, designH: number) {
  const scaleX = ref(1)
  const scaleY = ref(1)

  function resize() {
    const s = Math.min(window.innerWidth / designW, window.innerHeight / designH)
    scaleX.value = s
    scaleY.value = s
  }

  onMounted(() => { resize(); window.addEventListener('resize', resize) })
  onUnmounted(() => window.removeEventListener('resize', resize))

  return { scaleX, scaleY, transform: computedString(scaleX, scaleY) }
}

// inline to avoid import dependency
import { computed } from 'vue'
function computedString(sx: ReturnType<typeof ref<number>>, sy: ReturnType<typeof ref<number>>) {
  return computed(() => \`scale(\${sx.value},\${sy.value})\`)
}`;
  }

  private genTheme(): string {
    return `/* ${MARKER} dashboard=${this.ir.id} */
:root { --bg-primary: #0a0a2e; --bg-panel: rgba(6,30,93,0.5); --text-primary: #fff; --accent: #00d4ff; }`;
  }
}
