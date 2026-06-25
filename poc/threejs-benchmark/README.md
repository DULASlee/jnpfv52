# PoC-B: Three.js 性能基线

**JNPF v5.2 数字大屏 Three.js 渲染能力验证**

## 测试目标

验证 Three.js 在 16G RAM 开发机上渲染 10 万三角面的能力：
- 能否维持 ≥30fps 持续 10 分钟
- 内存是否持续增长（有无泄漏）
- 为 F-6b 全量 3D 大屏组件交付提供数据支撑

## 测试场景

| 指标 | 要求 |
|---|---|
| 几何体 | 10 万面（程序化生成：60 栋建筑 + 80 个设备 + 地形） |
| POI | 20 个 CSS2D 文字标注 |
| 飞线 | 5 条 QuadraticBezierCurve3 动态粒子飞线 |
| 光照 | 环境光 + 方向光（阴影 2048）+ 半球光 |
| 阴影 | PCFSoftShadowMap |
| 后处理 | 无（纯光栅化） |
| 相机 | PerspectiveCamera + OrbitControls（阻尼） |

## 环境要求

| 项目 | 最低要求 |
|---|---|
| Node.js | ≥18 |
| 包管理器 | pnpm（推荐）或 npm |
| 浏览器 | Chrome/Edge（需支持 WebGL 2.0 + CSS2DRenderer） |
| GPU | 集成显卡以上（Intel UHD / AMD Vega / NVIDIA MX） |
| RAM | 16GB 系统内存 |
| 操作系统 | Windows 10+ / macOS 12+ / Linux |

## 使用方法

```bash
# 进入目录
cd poc/threejs-benchmark

# 安装依赖
pnpm install

# 启动开发服务器
pnpm dev
# → http://localhost:3300

# 点击 "开始 10 分钟测试" 按钮
# 让测试运行完整 10 分钟（不要在期间切换标签页或最小化窗口）
# 测试结束后自动显示结果面板
```

## 监控指标

| 指标 | 来源 | 说明 |
|---|---|---|
| FPS | `requestAnimationFrame` 滚动窗口 | 每秒帧数 |
| 面数 | `renderer.info.render.triangles` | GPU 三角面数 |
| DrawCalls | `renderer.info.render.calls` | GPU 绘制调用数 |
| 内存 | `performance.memory.usedJSHeapSize` | JS 堆使用量（Chrome only） |
| 帧时间 | `performance.now()` delta | 单帧耗时 (ms) |

## 通过标准

| 指标 | 阈值 |
|---|---|
| 平均 FPS | ≥30 |
| 低于 30fps 采样占比 | <5% |
| 内存 | 不持续增长（无泄漏） |
| 持续时间 | 完整 10 分钟不崩溃 |

## 结果处置

- ✅ **通过** → 阶段二 F-6b 全量交付（Three.js 3D 大屏组件）
- ❌ **未通过** → 提出 LOD / 2.5D 降级方案，交创始人书面决策

## 文件结构

```
poc/threejs-benchmark/
├── package.json
├── vite.config.ts
├── tsconfig.json
├── index.html
├── README.md              ← 本文件
├── results.md             ← 实测数据（测试后填写）
└── src/
    ├── main.ts             ← 入口 + UI
    ├── scene/
    │   └── BenchmarkScene.ts  ← 场景初始化 + 渲染循环
    └── utils/
        ├── geometry-generator.ts  ← 程序化几何体
        ├── flyline.ts             ← 粒子飞线
        └── monitor.ts             ← 性能监控 + HUD
```

## 技术决策

1. **不使用 glTF 模型加载**：程序化几何体更可控，面数精确，无网络延迟
2. **不使用 Stats.js**：自建监控器可定制采样频率和指标
3. **CSS2D POI 而非 canvas POI**：CSS2D 文本清晰且性能好
4. **OrbitControls 而非固定相机**：测试中允许交互，真实使用场景
5. **PCFSoftShadowMap**：真实阴影质量，反映生产环境开销
