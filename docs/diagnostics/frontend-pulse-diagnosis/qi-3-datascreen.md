# Qi 3: 数据大屏渲染引擎（jnpf-web-datascreen）

> 诊断日期: 2026-06-08
> 诊断方法: 逐文件追踪数据流，标注每个环节的数据格式和转换方式
> 诊断范围: jnpf-web-datascreen 全链路 — 组件注册 → 配置解析 → 组件渲染 → 数据绑定 → 事件传递

---

## 一、完整架构概览

```
┌──────────────────────────────────────────────────────────────────────┐
│                        jnpf-web-datascreen                           │
├──────────────────────────────────────────────────────────────────────┤
│  registerConfig.js          config.js          option/config.js      │
│  (全局注册/路由/库)         (基础常量)          (默认配置/字典)       │
├──────────────────────────────────────────────────────────────────────┤
│                         Designer (build.vue)                         │
│  ┌──────────┐    ┌──────────────────┐    ┌──────────────────────┐   │
│  │ 图层面板  │    │   画布 (Canvas)   │    │  属性面板 (6 Tab)     │   │
│  │ layer.vue│    │  sketch-rule     │    │ 配置/数据/交互/事件/  │   │
│  │ (树形列表)│    │  container.vue   │    │ 动画/参数             │   │
│  └──────────┘    └──────────────────┘    └──────────────────────┘   │
├──────────────────────────────────────────────────────────────────────┤
│                         Runtime (view.vue)                           │
│  container.vue → subgroup.vue (递归) → 具体组件实例                   │
│       │               │                      │                       │
│       ▼               ▼                      ▼                       │
│  initData()     <component :is=>     echart/common.js mixin          │
│  3路径加载       avue-draggable      updateData() 数据核心            │
└──────────────────────────────────────────────────────────────────────┘
```

### 项目特征

| 维度 | 特征 |
|---|---|
| 框架 | Vue 3 + Options API（非 Composition API） |
| 图表 | ECharts 5 + @kjgl77/datav-vue3 |
| 构建 | Vite + import.meta.globEager |
| 状态 | 全局可变状态 window.$glob（非响应式） |
| 组件通信 | provide/inject + $refs 链 + window.$glob |
| 代码执行 | funEval() = `new Function("return " + str)()` 执行用户代码 |

---

## 二、组件注册机制

### 2.1 三层注册架构

```
Layer 1: registerConfig.js (应用启动时)
  ├── app.use(DataVVue3)           → 注册 DataV 官方组件库
  ├── app.use(JsonViewer)          → JSON 查看器
  ├── app.component('avue-draggable', draggable)  → 拖拽容器
  ├── app.component('avue-highlight', highlight)   → 代码高亮
  ├── app.component('svg-icon', SvgIcon)           → SVG 图标
  └── app.config.globalProperties.$component = app.component

Layer 2: container.vue init() (容器挂载时)
  └── 遍历 { ...echartComponents, ...components }
      └── this.$component(component.name, component)

Layer 3: subgroup.vue init() (子组挂载时)
  └── 遍历 echartComponents
      └── this.$component(component.name, component)  ← 重复注册!
```

### 2.2 ECharts 组件自动发现

```javascript
// src/echart/index.js
let requireComponent = import.meta.globEager('./packages/**/*.vue')
// → 自动扫描 packages/ 下所有 .vue 文件
// → 如: bar.vue → { 'avue-data-bar': BarComponent }
```

**已注册组件清单（推断）:** 柱状图、折线图、饼图、散点图、雷达图、仪表盘、地图、文本、图片、边框、装饰、表格、时间、数据容器等 ~40+ 组件。

### 2.3 设计器配置面板组件

```javascript
// src/option/components.js
// 1. import.meta.globEager('./components/**/*.vue')  → 所有以 Option 结尾的组件
// 2. import.meta.globEager('../components/**/*.vue')  → option.vue 文件
// 3. website.componentsList[].option → eval(cmp)      ← P0 安全风险!
```

