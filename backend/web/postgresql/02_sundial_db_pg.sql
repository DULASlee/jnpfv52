-- ============================================================
-- jnpf_sundial 调度库 PostgreSQL 初始化脚本
-- 源文件: jnpf_sundial_init_sqlserver.sql (SQL Server 版本)
-- ============================================================

-- Table: JOBCLUSTER (作业集群表)
CREATE TABLE JOBCLUSTER (
    ID            integer GENERATED ALWAYS AS IDENTITY NOT NULL,
    CLUSTERID     varchar(64)  NOT NULL,
    DESCRIPTION   varchar(128) NULL,
    STATUS        boolean      NOT NULL,
    UPDATEDTIME   timestamp    NULL,
    CONSTRAINT PK_JOBCLUSTER_ID PRIMARY KEY (ID)
);

-- Table: JOBDETAILS (作业信息表)
CREATE TABLE JOBDETAILS (
    ID                  integer GENERATED ALWAYS AS IDENTITY NOT NULL,
    JOBID               varchar(64)  NOT NULL,
    GROUPNAME           varchar(128) NULL,
    JOBTYPE             varchar(128) NULL,
    ASSEMBLYNAME        varchar(128) NULL,
    DESCRIPTION         varchar(128) NULL,
    CONCURRENT          boolean      NOT NULL,
    INCLUDEANNOTATIONS  boolean      NOT NULL,
    PROPERTIES          text         NULL,
    UPDATEDTIME         timestamp    NULL,
    CREATETYPE          integer      NOT NULL,
    SCRIPTCODE          text         NULL,
    TENANTID            varchar(50)  NULL,
    CONSTRAINT PK_JOBDETAILS_ID PRIMARY KEY (ID)
);

-- Table: JOBTRIGGERS (作业触发器表)
CREATE TABLE JOBTRIGGERS (
    ID                  integer GENERATED ALWAYS AS IDENTITY NOT NULL,
    TRIGGERID           varchar(64)  NOT NULL,
    JOBID               varchar(64)  NOT NULL,
    TRIGGERTYPE         varchar(128) NULL,
    ASSEMBLYNAME        varchar(128) NULL,
    ARGS                varchar(128) NULL,
    DESCRIPTION         varchar(128) NULL,
    STATUS              boolean      NOT NULL,
    STARTTIME           timestamp    NULL,
    ENDTIME             timestamp    NULL,
    LASTRUNTIME         timestamp    NULL,
    NEXTRUNTIME         timestamp    NULL,
    NUMBEROFRUNS        integer      NOT NULL,
    MAXNUMBEROFRUNS     integer      NOT NULL,
    NUMBEROFERRORS      integer      NOT NULL,
    MAXNUMBEROFERRORS   integer      NOT NULL,
    NUMRETRIES          integer      NOT NULL,
    RETRYTIMEOUT        integer      NOT NULL,
    STARTNOW            boolean      NOT NULL,
    RUNONSTART          boolean      NOT NULL,
    RESETONLYONCE       boolean      NOT NULL,
    UPDATEDTIME         timestamp    NULL,
    CONSTRAINT PK_JOBTRIGGERS_ID PRIMARY KEY (ID)
);
