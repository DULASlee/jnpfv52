-- 演示品牌：面包树 / Baobab（ZXAF_V1_DevTest1）

-- A. 登录后 UI（F_Category = SysConfig）
UPDATE BASE_SYS_CONFIG SET F_Value = N'Baobab快速开发平台' WHERE F_Category = 'SysConfig' AND F_Key = 'title';
UPDATE BASE_SYS_CONFIG SET F_Value = N'Baobab快速开发平台' WHERE F_Category = 'SysConfig' AND F_Key = 'sysName';
UPDATE BASE_SYS_CONFIG SET F_Value = N'Copyright @ 2026 面包树科技有限公司版权所有' WHERE F_Category = 'SysConfig' AND F_Key = 'copyright';
UPDATE BASE_SYS_CONFIG SET F_Value = N'面包树科技有限公司' WHERE F_Category = 'SysConfig' AND F_Key = 'companyName';
UPDATE BASE_SYS_CONFIG SET F_Value = N'support@baobab.com' WHERE F_Key IN ('companyEmail','emailAccount') AND F_Value LIKE '%yinmaisoft%';
UPDATE BASE_SYS_CONFIG SET F_Value = N'面包树科技' WHERE F_Key = 'emailSenderName' AND F_Value LIKE '%JNPF%';
UPDATE BASE_SYS_CONFIG SET F_Value = N'面包树科技' WHERE F_Key = 'qyh_JoinTitle';

-- B. 批量清理残留（若有 JSON/其它键）
UPDATE BASE_SYS_CONFIG SET F_Value = REPLACE(F_Value, N'引迈信息技术有限公司', N'面包树科技有限公司') WHERE F_Value LIKE N'%引迈%';
UPDATE BASE_SYS_CONFIG SET F_Value = REPLACE(F_Value, N'引迈', N'面包树') WHERE F_Value LIKE N'%引迈%';
UPDATE BASE_SYS_CONFIG SET F_Value = REPLACE(F_Value, N'JNPF快速开发平台', N'Baobab快速开发平台') WHERE F_Value LIKE N'%JNPF快速开发平台%';
UPDATE BASE_SYS_CONFIG SET F_Value = REPLACE(F_Value, N'智轩云', N'Baobab云') WHERE F_Value LIKE N'%智轩云%';
UPDATE BASE_SYS_CONFIG SET F_Value = REPLACE(F_Value, N'智慧信息技术有限公司', N'面包树科技有限公司') WHERE F_Value LIKE N'%智慧信息技术%';
UPDATE BASE_SYS_CONFIG SET F_Value = REPLACE(F_Value, N'yinmaisoft.com', N'baobab.com') WHERE F_Value LIKE N'%yinmaisoft%';
UPDATE BASE_SYS_CONFIG SET F_Value = REPLACE(F_Value, N'jnpfsoft.com', N'baobab.com') WHERE F_Value LIKE N'%jnpfsoft%';

-- 可选：微信公众号 Token（改错会导致微信对接失效，演示可保留 JNPF）
-- UPDATE BASE_SYS_CONFIG SET F_Value = N'BAOBAB' WHERE F_Key = 'wx_GZH_TOKEN';