**发现问题:** 第 3 步使用 `eval(cmp)` 动态注册组件 — 如果 `website.componentsList` 来自远程配置，就是远程代码执行。

### 2.4 组件命名约定

```javascript
// config.js
export const COMPNAME = 'avue-data-'   // 组件名前缀
export const NAME = 'avue-ref-'        // ref 前缀
export const DEAFNAME = 'avue-drag-'   // 拖拽容器 ref 前缀

// 运行时解析: 'avue-data-' + 'bar' → <avue-data-bar>
```

---

## 三、大屏配置 JSON 结构

### 3.1 完整数据结构

```typescript
// API 返回: { detail: string, component: string, visual: {...} }
// 解析后:
{
  config: {                          // = JSON.parse(detail)
    width: 1920,                     // 设计宽度
    height: 1080,                    // 设计高度
    backgroundColor: '#030c3b',     // 背景色
    backgroundImage: '',             // 背景图 URL
    screen: 'x' | 'y' | 'xy',       // 适配模式
    overflow: false,                 // 超出隐藏
    styles: {                        // CSS 滤镜
      show: false,
      contrast: 100, saturate: 100, brightness: 100,
      opacity: 100, grayscale: 0, hueRotate: 0, invert: 0, blur: 0
    },
    mark: {                          // 水印
      show: false, text: '', fontSize: 16, color: '', degree: -20
    },
    gradeLen: 0,                     // 网格大小
    gradeShow: false,                // 显示网格
    group: [{ name: '主屏幕', id: '' }],  // 分组屏幕
    theme: { ... },                  // 主题配色
    themeId: '',                     // 当前主题 ID
    glob: [],                        // 全局变量
    header: '...',                   // eval → 请求头函数
    query: '...',                    // eval → 请求参数函数
    before: '...',                   // eval → 渲染前钩子
    style: '...',                    // 全局 CSS 注入
    filters: {},                     // 数据过滤器
    url: ''                          // 全局 API 前缀
  },
  component: [                       // 组件树 = nav
    {
      index: 0,                      // 唯一索引 (自增)
      name: '柱状图',                // 图层名
      left: 100,                     // X 位置 (px)
      top: 200,                      // Y 位置 (px)
      display: false,                // 隐藏
      lock: false,                   // 锁定
      group: '',                     // 所属分组屏幕 ID
      auto: false,                   // 轮播
      interval: 3000,               // 轮播间隔
      zIndex: 0,                     // 层级 (运行时计算)
      component: {
        name: 'bar',                 // 组件类型
        prop: 'bar',                 // 组件属性分类
        width: 600, height: 400,    // 尺寸
        opacity: 1, scale: 1,       // 视觉属性
        contrast: 100, saturate: 100, brightness: 100,  // 独立滤镜
        grayscale: 0, hueRotate: 0, invert: 0, blur: 0,
        rotateX: 0, rotateY: 0, rotateZ: 0,             // 3D 旋转
        perspective: 500,
        fontFamily: '',
        animated: 'fadeIn',          // 动画名称
        animatedSwitch: false,       // 启用动画
        animatedInfinite: false,     // 循环动画
        animateDuration: 2,          // 动画时长
        animateDelay: 0,             // 动画延时
        animateSpeed: '',            // 动画速度曲线
        animateDirection: ''         // 动画方向
      },
      option: {                      // 组件专属配置 (图表 option)
        // 如 bar: { barColor, legend, grid, xAxis, yAxis, series, ... }
      },
      dataType: 0,                   // 数据源类型 (0-7)
      data: {},                      // 静态数据
      url: '',                       // API/WS/MQTT 地址
      dataMethod: 'get',             // HTTP 方法
      proxy: false,                  // 代理模式
      time: 0,                       // 轮询间隔 (ms, 0=不轮询)
      sql: '',                       // SQL 配置 (AES 加密)
      record: '',                    // 数据集 ID
      public: '',                    // 公共数据集组件 index
      wsUrl: '', mqttUrl: '', mqttConfig: '',
      // 事件函数 (全部为字符串, 运行时 eval)
      dataFormatter: '',             // 数据过滤器
      clickFormatter: '',            // 点击事件
      dblClickFormatter: '',         // 双击事件
      mouseEnterFormatter: '',       // 鼠标移入
      mouseLeaveFormatter: '',       // 鼠标移出
      dataBeforeFormatter: '',       // 数据渲染前
      dataAfterFormatter: '',        // 数据渲染后
      echartFormatter: '',           // ECharts option 后处理
      labelFormatter: '',            // 标签格式化
      stylesFormatter: '',           // 样式格式化
      formatter: '',                 // 提示框格式化
      child: {                       // 交互配置
        index: [],                   // 目标子组件 index 列表
        paramName: '',               // 传参名
        paramValue: '',              // 取值字段
        paramList: []                // 复杂交互事件列表
      },
      children: [ ... ]              // 子组件 (成组)
    }
  ]
}
```

