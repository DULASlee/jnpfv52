# JNPF v5.2 代码基线快照

## 生成时间
2026-05-28（验收同步）

<!-- 实测修正：路径统一 d:\JNPF-v52 -->

## 后端源码
- 路径: d:\JNPF-v52\backend
- 框架: 10.0.202
- 端口: 5000（launchSettings.json）

## 数据库
- Host: (local)\SQLEXPRESS
- 主库: ZXAF_V1_DevTest1
- 调度库: jnpf_sundial
- 用户: sa
- 密码: 1qazxsw2
- admin 密码: 123456（MD5 + AES 加密后传输）

## 前端项目

### PC 前端
- 路径: d:\JNPF-v52\jnpf-web-vue3
- 端口: 3100
- 代理: /dev → localhost:5000

### 大屏前端
- 路径: d:\JNPF-v52\jnpf-web-datascreen
- 端口: 8100
- 代理: /dev → localhost:5000

### 移动端
- 路径: d:\JNPF-v52\jnpf-app-vue3
- 端口: 3800（`scripts/proxy_server.py` 或 HBuilderX 运行）
- API: localhost:5000（`utils/define.js` 已指向 5000）
- H5 发行包: `unpackage/dist/build/web/`（演示前须存在）

## 配置文件

### ConnectionStrings.json
{
  "ConnectionStrings": {
    "ConnectionConfigs": [
      {
        "Domain": "dev_v1.",
        "ConfigId": "default",
        "DBName": "ZXAF_V1_DevTest1",
        "DBType": "SqlServer",
        "Host": "(local)\\SQLEXPRESS",
        "Port": "1433",
        "UserName": "sa",
        "Password": "1qazxsw2",
        "DBSchema": "public"
      },
      {
        "ConfigId": "JNPF-Job",
        "DBName": "jnpf_sundial",
        "DBType": "SqlServer",
        "Host": "(local)\\SQLEXPRESS",
        "Port": "1433",
        "UserName": "sa",
        "Password": "1qazxsw2",
        "DBSchema": "public"
      }
    ]
  }
}

### Cache.json
{
  "Cache": {
    "CacheType": "MemoryCache", // MemoryCache
    "ip": "127.0.0.1",
    "port": 6379,
    "RedisConnectionString": "{0}:{1}, poolsize=500,ssl=false,defaultDatabase=7"

  }
}

### EventBus.json
{
  "EventBus": {
    "EventBusType": "Memory", //Memory,RabbitMQ,Redis
    "HostName": "192.168.0.232",
    "UserName": "jnpf",
    "Password": "jnpf@2019"
  }
}

### Cors.json
{
  "CorsAccessorSettings": {
    "PolicyName": "JNPFCorsAccessor",
    "WithOrigins": [ "http://v1.zlsyun.com", "http://localhost:3100", "http://localhost:3000", "http://localhost:8080", "http://localhost:4173", "http://127.0.0.1:4173", "http://127.0.0.1:3100", "http://114.115.175.162:8092", "http://114.115.175.162:8091", "http://localhost:3800" ],
    "WithExposedHeaders": [ "access-token", "x-access-token", "Content-Disposition" ]
  }
}

## 已知被修改文件
- OAuthService.cs（登录逻辑，密码验证流程）
- MemoryCache.cs（缓存实现）
- UserAgent.cs（UA 解析）
- LoginForm.vue（登录页面）
- basic.ts（PageNotFound 路由，子路由改为 PageNotFoundContent）
- define.js（移动端 API 地址指向 localhost:5000）
- jnpf-app-vue3/scripts/proxy_server.py（H5 演示代理，2026-05-28 新增）
- docs/v52-demo-manual.md（演示脚本，含实测修正）

## VisualData 状态
已引用: <ProjectReference Include="..\..\modularity\visualdata\JNPF.VisualData\JNPF.VisualData.csproj" />

## 密码加密方式
- 前端: 先 MD5(密码) 再 AES 加密
- AES 密钥: EY8WePvjM5GGwQzn
- 后端: 收到后 MD5(+密钥) 与数据库 hash 对比

