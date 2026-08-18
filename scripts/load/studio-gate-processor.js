/**
 * artillery 负载测试 — JNPF Auth Processor
 *
 * 在 load test 中预获取 token，设置到变量中供后续请求使用。
 *
 * 用法：artillery run 会自动加载此文件（配置中 processor 字段）
 */

function jnpfAuthProcessor(req, context, ee, next) {
  // 占位：未来支持从 jnpf-auth.mjs 的 cache 文件读取 token
  // 当前 artillery scenarios 的接口多为公开端点或使用已有 pipeline
  return next();
}

module.exports = { jnpfAuthProcessor };