### 3.2 三种加载路径

```
container.vue → initData()
  │
  ├─ [路径1: src] query.src 存在
  │   axios.get(src) → 取响应体 → 去掉 'const option = ' → JSON.parse
  │   格式: { detail: {...}, component: [...] }
  │
  ├─ [路径2: id] query.id 或 route params.id
  │   getObj(id) → res.data.data.config
  │   config.detail → JSON.parse → 屏幕配置
  │   config.component → JSON.parse → 组件树
  │   额外: 密码保护 / 发布状态检查
  │
  └─ [路径3: option] prop 直接传入
      this.option = { detail: {...}, component: [...] }
```

### 3.3 数据加载后的处理流程

```
initData() → draw()
  │
  ├── 1. setGlobParams()
  │   ├── 注入全局 CSS (config.style → <style>)
  │   ├── header/query/before/style → funEval → window.$glob
  │   └── window.$glob.url = config.url
  │
  ├── 2. 分组初始化 (config.group, window.$glob.group)
  │
  ├── 3. 主题初始化 (window.$glob.themeId → refresh())
  │
  ├── 4. 水印初始化 (watermark())
  │
  ├── 5. calcData() / setScale()
  │
  ├── 6. before 钩子 (config.before → funEval → Promise)
  │   ├── then → 继续
  │   └── catch → 仍然继续 (静默失败!)
  │
  └── 7. contain.nav = contain.component  → 触发渲染
```

---

## 四、组件渲染链路

### 4.1 渲染层级

```
container.vue
  └── <subgroup :nav="contain.list" :key="reload">
        │
        ├── contain.list = computed: nav 扁平化 + group 过滤 + zIndex 赋值
        │
        └── subgroup.vue (递归模板)
              │
              ├── [叶子节点] → <avue-draggable> + <component :is="'avue-data-' + item.component.name">
              │     props 传递: v-bind="item" (整个配置对象)
              │                + component, transfer, initialize
              │                + *Formatter 字符串 (14 个事件函数)
              │                + width, height, disabled, scale
              │
              └── [成组节点] → <folder> (显示选择框) + <subgroup :nav="item.children"> (递归)
```

### 4.2 avue-draggable 容器

每个组件实例被 `<avue-draggable>` 包裹，提供：
- **拖拽移动**: @move → 更新 left/top
- **选中态**: :active-flag="contain.active.includes(item.index)"
- **缩放**: :scale="container.stepScale" (适配画布缩放)
- **尺寸**: :width / :height
- **锁定**: :disabled="!contain.menuFlag"
- **右键菜单**: v-contextmenu

### 4.3 组件实例的 Mixin 链

