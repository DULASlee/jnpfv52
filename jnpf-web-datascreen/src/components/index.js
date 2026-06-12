/**
 * 自定义组件参考文档
 * https://cn.vuejs.org/v2/guide/components-registration.html
*/
import { website } from '@/config.js'
import $Echart from '../echart/common';
import { KEY_COMPONENT_NAME } from '../echart/variable';
export default (() => {
  let components = {}
  const mixins = [$Echart]

  const requireComponent = import.meta.globEager('./**/**/*.vue')

  // Build component registry by name (for safe lookup, replacing eval)
  const nameRegistry = {}
  Object.keys(requireComponent).forEach(fileName => {
    const cmp = requireComponent[fileName].default
    if (cmp && cmp.name) {
      nameRegistry[cmp.name] = cmp
      const baseName = fileName.split('/').pop().replace(/\.vue$/, '')
      if (!nameRegistry[baseName]) {
        nameRegistry[baseName] = cmp
      }
    }
  })

  Object.keys(requireComponent).forEach(fileName => {
    if (fileName.includes('index.vue')) {
      const cmp = requireComponent[fileName].default
      cmp.mixins = mixins
      components[`${KEY_COMPONENT_NAME}${cmp.name}`] = cmp
      cmp.name = `${KEY_COMPONENT_NAME}${cmp.name}`
      components[cmp.name] = cmp
    }
  })

  website.componentsList.forEach(ele => {
    const cmpName = ele.component
    const cmp = nameRegistry[cmpName]
    if (!cmp) {
      console.warn('[components] 未找到动态组件: ' + cmpName)
      return
    }
    cmp.mixins = mixins
    cmp.name = KEY_COMPONENT_NAME + cmp.name
    components[cmp.name] = cmp
  })

  return components
})()
