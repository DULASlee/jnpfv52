#jnpf-app-vue3

## 一 环境要求

### 1.1 开发环境

- 操作系统：`Windows 10/11`，`MacOS`；
- `Node.js` v16.15.x(某些情况下可能需要安装 Python3)及以上版本；
- `HBuilder X` (最新版)

### 1.2 运行环境

`Nginx` 建议使用 `1.18.0` 及以上版本、兼容 `OpenResty` 或 `TongHttpServer` 6.0(国产信创)

## 二 关联项目

> 需要使用下表中的对应分支

| 项目						| 分支			| 说明										|
| -------------------------	| -------------	| ------------------------------------------|
| **后端**(任一后端服务)	|				|											|
| jnpf-java-boot			| v5.2.x-stable	| Java 单体项目源码							|
| jnpf-java-cloud			| v5.2.x-stable	| Java 微服务项目源码						|
| jnpf-dotnet				| v5.2.x-stable	| .NET 单体项目源码							|
| jnpf-dotnet-cloud			| v5.2.x-stable	| .NET 微服务项目源码						|

## 三 使用说明

### 3.1 高德地图配置

打开 `/utils/define.ts` 配置文件，修改 `aMapWebKey` 值

打开 `manifest.json` 文件，点击app模块配置，修改高德地图相关配置信息。点击web配置，修改高德地图相关配置信息。