```
具体组件 (如 bar.vue)
  └── create.js → 注入 mixins: [bem, common]
        │
        ├── bem.js: BEM 命名规范工具
        │
        └── common.js: 核心 mixin (417 行)
              ├── props: 接收所有配置属性 (30+ props)
              ├── computed: dataFormatter, clickFormatter, ..., styleSizeName, styleChartName
              ├── watch: data → updateData(), width/height → updateChart(),
              │         theme → 销毁重建, option → updateChart() (deep),
              │         component.animated* → initAnima()
              └── methods:
                    init()          → 创建 ECharts 实例 + updateChart + updateData
                    updateData()    → 数据加载核心 (100+ 行)
                    updateChart()   → 图表渲染 (由具体组件覆写)
                    updateClick()   → 交互事件传递
                    bindEvent()     → ECharts 原生事件 → formatter 函数
                    getColor()      → 主题色/渐变色获取
                    getItemRefs()   → 遍历父组件 $refs 获取所有组件实例
```

### 4.4 updateChart 覆写模式

```javascript
// 每个图表组件覆写 updateChart()
// 例如 bar.vue:
updateChart() {
  // 1. 从 this.option 读取配置
  // 2. 从 this.dataChart 读取数据
  // 3. 调用 getColor() 获取主题色
  // 4. 组装 ECharts option
  // 5. this.myChart.setOption(option)
}
```

---

## 五、数据源绑定与刷新

### 5.1 8 种数据源类型

| Type | 名称 | 配置字段 | 数据获取方式 |
|------|------|---------|-------------|
| 0 | 静态数据 | `data` | `safe.data` 直接取值 |
| 1 | API 接口 | `url`, `dataMethod`, `proxy`, `dataQuery`, `dataHeader` | axios 请求 |
| 2 | SQL 查询 | `sql` (AES 加密 JSON: {id, sql}) | `sqlFormatter(data)` → 后端执行 |
| 3 | WebSocket | `wsUrl` | `new WebSocket(url)` → onmessage |
| 4 | 数据集 | `record` | `recordFormatter(record)` → 查数据集 → 取 sql → 后端执行 |
| 5 | 公共数据集 | `public` (目标组件 index) | `refList[index].dataChart` 跨组件共享 |
| 6 | MQTT | `mqttUrl`, `mqttConfig` | mqtt.connect + subscribe |
| 7 | Node 代理 | `url`, `proxy=true` | 同 API 但强制代理模式 |

### 5.2 updateData() 核心流程

```
updateData(params)
  │
  ├── resetData()                        // 组件自定义重置
  │
  ├── formatter(data, params)             // 数据过滤器管道
  │   ├── 1. dataOldChart = data         // 保存原始数据
  │   ├── 2. [isRecord] dataFormatter    // 数据集额外过滤
  │   ├── 3. this.dataFormatter(data)    // 用户自定义过滤器 (funEval)
  │   └── 4. dataAfterFormatter(data)    // 渲染后回调
  │
  ├── 分支 (按 dataType):
  │   ├── 0 (Static):  dataChart = formatter(safe.data)
  │   ├── 1 (API):     axios → formatter(res.data)
  │   ├── 2 (SQL):     sqlFormatter(data) → formatter(result)
  │   ├── 3 (WS):      wsClient.onmessage → formatter(JSON.parse)
  │   ├── 4 (Record):  recordFormatter → 解包 sql → sqlFormatter → formatter
  │   ├── 5 (Public):  refList[index].dataChart → formatter(result)
  │   ├── 6 (MQTT):    mqClient.on('message') → formatter(JSON.parse)
  │   └── 7 (Node):    axios (proxy模式) → formatter(res.data)
  │
  ├── bindEvent():
  │   ├── updateChart()                   // 刷新图表
  │   ├── this.myChart.on('click', ...)   // 绑定 4 个 ECharts 事件
  │   ├── stylesFormatter() → this.styles // 动态样式
  │   └── resolve({ news, old })          // 返回结果
  │
  └── 轮询设置:
      ├── isPublic → setInterval(getData, 100)  // 公共数据 100ms 轮询
      └── time > 0 → setInterval(getData, time)  // 自定义轮询
```

### 5.3 数据追加模式

