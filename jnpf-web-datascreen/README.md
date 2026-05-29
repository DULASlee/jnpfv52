## 一 环境要求

### 1.1 开发环境

- 操作系统：`Windows 10/11`，`MacOS`；
- `Node 16.15.0` 版本以上(某些情况下可能需要安装 `Python3` 环境)；
- `pnpm v8.1.0`及以上版本；
- `Visual Studio Code`(简称 VSCode)

### 1.2 运行环境

`Nginx` 建议使用 1.18.0 及以上版本、`OpenResty` 或 `TongHttpServer 6.0`(国产信创)

## 二 浏览器支持

> 支持现代浏览器，不支持 IE

| IE          | Edge            | Firefox         | Chrome          | Safari          |
| ----------- | --------------- | --------------- | --------------- | --------------- |
| not support | last 2 versions | last 2 versions | last 2 versions | last 2 versions |

## 三 关联项目

> 需要使用下表中的对应分支

| 项目                   | 分支          | 说明                    |
| ---------------------- | ------------- | ----------------------- |
| **后端**(任一后端服务) |               |                         |
| jnpf-java-boot         | v5.2.x-stable | Java 单体后端项目源码   |
| jnpf-java-cloud        | v5.2.x-stable | Java 微服务后端项目源码 |
| jnpf-dotnet            | v5.2.x-stable | .NET 单体后端项目源码   |
| jnpf-dotnet-cloud      | v5.2.x-stable | .NET 微服务后端项目源码 |
| **前端**               |               |                         |
| jnpf-web-vue3          | v5.2.x-stable | 前端主项目(Vue3)源码    |

## 四 使用说明

### 4.1 开发环境

#### 4.1.1 安装 pnpm

> 推荐使用 `pnpm`

在 Windows 的 `PowerShell` 中执行如下命令

```bash
iwr https://get.pnpm.io/install.ps1 -useb | iex
```

MacOS 通过 `Homebrew` 安装 `pnpm` <br/>
若已经安装了 `Homebrew` 软件包管理器，则可以使用如下命令赖安装 pnpm：

```bash
brew install pnpm
```

也可以通过 npm 安装 pnpm

```bash
npm install -g pnpm
或
npm install -g @pnpm/exe
```

#### 4.1.2 安装依赖

使用如下命令安装项目依赖

```bash
pnpm install --registry http://registry.npmmirror.com
```

#### 4.1.3 后端接口配置

修改项目根目录 `.env.development` 中的后端接口地址

- Java 项目本地开发默认地址：`http://localhost:30000`
- .NET 项目本地开发默认地址：`http://localhost:5000`

```bash
# 第5行，后端接口
VITE_PROXY = "http://localhost:30000"
```

#### 4.1.4 本地运行

完成上述操作后，使用如下命令运行前端项目

```bash
pnpm dev
```

### 4.2 运行环境

> 测试或生产环境

如果需要测试或生产环境发布，使用如下命令打包项目

```bash
pnpm build
```

然后将项目根目录下 `/dist/` 中所有的文件上传至服务器。

## 五 常见问题

### 5.1 修改网站标题

打开项目根目录 `/public/config.js` 文件，可以看到 网站标题 等配置。

```bash
# 修改网站标题（第5行）
title: 'JNPF数据大屏',
```

### 5.2 修改本地运行默认端口

打开项目根目录 `vite.config.js` 文件，修改如下配置：

> 如果默认端口被修改，需要前往 jnpf-web-vue3 调整相关配置，详细见 jnpf-web-vue3 项目根目录 README.md 文件说明

```bash
# 修改默认端口（第13行）
port: 8100,
```

### 5.3 代码更新后报错

在开发或打包时报依赖缺失，可以先删除项目根目录下的 `pnpm-lock.yaml` 文件，然后重新执行 `pnpm install` 安装依赖即可解决。
