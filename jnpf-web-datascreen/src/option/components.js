
import { website } from '@/config.js'

let requireComponent = import.meta.globEager('./components/**/*.vue')
let components = {}
const key = "Option"

Object.keys(requireComponent).forEach(fileName => {
  const cmp = requireComponent[fileName].default
  components[cmp.name + key] = cmp
})

requireComponent = import.meta.globEager('../components/**/*.vue')

// Build unified component registry from all globbed modules
const optionRegistry = {}
function buildRegistry(modules) {
  for (const [fileName, mod] of Object.entries(modules)) {
    const cmp = mod.default
    if (!cmp || !cmp.name) continue
    if (!optionRegistry[cmp.name]) {
      optionRegistry[cmp.name] = cmp
    }
    const baseName = fileName.split('/').pop().replace(/\.vue$/, '')
    if (!optionRegistry[baseName]) {
      optionRegistry[baseName] = cmp
    }
  }
}
buildRegistry(import.meta.globEager('./components/**/*.vue'))
buildRegistry(requireComponent)

Object.keys(requireComponent).forEach(fileName => {
  if (fileName.includes('option.vue')) {
    const cmp = requireComponent[fileName].default
    components[cmp.name + key] = cmp
  }
})

website.componentsList.forEach(ele => {
  const cmpName = ele.option
  const cmp = optionRegistry[cmpName]
  if (cmp) {
    components[cmp.name + key] = cmp
  } else {
    console.warn('[option/components] 未找到图表选项: ' + cmpName)
  }
})

export default {
  components: components
}
