# JNPF v5.2 干净工作区

单一目录、单一版本，与 `d:\liu202505v2`（含 3.6 残留）隔离。

## 目录结构

```
d:\JNPF-v52\
├── backend\                 # .NET 6 API（application / framework / modularity / infrastructure）
├── jnpf-web-vue3\           # PC 前端 → http://localhost:3100
├── jnpf-web-datascreen\     # 大屏前端 → http://localhost:8100/DataV/
├── jnpf-app-vue3\           # UniApp 移动端（HBuilderX 或已构建 web + proxy）
└── docs\                    # 演示手册、基线快照、架构文档（路径已指向本目录）
```

## 快速启动

```powershell
# 1. 后端（需本机 SQL Server 与 ConnectionStrings.json）
cd d:\JNPF-v52\backend
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj

# 2. PC 前端
cd d:\JNPF-v52\jnpf-web-vue3
pnpm run dev

# 3. 大屏
cd d:\JNPF-v52\jnpf-web-datascreen
pnpm run dev
```

API 基址：`http://localhost:5000`（大屏 `.env.development`、移动端 `utils/define.js` 已对齐）

## 来源

| 组件 | 来源 |
|------|------|
| backend + jnpf-web-vue3 | `d:\liu202505v2` |
| jnpf-web-datascreen + jnpf-app-vue3 | `d:\jnpfBAK\v52-project\web\jnpf-v52-webn\` |

## 文档

- 演示：`docs\v52-demo-manual.md`
- 基线：`docs\v52-baseline-snapshot.md`

`d:\liu202505v2` 保留为存档，日常开发以本目录为准。
