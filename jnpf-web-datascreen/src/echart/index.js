// 按需懒加载 37 个图表组件，避免首屏全量加载
const modules = import.meta.glob('./packages/**/*.vue')
const components = {}
let loaded = false

export async function loadChartComponents() {
  if (loaded) return components
  const entries = await Promise.all(
    Object.entries(modules).map(async ([, loader]) => {
      try {
        const mod = await loader()
        return mod.default
      } catch (e) {
        console.warn('[echart] 加载图表组件失败:', e)
        return null
      }
    })
  )
  entries.filter(Boolean).forEach(cmp => {
    if (cmp && cmp.name) components[cmp.name] = cmp
  })
  loaded = true
  return components
}

export default components