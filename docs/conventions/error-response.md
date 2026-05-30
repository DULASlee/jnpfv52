# JNPF V5.2 统一 API 响应格式

> 框架已内置 `RESTfulResultProvider`（`framework/JNPF/UnifyResult/`），自动包装所有 DynamicApi 返回值。
> 本文档是对已有实现的约定说明，供前后端对齐。

## 响应结构

```json
{
  "code": 200,
  "msg": "操作成功",
  "data": { ... },
  "extras": null,
  "timestamp": 1717056000
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| code | int | HTTP 状态码（200/400/500 等），401 返回 600（前端据此跳转登录） |
| msg | string/object | 成功时为提示文字；失败时为错误信息或校验错误对象 |
| data | any | 业务数据 |
| extras | any | 附加数据（`UnifyContext.Take()`） |
| timestamp | long | UTC 时间戳（秒） |

## 状态码约定

| code | 含义 | 场景 |
|------|------|------|
| 200 | 成功 | 正常返回 |
| 400 | 参数/业务校验失败 | DataValidation 失败、业务规则不满足 |
| 403 | 无权限 | 菜单/按钮/数据权限不足（含未来设备级权限） |
| 500 | 服务器内部错误 | 未捕获异常、FriendlyException |
| 600 | 登录过期 | JWT 过期/无效（前端跳转登录页） |

## 业务异常抛出方式

使用框架 `FriendlyException`（`Oops.Oh()`），禁止直接 `throw new Exception()`：

```csharp
// 正确 — 会被 RESTfulResultProvider 统一包装
throw Oops.Oh("设备名称不能为空");

// 错误 — 会返回原始堆栈，泄露内部信息
throw new Exception("设备名称不能为空");
```

## 前端对接

前端 axios 拦截器按 `code` 分流处理：

| code | 前端行为 |
|------|----------|
| 200 | 正常处理 data |
| 400 | 显示 msg 中的校验提示 |
| 403 | 提示"无权限" |
| 500 | 提示"服务异常" |
| 600 | 清除 token，跳转登录页 |
