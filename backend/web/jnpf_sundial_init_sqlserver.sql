-- ============================================================
-- jnpf_sundial 数据库初始化脚本 (SQL Server 版本)
-- 由 Oracle 版本转换而来
-- ============================================================

CREATE DATABASE [jnpf_sundial];
GO

USE [jnpf_sundial];
GO

-- ----------------------------
-- Table: JOBCLUSTER (作业集群表)
-- ----------------------------
CREATE TABLE [JOBCLUSTER] (
    [ID]            INT             IDENTITY(1,1) NOT NULL,
    [CLUSTERID]     NVARCHAR(64)    NOT NULL,
    [DESCRIPTION]   NVARCHAR(128)   NULL,
    [STATUS]        BIT             NOT NULL,
    [UPDATEDTIME]   DATETIME        NULL,
    CONSTRAINT [PK_JOBCLUSTER_ID] PRIMARY KEY CLUSTERED ([ID])
);
GO

-- ----------------------------
-- Table: JOBDETAILS (作业信息表)
-- ----------------------------
CREATE TABLE [JOBDETAILS] (
    [ID]                  INT             IDENTITY(1,1) NOT NULL,
    [JOBID]               NVARCHAR(64)    NOT NULL,
    [GROUPNAME]           NVARCHAR(128)   NULL,
    [JOBTYPE]             NVARCHAR(128)   NULL,
    [ASSEMBLYNAME]        NVARCHAR(128)   NULL,
    [DESCRIPTION]         NVARCHAR(128)   NULL,
    [CONCURRENT]          BIT             NOT NULL,
    [INCLUDEANNOTATIONS]  BIT             NOT NULL,
    [PROPERTIES]          NVARCHAR(MAX)   NULL,
    [UPDATEDTIME]         DATETIME        NULL,
    [CREATETYPE]          INT             NOT NULL,
    [SCRIPTCODE]          NVARCHAR(MAX)   NULL,
    [TENANTID]            NVARCHAR(50)    NULL,
    CONSTRAINT [PK_JOBDETAILS_ID] PRIMARY KEY CLUSTERED ([ID])
);
GO

-- ----------------------------
-- Table: JOBTRIGGERS (作业触发器表)
-- ----------------------------
CREATE TABLE [JOBTRIGGERS] (
    [ID]              INT             IDENTITY(1,1) NOT NULL,
    [TRIGGERID]       NVARCHAR(64)    NOT NULL,
    [JOBID]           NVARCHAR(64)    NOT NULL,
    [TRIGGERTYPE]     NVARCHAR(128)   NULL,
    [ASSEMBLYNAME]    NVARCHAR(128)   NULL,
    [ARGS]            NVARCHAR(128)   NULL,
    [DESCRIPTION]     NVARCHAR(128)   NULL,
    [STATUS]          BIT             NOT NULL,
    [STARTTIME]       DATETIME        NULL,
    [ENDTIME]         DATETIME        NULL,
    [LASTRUNTIME]     DATETIME        NULL,
    [NEXTRUNTIME]     DATETIME        NULL,
    [NUMBEROFRUNS]    INT             NOT NULL,
    [MAXNUMBEROFRUNS] INT             NOT NULL,
    [NUMBEROFERRORS]  INT             NOT NULL,
    [MAXNUMBEROFERRORS] INT           NOT NULL,
    [NUMRETRIES]      INT             NOT NULL,
    [RETRYTIMEOUT]    INT             NOT NULL,
    [STARTNOW]        BIT             NOT NULL,
    [RUNONSTART]      BIT             NOT NULL,
    [RESETONLYONCE]   BIT             NOT NULL,
    [UPDATEDTIME]     DATETIME        NULL,
    CONSTRAINT [PK_JOBTRIGGERS_ID] PRIMARY KEY CLUSTERED ([ID])
);
GO
