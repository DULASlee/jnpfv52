# JNPF v3.6 架构文档归档区

> **适用源码**：JNPF v3.6（及更早）  
> **文档状态**：已归档 · **只读**  
> **维护策略**：禁止在本目录修改内容；仅允许新增自外部快照的归档文件  

---

## 用途

保存 v3.6 时代架构文档的**历史快照**，供运维追溯、技术决策复盘、与 v5.2 文档对照使用。

v5.2 现行架构文档见：[`../../v52/README.md`](../../v52/README.md)

**迁入失败的 hybrid 快照**（含 v3.6 污染，勿作 v5.2 依据）：[`../pre-v52-rewrite/README.md`](../pre-v52-rewrite/README.md)

---

## 归档清单

| 文件 | 来源 | 归档日期 | 说明 |
|------|------|----------|------|
| （待入库） | 外部 v3.6 文档库 / 旧共享目录 | — | 请原样拷贝，勿顺手「优化」 |

---

## 入库操作规范

1. 从 v3.6 文档源**原样复制**至本目录（保留原文件名或加前缀 `v36-`）。  
2. 在文件头部追加（不删原文）：

```markdown
> **归档说明**：JNPF v3.6 历史快照 · 已停止维护 · 归档日期 YYYY-MM-DD
```

3. 在本 README「归档清单」表格登记一行。  
4. **禁止**将 v5.2 内容写入本目录。

---

## 与 v5.2 的主要差异（对照备忘）

| 维度 | v3.6（常见） | v5.2（现行） |
|------|-------------|-------------|
| 表前缀 | `sys_*` 等 | **`BASE_*`** |
| API 层 | 显式 Controller 较多 | **`DynamicApiController` + `*Service`** |
| 启动 | 经典 Startup | **`Serve.Run()` + `AppStartup`** |
| 配置 | 单一 appsettings | **`Configurations/*.json` 扫描** |
| 分层 | Application/Core 常见 | **`modularity/` + `framework/`** |

详细 v5.2 总纲：[`../../v52/00-outline-core-framework.md`](../../v52/00-outline-core-framework.md)
