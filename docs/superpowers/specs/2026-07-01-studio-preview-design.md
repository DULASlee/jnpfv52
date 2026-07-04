# B1 — AI 前端产物独立预览工程设计文档

> **日期**：2026-07-01
> **方案**：A — 扩展现有沙箱镜像装 Node.js
> **分支**：`frontend-architecture-refactor`

---

## 1. 架构概览

```
用户点"预览"
  │
  ▼
AIDevelopmentPipelineService (preview 阶段)
  │  1. StudioWorkspaceHelper.InjectFrontendFiles()
  │     将 generated/*.vue 复制到 studio-preview/src/views/
  │  2. SandboxManager.UploadFilesAsync()
  │     上传完整 studio-preview/ 到沙箱
  │  3. SandboxManager.ExecuteCommandAsync()
  │     在容器内执行 npm install && npx vite --port 4173 --host &
  │  4. 推 SSE preview_ready → 前端渲染 iframe
  │
  ▼
沙箱容器 (:4173) → Vite 热更新 → 用户浏览器实时预览
```

**破坏性**：零。所有改动落在 `studio-preview/`（独立工程）、`SandboxManager`（扩展端口）、`AIDevelopmentPipelineService`（新增阶段）。

---

## 2. 组件设计

### 2.1 `studio-preview/` 壳工程（新建）

目录结构：
```
studio-preview/
├── package.json          ← vue3 + vite + vue-router
├── vite.config.ts        ← alias @/views → src/views/
├── tsconfig.json
├── index.html
└── src/
    ├── main.ts           ← createApp + router
    ├── App.vue           ← <router-view />
    ├── router/
    │   └── index.ts      ← 动态路由 /* → 生成页面
    └── views/            ← [运行时注入] AI 生成的 .vue 文件
        └── .gitkeep
```

**关键设计**：
- `views/` 初始为空，运行时时由 `InjectFrontendFiles()` 填充
- 路由使用 catch-all `/*`，让生成页面自行接管路径
- `vite.config.ts` 不做复杂配置，默认即可

### 2.2 沙箱 Docker 镜像扩展

在现有 Dockerfile 追加 Node.js 20.x：
```dockerfile
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean
```

### 2.3 `SandboxManager` 端口扩展

**`SandboxConfig` 新增字段**：
```csharp
public int PreviewPort { get; set; } = 4173;
```

**`CreateAsync` 端口映射扩展**：
```csharp
args.Append($"-p {config.Port}:8080 ");       // 现有：应用端口
args.Append($"-p {config.PreviewPort}:4173 "); // 新增：预览端口
```

**`SandboxInfo` 新增字段**：
```csharp
public int PreviewPort { get; init; }
public string PreviewUrl => $"http://{Host}:{PreviewPort}";
```

### 2.4 `StudioWorkspaceHelper.InjectFrontendFiles()`

```csharp
public static void InjectFrontendFiles(string generatedDir, string previewProjectDir)
{
    var viewsDir = Path.Combine(previewProjectDir, "src", "views");
    Directory.CreateDirectory(viewsDir);

    var extensions = new[] { "*.vue", "*.ts", "*.css", "*.scss", "*.less" };
    foreach (var pattern in extensions)
    {
        foreach (var file in Directory.GetFiles(generatedDir, pattern, SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(generatedDir, file);
            var dest = Path.Combine(viewsDir, relativePath);
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
```

### 2.5 `AIDevelopmentPipelineService` preview 阶段

新增 API 端点：
```csharp
[HttpPost("{pipelineId:long}/preview")]
public async Task<object> StartPreviewAsync(long pipelineId)
```

流程：
1. 获取 pipeline 的 workspace 路径
2. 将 `generated/` 文件注入到 `studio-preview/src/views/`
3. 确保沙箱存在（如不存在则创建）
4. 上传完整 `studio-preview/` 到容器 `/workspace/studio-preview/`
5. 执行 `cd /workspace/studio-preview && npm install && npx vite --port 4173 --host &`
6. 获取沙箱信息，构造 `previewUrl`
7. SSE 推送 `preview_ready` 事件
8. 返回预览 URL

### 2.6 SSE 事件

```json
{
  "type": "preview_ready",
  "data": {
    "previewUrl": "http://localhost:4174",
    "sandboxId": "pipeline-123",
    "status": "running"
  }
}
```

---

## 3. 文件变更清单

| 文件 | 动作 | 行数 |
|------|------|------|
| `studio-preview/` (整个目录) | **新建** | ~15 文件 |
| `SandboxManager.cs` | 扩展端口映射 + SandboxInfo | ~10 |
| `ISandboxManager.cs` (SandboxConfig/SandboxInfo) | 新增 PreviewPort | ~5 |
| `StudioWorkspaceHelper.cs` | 新增 `InjectFrontendFiles()` | ~25 |
| `AIDevelopmentPipelineService.cs` | 新增 `StartPreviewAsync` | ~50 |
| Dockerfile (sandbox) | 追加 Node.js 安装 | +3 |

---

## 4. 错误处理

| 场景 | 策略 |
|------|------|
| 沙箱不存在 | 自动创建 |
| `npm install` 失败 | 返回错误信息，SSE 推送 `preview_error` |
| Vite 启动超时 (60s) | 超时后推送 `preview_timeout` |
| `generated/` 无 .vue 文件 | 返回提示 "无可预览的前端文件" |
| 容器内端口冲突 | 使用 `--port 4173` 固定端口 |

---

## 5. 验收标准

- [ ] `studio-preview/` 独立 `npm install && npm run dev` 可启动
- [ ] AI 生成的 .vue 注入后可实时预览 (HMR)
- [ ] 预览 URL 通过 SSE `preview_ready` 推送到前端
- [ ] `dotnet build` 零错误
- [ ] 沙箱销毁时预览服务自动终止