```javascript
// option.dataAppend = true 时启用
// 新数据不替换旧数据，而是追加到 dataChart 头部
// 每次追加间隔 2000ms (硬编码)
updateAppend(result) {
  // 维护 appendList 队列
  // setInterval 每 2 秒取一条追加
}
```

### 5.4 连接生命周期

```javascript
// 每次 updateData() 前关闭旧连接
closeClient() {
  this.wsClient.close && this.wsClient.close()
  this.mqClient.end && this.mqClient.end()
}

// beforeDestroy 时清理
beforeDestroy() {
  clearInterval(this.checkChart);
  this.closeClient()
}
```

---

## 六、交互事件传递

### 6.1 事件系统架构

```
事件源 (图表组件)                 事件传递               事件目标 (其他组件)
    │                               │                         │
    ├─ ECharts 原生事件              │                         │
    │   click/dblclick/             │                         │
    │   mouseover/mouseout          │                         │
    │       │                       │                         │
    │       ▼                       │                         │
    │   bindEvent()                 │                         │
    │   → handleCommonBind()        │                         │
    │       │                       │                         │
    │       ├─ updateClick(item)    │                         │
    │       │   ├─ child.index → ───┼─ refList[i].updateData(p) ──→ 更新目标组件数据
    │       │   └─ transfer() ──────┼─ paramList → 复杂交互 ──────→ 多目标操作
    │       │                       │                         │
    │       └─ clickFormatter() ────┼─ 用户自定义事件函数 ──────→ 自由逻辑
    │                               │
    ├─ 用户自定义事件 (8 种)          │
    │   click/dblClick/             │
    │   mouseEnter/mouseLeave/      │
    │   dataBefore/dataAfter/       │
    │   formatter/labelFormatter    │
    │   → funEval(字符串) → 函数    │
    │                               │
    └─ 组件 ref 查找: getItemRefs() │
        遍历 $parent.$parent.       │
        $parent.$refs               │
        按 NAME/DEAFNAME 前缀匹配   │
```

### 6.2 交互传递类型 (transfer.vue)

| 类型 | 动作 | 配置 |
|------|------|------|
| `params` | 传递数据给子组件 | `index[]` + `child[{name, value}]` → `refList[i].updateData(params)` |
| `href` | 打开外链 | `target` + `src` → `window.open()` |
| `group` | 切换分组屏幕 | `group` → `window.$glob.group = groupId` |
| `display` | 显隐控制 | `index[]` + `displayType` → `refList[i].$el.style.display = ...` |

### 6.3 交互配置数据结构

```typescript
// item.child = {
//   index: [1, 3],         // 目标组件索引数组
//   paramName: 'keyword',  // 传参名
//   paramValue: 'value',   // 源数据取值字段
//   paramList: [           // 复杂交互列表
//     {
//       index: [2],
//       type: 'params',    // params | href | group | display
//       child: [{ name: 'key', value: 'field' }],
//       group: '',         // type=group 时
//       src: '',           // type=href 时
//       target: false,     // type=href 时 新窗口
//       displayType: 'none' // type=display 时
//     }
//   ]
// }
```

---

## 七、缩放适配算法

### 7.1 三种适配模式

```
┌──────────────────────────────────────────────────────────┐
│ screen: 'x'   X轴铺满，Y轴滚动                            │
│   scaleX = viewWidth / configWidth                       │
│   scaleY = scaleX (保持等比)                              │
│   wrapper: overflow-y: auto                              │
├──────────────────────────────────────────────────────────┤
│ screen: 'y'   Y轴铺满，X轴滚动                            │
│   scaleX = viewHeight / configHeight                     │
│   scaleY = scaleX (保持等比)                              │
│   wrapper: overflow-x: auto                              │
├──────────────────────────────────────────────────────────┤
│ screen: 'xy'  双向拉伸 (不保持等比)                        │
│   scaleX = viewWidth / configWidth                       │
│   scaleY = viewHeight / configHeight                     │
└──────────────────────────────────────────────────────────┘
```

