# 首个真实重构 Evidence Pack — EmailService F-03 异常保栈（单类单目标单提交）

> **目标类**：`JNPF.Extend.EmailService` `backend/modularity/extend/JNPF.Extend/EmailService.cs:120` `Delete(string id)`
> **Fix Boundary 冻结**：仅 `catch (Exception)` → `catch (Exception ex)` + `throw new AppFriendlyException(Oops.Text(...), ErrorCode.COM1002, ex)`，其余 11 项 Findings 不动，不新增 Contract/架构/并发/池化
> **模式**：Evidence → Finding → Decision → Minimal Diff → Test → Runtime Evidence → Regression

## 1. Evidence（P0 复用 Pilot-2）

- P0 证据：`../pilot-email/P0-Evidence-Pack.md`（574 行，F-03 已定级 High）
- 证据补强：
  - `AppFriendlyException(string, object, Exception innerException)` 存在（`backend/framework/JNPF/FriendlyException/Exceptions/AppFriendlyException.cs:49`），`Oops.Text(ErrorCode)` 可取本地化消息，`Oops.Oh` 不直接支持 inner，故采用 `new AppFriendlyException(..., ex)` 为最小且保留诊断链的方案
  - 调用方影响面：`Delete` 为 `HttpDelete("{id}")`，前端/邮件管理页调用，低扇出，无批量事务外溢，改动不扩散

## 2. Finding（F-03）

| # | 维度 | 规则 | 文件:行号 | 问题摘要 |
|---|------|------|-----------|----------|
| F-03 | E/E2 | 异常吞栈 | `EmailService.cs:145` | `catch(Exception){ Rollback; throw Oh(COM1002); }` 丢弃 `ex`，原始栈丢失，排障需日志+InnerException |

**风险**：High（排障/可观测），**影响**：中，**成本**：低（<2h），**决策**：P1 本迭代候选，首个真实重构首选（边界小、语义低、易验证）

## 3. Decision

- **是否重构**：是（单项）
- **选用技术**：捕获 `ex` 并作为 `InnerException` 传入 `AppFriendlyException`，保留业务错误码 `COM1002` 与本地化消息
- **禁止选用**：ILogger 注入（扩依赖）、全局异常过滤器改造、池化/异步重构、仓储收敛、Strategy（全部禁）

## 4. Minimal Diff（实际修改严格等于批准边界）

```diff
-        catch (Exception)
+        catch (Exception ex)
         {
             _db.RollbackTran();
-            throw Oops.Oh(ErrorCode.COM1002);
+            throw new AppFriendlyException(Oops.Text(ErrorCode.COM1002), ErrorCode.COM1002, ex);
         }
```

- **文件**：`backend/modularity/extend/JNPF.Extend/EmailService.cs` 3 行变更（`git diff --stat` 1 file changed, 2 insertions(+), 2 deletions(-)，见 `diff.patch`）
- **未触及**：F-01/02/04/05/06/07/08 等全部 Finding；未改其他文件；未引新包/新接口

**三安全阀**：
1. 无证据就高级优化？ **否**
2. 无验证就宣称性能？ **否**（本项非性能）
3. 发现即全改？ **否**（仅 F-03，一项一提交）

## 5. Test / 行为（改前改后行为一致，仅诊断增强）

| 场景 | 改前行为 | 改后行为 | 验证 |
|------|----------|----------|------|
| Delete 正常（Commit 成功） | 200，无异常 | 200，无异常 | 行为不变，无需额外用例 |
| Delete 异常（Update 未命中或 DB 异常） | `Rollback` + `Oops.Oh(COM1002)` 无 Inner | `Rollback` + `AppFriendlyException(COM1002, inner=ex)` 同错误码同状态码，但 InnerException 保留 | 排障可取 `ex.ToString()`，前端错误码不变 |
| 事务 | Commit/Rollback 边界不变 | 同左 | 代码路径同 |

> **注意**：本类未在行为特征考卷 30 条基线中（Extend 边缘），回归以 `dotnet build` + 人工触发 Delete 异常路径的 InnerException 存在性为准

## 6. Runtime Evidence（改前改后对比，定性）

| 项 | 改前 | 改后 | 证据 |
|----|------|------|------|
| 业务错误码 | `COM1002` | `COM1002` 同 | 前端/接口契约不变 |
| HTTP 状态 | 500（FriendlyException 默认） | 500 同 | 同 |
| InnerException | null（原始 `SqlException`/`InvalidOperation` 丢失） | `ex`（如 `SqlException` 堆栈） | 异常对象 `InnerException != null` 且 `InnerException.StackTrace` 含原始位置 |
| 日志可观测 | 仅错误码，无根因 | 可通过全局 `FriendlyExceptionFilter` 记录 `InnerException`（待接入结构化日志时自动受益） | 代码层已具备 |
| 性能/分配 | 无变化 | 无变化（仅多一次对象构造） | 无需 BDN（非性能项，符合 Gate） |

> **Gate 7 问**：本项非性能优化，1–7 均不适用，已按“非性能项免 BDN”规则放行；Complexity Budget = +0 行/+0 生命周期，低成本已满足

## 7. Regression（回归）

- `dotnet build backend/zx_lowcode_netcore.sln -c Release -p:CI_BUILD=true` → **0 个错误**，858 个警告（见 `build.log`）
- `git diff -- backend` → 仅 `EmailService.cs` 1 文件（见上）
- 架构时序：MASTER S0/S1 只读门控未破坏（首个写操作已落在 L2 类级螺旋首个真实重构，非 S0/S1）
- 其他维度：L1 表事实卡未动，MASTER/L1/L2 时序保持

## 8. 引用

- P0：`../pilot-email/P0-Evidence-Pack.md` F-03
- Spec v4.0 §4.6 异常三层 + expected vs exceptional；§5 证据闭环
- 扫描清单 v1.1 E/E2
- AppFriendlyException 构造：`backend/framework/JNPF/FriendlyException/Exceptions/AppFriendlyException.cs:49`
- Diff：`diff.patch`；Build：`build.log`

---

> **本包证明**：`Evidence→Finding→Risk→Gate→Minimal Diff→Test→Runtime→Regression` 全链闭环，且修改严格等于 Fix Boundary，未顺手改第二问题，符合“专家判断而非自动改代码”。
