# GeneratedModule — AI 生成业务模块

> 由 JNPF AI 原生低代码平台确定性编译器生成（零 LLM 代码生成）

## 模块信息
- **系统名**: GeneratedModule
- **编译后端**: csharp-monolithic
- **实体数**: 1
- **技术栈**: csharp + vue3

## 实体清单
- `LeaveRequest`

## 安装步骤
1. 将本目录复制到 `backend/modularity/generated/`
2. 在 API.Entry.csproj 添加 ProjectReference
3. 执行 `Migrations/init.sql` 创建数据库表
4. 导入 `Configurations/menus.json` 注册菜单
5. 将 `frontend/` 复制到前端项目的 views 目录
6. 重启后端（JnpfModule 自动发现注册）