### 7.2 containerStyle 计算

```javascript
// container.vue computed
containerStyle() {
  // 1. 计算缩放比
  const widthVal = contain.width / contain.config.width
  const heightVal = contain.height / contain.config.height
  let scaleX = widthVal, scaleY = widthVal
  if (screen == 'y') { scaleX = heightVal; scaleY = heightVal }
  if (screen == 'xy') { scaleX = widthVal; scaleY = heightVal }

  // 2. CSS 滤镜
  wrapperStyle.filter = `contrast(${c}%) saturate(${s}%) brightness(${b}%) ...`

  // 3. 返回 transform + 背景 + 尺寸
  return {
    transform: `scale(${scaleX}, ${scaleY})`,
    width: setPx(config.width),
    height: setPx(config.height),
    backgroundColor: config.backgroundColor,
    background: config.backgroundImage ? `url(...)` : undefined
  }
}
```

### 7.3 设计器缩放

```javascript
// stepScale: 拖拽/缩放时的步进精度
stepScale() {
  return Number(100 / (this.contain.scale * 100))
}
// canvasStyle: 画布 CSS transform
canvasStyle() {
  return {
    transform: `scale(${this.scale})`,  // build.vue data.scale
    overflow: config.overflow ? 'hidden' : ''
  }
}
// 缩放控制: Ctrl+滚轮 → scale ± 0.2
```

---

## 八、主题系统与 CSS 特效

### 8.1 5 套预设主题

```javascript
// option/config.js
theme: {
  '0': { name: '科技蓝', color: ['#00c1de', '#0090ff', '#0077d6', ...] },
  '1': { name: '极简白', color: ['#ffffff', '#f0f0f0', ...] },
  '2': { name: '炫酷青', color: ['#00ffcc', '#00cc99', ...] },
  '3': { name: '高端紫', color: ['#9b59b6', '#8e44ad', ...] },
  '4': { name: '经典红', color: ['#ff6b6b', '#ee5a24', ...] }
}
```

### 8.2 双层 CSS 特效

```
大屏级 (config.styles):
  filter: contrast() saturate() brightness() opacity()
          grayscale() hue-rotate() invert() blur()

组件级 (component.xxx):
  filter: contrast() saturate() brightness() opacity()
          grayscale() hue-rotate() invert() blur()
  transform: scale() perspective() rotateX() rotateY() rotateZ()
```

### 8.3 动画系统 (Animate.css)

```
组件属性:
  animated: 'fadeIn'         → CSS class: animated fadeIn
  animatedSwitch: true/false → 开启动画
  animatedInfinite: true     → CSS class: animated fadeIn infinite
  animateDuration: 2         → animation-duration: 2s
  animateDelay: 0            → animation-delay: 0s
  animateSpeed: 'ease'       → animation-timing-function
  animateDirection: 'alternate' → animation-direction

70+ 动画分为 3 大类:
  移入 (33种): fadeIn, slideIn, bounceIn, zoomIn, rotateIn, flip, rollIn, lightSpeedIn
  强调 (10种): bounce, flash, pulse, heartBeat, rubberBand, shake, swing, tada, wobble, jello
  退出 (30种): fadeOut, slideOut, bounceOut, zoomOut, rotateOut, flipOut, rollOut
```

### 8.4 主题切换机制

```javascript
// mixins/index.js
Object.defineProperty(window.$glob, 'themeId', {
  set: (val) => {
    this.$refs.container.refresh(val)
  }
})

// container.vue
refresh(themeId) {
  // 1. 取主题配色 → window.$glob.theme
  // 2. reload = Math.random()  → 强制重建所有子组件
  // 3. $nextTick → initFun()   → 重新绑定 ref
}
// 整个大屏销毁重建 — 最暴力的刷新方式
```

---

## 九、设计器特有功能

### 9.1 撤销/重做

