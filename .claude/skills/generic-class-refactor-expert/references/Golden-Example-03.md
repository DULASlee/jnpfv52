# Golden Example #3 — Resource Lifetime / FileDown Deterministic Disposal (F-L2)

> **提交**：`acc6f5d0`  
> **Finding**：F-L2 FileDown `FileStreamResult.FileStream` 手动 `Close()` 异常路径泄漏  
> **Fix**：`using var fs = fileStreamResult.FileStream;` 确定性释放  
> **技术性质**：Resource Lifetime（与 #2 同类，异场景）  
> **业务方向**：下载路径（与 #2 上传路径形成异场景双样本）

## 定位说明

#3 与 #2 属于**同一技术性质（Resource Lifetime）下的两个不同业务场景**：

- **#2**：上传路径 `UploadFileByType` 的 `FileStream` 泄漏
- **#3**：下载路径 `FileDown` 的 `FileStreamResult.FileStream` 泄漏

两者共同验证了 Skill 在 Resource Lifetime 原则下的**可重复性**：

> 不同业务方向（上传 vs 下载）、不同代码位置、不同 ownership 模式，但同一套专家判断流程：
>
> 发现 → 三问 ownership → Gate → 单点修复 → 正常/异常验证 → 契约验证 → 关闭

## 基线结构

```
v4.0 类级专家重构 Skill
│
├── Exception Semantics
│   └── Golden #1 — Preserve Cause (e45f724a)
│
└── Resource Lifetime
    ├── Golden #2 — UploadFileByType `using var` (d6117dce)
    └── Golden #3 — FileDown `using var` (acc6f5d0)
```

## 价值

#2 与 #3 共同证明：

> Skill 不只是偶然成功修复某一个 `using var` 场景，而是在同一资源生命周期原则下，对不同业务方向的真实代码都能够执行相同的专家判断。

这比简单增加 Finding 数量更有价值。

## 下一步指引

**不要为了凑 Golden Example 数量而连续修 Resource Lifetime Finding。**

下一步应回到"类级剩余风险盘点/选择"，从剩余 Stop Findings 中按证据、风险、收益和改造半径重新选择最值得推进的一个。

## 引用

- Gate Pack：`../d4-f05-gate/Gate-Pack.md`
- Evidence Pack：`../d4-f05-fix/Evidence-Pack.md`
- Verification Report：`../d4-f05-fix/Verification-Report.md`
- Diff：`../d4-f05-fix/diff.patch`
- 提交：`acc6f5d0`
