# Golden Example #2 — Resource Lifetime / `using var` / F-L1（v4.0 冻结）

> **提交**：`d6117dce` `backend/modularity/system/JNPF.Systems/Common/FileService.cs:446` `UploadFileByType` `FileStream? → using var`  
> **类型**：Resource Lifetime / Deterministic Disposal（单点 1+1 行）  
> **价值**：与 #1 形成异质双样本，证明同一纪律在“异常语义→资源生命周期”两种技术性质上可重复执行

## 冻结的范式（同 #1，仅技术性质不同）

```
发现多个问题（6 Findings）→ 不全部修改
  → 三问定所有权（谁创建/谁拥有/谁释放）
  → 仅 F-L1 满足 6 要素准入，其余 5 项 Stop 冻结
  → 单点 `using var`（语言级 try/finally）
  → 正常/异常均确定释放，行为/契约不变
  → 单提交（1 file 1+1），Build 0 错
  → 小闭环复验 8 项（Scope/Ownership/Normal/Exception/Contract/Test/Purity/Build + 验证限制诚实记录）
```

## 通过的 6 要素（Evidence→Modify）

| 要素 | 本例证据 |
|------|----------|
| 1 Evidence 确认 | `FileService.cs:448` `new FileStream` 有文件:行号，三问已答 |
| 2 Contract | 资源 Contract：创建方释放，异常路径亦释放 |
| 3 单点边界 | `UploadFileByType` 单点局部 file |
| 4 门控通过 | Risk Medium + 非性能 + Budget 低成本 |
| 5 回归路径 | build + 句柄语义（using try/finally）+ 行为不变 |
| 6 不扩 Contract | 签名/错误码不变，未引新架构 |

## 拒绝的 5 项（Evidence→Stop，保持冻结）

- F-L2 `FileDown Close`、F-L3 临时目录、F-J1 路径越界（需跨类白名单）、F-J2 空 catch、F-P LOH（需 BDN）— 均记为 Stop，不因 #2 成功而自动解锁

## 自检对照（与 #1 互补）

- #1 证明：Found 11 → keep 1，异常保栈最小改
- #2 证明：Found 6 → keep 1，资源确定性释放最小改
- 共同纪律：证据→分级→门控→单点→行为保持→单提交→8 项复验→Golden 登记

## 复用指引

本例与 #1 共同构成 v4.0 双 Golden Example（异质技术性质），证明纪律可跨领域重复。下一 Finding 仍需独立过 6 要素，不得因“已有一个资源样本”而批量放行剩余 Stop 项。

## 引用

- 证据链：`../../evidence/class-refactor-expert-v40/pilot-file-lifecycle/P0-Evidence-Pack.md` F-L1 + `../../evidence/class-refactor-expert-v40/first-refactor-file-f01/Evidence-Pack.md` + `Verification-Report.md`
- 提交：`d6117dce`
- 双样本基线：#1 `e45f724a` Exception Preserve Cause + #2 `d6117dce` Resource Lifetime