```javascript
// build.vue
// 防抖 300ms → 深拷贝 nav → history.push()
// 最多 100 条历史
// Ctrl+Z 撤销 / Ctrl+Y 重做
recordHistoryCache(val)  → debounce 300ms → addHistoryCache(val)
addHistoryCache(val)     → splice 多余历史 → push → max 100
editorUndo() / editorRedo() → 恢复历史状态
```

### 9.2 键盘快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+C | 复制组件 |
| Ctrl+V | 粘贴组件 |
| Ctrl+X | 剪切组件 |
| Ctrl+Z | 撤销 |
| Ctrl+Y | 重做 |
| Ctrl+S | 保存 |
| Ctrl+L | 锁定/解锁 |
| Delete | 删除 |
| Space | 拖拽画布模式 |
| Ctrl+滚轮 | 缩放画布 |

### 9.3 成组/解散

- 多选 → 右键 → 成组 → 创建 `children[]`
- folder.vue 显示组边界框（自动计算最小包围矩形）
- 组支持：轮播、锁定、隐藏、删除、解散

### 9.4 分组屏幕

```javascript
// 主屏下可建多个分组屏幕
config.group = [{ name: '主屏幕', id: '' }, { name: '屏幕2', id: 'xxx' }]

// contain.computed.list:
// 过滤: ele.group == window.$glob.group
// 切换: window.$glob.group = 'xxx'
// 运行时 z-index 自动递减分配
```

---

## 十、安全发现

### P0 严重安全隐患

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| DS-1 | `funEval()` = `new Function("return " + str)()` 执行用户代码 | utils.js:41 | 所有 formatter 函数、before/header/query/style 配置均可注入任意 JS |
| DS-2 | `eval(cmp)` 注册组件 | option/components.js:25 | remote `website.componentsList[].option` 可远程执行代码 |
| DS-3 | `.vm 模板语法 ${jnpfToken}` Token 泄露到 URL | routeHelper.ts:177 | 已在 Pulse 1 报告，DataV 自身也有此问题 (swiper.vue:40/53) |
| DS-4 | `window.$glob` 全局可变无防护 | mixins/index.js | 任意组件可修改全局状态，无访问控制 |
| DS-5 | `document.head.appendChild(styleEl)` 无 CSP | mixins/index.js:117 | config.style 直接注入页面，可插入 CSS keylogger |

### P1 架构问题

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| DA-1 | 组件重复注册 (container + subgroup 都注册 echartComponents) | container.vue:137 / subgroup.vue:76 | 冗余 |
| DA-2 | `$refs` 链查找组件实例 (`$parent.$parent.$parent.$refs`) | common.js:313 | 层级变更即崩溃 |
| DA-3 | 主题切换 = 全量销毁重建 (random key) | container.vue:234 | 性能浪费 |
| DA-4 | 公共数据集 100ms 固定轮询 | common.js:560 | 高频轮询无节流 |
| DA-5 | before 钩子 catch 后静默继续 | container.vue:287 | 错误不可见 |
| DA-6 | window.$glob 非响应式，依赖 Object.defineProperty setter | mixins/index.js:72 | 脆弱的设计 |
| DA-7 | Options API 全部逻辑在一个对象里 | 所有组件 | 难以拆分复用 |
| DA-8 | 数据库配置 SQL 明文 AES 加密但密钥硬编码 | build.vue:755 | 假加密 (已知 P0) |

### P2 技术债务

| # | 发现 | 位置 |
|---|---|---|
| DE-1 | `import.meta.globEager` 已废弃 (Vite 4+) | 多处 |
| DE-2 | 每个组件独立创建 ECharts 实例，20+ 组件时内存压力大 | common.js:300 |
| DE-3 | WebSocket/MQTT 无重连机制 | common.js:493 |
| DE-4 | `JSON.parse(JSON.stringify)` 深拷贝大配置性能差 | 多处 |
| DE-5 | 没有 TypeScript | 全局 |
| DE-6 | `dataAppend` 模式硬编码 2000ms 间隔 | common.js:368 |

