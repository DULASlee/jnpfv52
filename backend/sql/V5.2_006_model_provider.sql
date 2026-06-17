-- ============================================================
-- Sprint 7: 模型供应商配置 — 实现 LLM 大模型可配置
-- 功能: 管理供应商凭证(API Key + Base URL)，不改源代码即可切换/新增供应商
-- ============================================================

-- 1. 供应商凭证表
CREATE TABLE BASE_AI_MODEL_PROVIDER (
    F_Id              BIGINT          PRIMARY KEY,
    F_ProviderCode    NVARCHAR(50)    NOT NULL,
    F_Name            NVARCHAR(100)   NOT NULL,
    F_BaseUrl         NVARCHAR(500)   NOT NULL,
    F_ApiKey          NVARCHAR(500)   NOT NULL,
    F_DefaultModel    NVARCHAR(100),
    F_MaxTokens       BIGINT          NOT NULL DEFAULT 1000000,
    F_Temperature     DECIMAL(5,2)    NOT NULL DEFAULT 0.7,
    F_Status          NVARCHAR(20)    NOT NULL DEFAULT 'healthy',
    F_Priority        INT             NOT NULL DEFAULT 1,
    F_Enabled         BIT             NOT NULL DEFAULT 1,
    F_Description     NVARCHAR(500),
    F_LastTestTime    DATETIME,
    F_LastTestResult  NVARCHAR(2000),
    F_CreatorTime     DATETIME        NOT NULL,
    F_CreatorUserId   BIGINT,
    F_ModifyTime      DATETIME,
    F_ModifyUserId    BIGINT,
    F_DeleteMark      BIT             NOT NULL DEFAULT 0,
    CONSTRAINT UQ_PROVIDER_CODE UNIQUE (F_ProviderCode)
);

CREATE INDEX IX_PROVIDER_ENABLED ON BASE_AI_MODEL_PROVIDER(F_Enabled, F_Priority, F_DeleteMark);

-- 2. 种子数据
INSERT INTO BASE_AI_MODEL_PROVIDER
    (F_Id, F_ProviderCode, F_Name, F_BaseUrl, F_ApiKey, F_DefaultModel, F_MaxTokens, F_Temperature, F_Status, F_Priority, F_Enabled, F_Description, F_CreatorTime)
VALUES
    (100000001, 'deepseek', N'DeepSeek',
     'https://api.deepseek.com', 'YOUR_DEEPSEEK_API_KEY_HERE',
     'deepseek-v4-pro', 2000000, 0.7, 'healthy', 1, 1,
     N'DeepSeek V4 Pro — 国产高性价比，2M 上下文', GETDATE()),

    (100000002, 'mimo', N'MiMo',
     'https://api.mimo.xiaomi.com', 'YOUR_MIMO_API_KEY_HERE',
     'mimo-2.5-pro', 2500000, 0.7, 'healthy', 2, 1,
     N'MiMo 2.5 Pro — 小米大模型，2.5M 上下文', GETDATE()),

    (100000003, 'tongyi', N'通义千问',
     'https://dashscope.aliyuncs.com/api/v1', 'YOUR_TONGYI_API_KEY_HERE',
     'qwen-max', 1000000, 0.7, 'offline', 3, 1,
     N'通义千问 — 阿里生态，1M 上下文', GETDATE()),

    (100000004, 'openai', N'OpenAI',
     'https://api.openai.com/v1', 'YOUR_OPENAI_API_KEY_HERE',
     'gpt-4o', 1000000, 0.7, 'offline', 4, 1,
     N'OpenAI GPT-4o — 通用能力最强，1M 上下文', GETDATE()),

    (100000005, 'ollama', N'本地模型 (Ollama)',
     'http://localhost:11434/v1', 'ollama',
     'llama3', 4096000, 0.7, 'offline', 5, 1,
     N'本地 Ollama 离线模型 — 4096k 上下文，无需 API Key', GETDATE());