---

## 十一、数据流矩阵

### 11.1 组件生命周期

```
组件创建
  ├── create.js: 注入 mixins → 设置 name = 'avue-data-' + name
  ├── bem.js: BEM 工具挂载
  ├── common.js: props/computed/watch/methods 全部注入
  ├── 组件自身 data/computed/methods
  └── mounted → init()
        ├── getItemRefs() → refList (同级组件引用)
        ├── initAnima() → CSS 动画 class
        ├── [isChart] → echarts.init(main, theme)
        ├── updateChart() → 首次渲染
        └── updateData() → 首次加载数据
              └── → ... → bindEvent() → 绑定 ECharts 事件

数据刷新
  time > 0 → setInterval → getData() → formatter → dataChart → updateChart()

组件销毁
  beforeDestroy → clearInterval + closeClient()
```

### 11.2 跨组件数据流

```
组件 A (图表)                          组件 B (目标)
    │                                      │
    │ 用户点击图表                          │
    │ → ECharts click 事件                 │
    │ → handleClick(item, index)           │
    │ → handleCommonBind()                 │
    │ → updateClick(params)                │
    │     │                                │
    │     ├─ child.index → refList[i] ─────┼→ updateData({ name: value })
    │     │                                │   → 重新加载数据
    │     │                                │   → updateChart()
    │     │                                │
    │     └─ transfer(paramList) ──────────┼→ params/href/group/display
    │                                      │
    │ 或:                                  │
    │ component B.dataType=5 (public)      │
    │   → refList[publicIndex].dataChart ──┼→ 直接读取组件 A 的数据
    │     100ms 轮询检测变化                │
```

### 11.3 全局状态模型

```
window.$glob = {
  group: '',         // 当前分组屏幕 ID (setter → mixins.group)
  themeId: '',       // 当前主题 ID (setter → container.refresh)
  theme: {},         // 当前主题配色 (refresh 时注入)
  header: {},        // 全局请求头 (funEval(config.header))
  query: {},         // 全局请求参数 (funEval(config.query))
  before: {},        // 渲染前钩子返回值
  style: {},         // 全局样式 (注入 <style>)
  url: '',           // 全局 API 前缀
  [key]: value       // config.glob 自定义全局变量
}
```

---

## 十二、性能观察

| 环节 | 估算耗时 | 说明 |
|---|---|---|
| ECharts 初始化 | 5-20ms/实例 | 20 个图表 ≈ 100-400ms |
| updateData (API) | 取决于网络 | 并发请求无限制 |
| updateChart (setOption) | 1-5ms/图表 | notMerge 模式 |
| 主题切换 (全量重建) | 200-500ms | 销毁+重建所有图表 |
| 缩放适配 | <5ms | CSS transform 纯 GPU |
| 公共数据 100ms 轮询 | 10次/秒 | 高频 JSON 比对 |

**潜在性能瓶颈:**
- 20+ 图表同时 `setOption` 可能造成主线程卡顿
- 公共数据集 100ms 轮询 + JSON.stringify 比对消耗 CPU
- 主题切换全量销毁重建可改为增量更新

---

## 十三、改进建议

1. **P0: 替换 funEval/eval** — 使用沙箱 (iframe/Web Worker) 或 DSL 替代直接代码执行
2. **P0: 添加 CSP (Content Security Policy)** — 禁止 inline style/script
3. **P1: 重构组件引用查找** — `$parent.$parent.$parent.$refs` 改为 provide/inject 注册表
4. **P1: 主题切换改为增量** — 遍历组件调用 updateTheme() 而非销毁重建
5. **P1: 公共数据集轮询改为事件驱动** — 数据源组件 emit change 事件
6. **P2: 迁移到 Composition API** — 提取 useData/useChart/useTheme 等 composables
7. **P2: ECharts 实例池** — 复用不可见图表的实例，减少内存
8. **P2: 迁移到 TypeScript**
