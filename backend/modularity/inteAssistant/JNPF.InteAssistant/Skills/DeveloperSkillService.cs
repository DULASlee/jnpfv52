using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Codegen.ModulePackaging;
using JNPF.InteAssistant.Codegen.TemplateContext;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Entitys.Ir.Contracts;
using JNPF.InteAssistant.Ir;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// developer-skill：IR-2 → .vm 渲染 → workspace 落盘 → CodeGenerated draft。
///
/// P9-S2 升级：从单实体 3 模板升级为多实体全栈编译器（10 模板/实体 × N 实体）。
/// 接入 CodegenProfile（对标 ABP CLI 选项）+ ICodegenBackend（后端选择器）。
/// 零 LLM：纯模板渲染，确定性编译。
/// </summary>
public sealed class DeveloperSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly TemplateContextBuilder _contextBuilder;
    private readonly CodegenWorkspaceWriter _workspaceWriter;
    private readonly ISystemDesignLockedCompletenessGate _completenessGate;
    private readonly ICodegenBackendRegistry _backendRegistry;
    private readonly ILogger<DeveloperSkillService> _logger;
    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly string _templateRoot;

    public DeveloperSkillService(
        TemplateContextBuilder contextBuilder,
        CodegenWorkspaceWriter workspaceWriter,
        ISystemDesignLockedCompletenessGate completenessGate,
        ICodegenBackendRegistry backendRegistry,
        ILogger<DeveloperSkillService> logger,
        EntityDesignRepository entityDesignRepo)
    {
        _contextBuilder = contextBuilder;
        _workspaceWriter = workspaceWriter;
        _completenessGate = completenessGate;
        _backendRegistry = backendRegistry;
        _logger = logger;
        _entityDesignRepo = entityDesignRepo;
        _templateRoot = VmTemplateCatalog.ResolveDefaultTemplateRoot();
    }

    public string SkillId => DevelopmentSkillIds.Developer;
    private const string SkillVersion = "2.0.0-p9s2";
    public string Version => SkillVersion;

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[]
        {
            IrFragmentTypes.Architecture,
            IrFragmentTypes.DDL,
            IrFragmentTypes.FormPageIR,
            IrFragmentTypes.SystemDesign,
        },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.CodeGenerated,
            IrEventTypes.DeveloperSkillCompleted,
        },
    };

    public async Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        _ = ct;
        return await _completenessGate.ValidateAsync(snapshot, null, ct);
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // P9-S2：多实体全栈编译
        // 1. 构建 CodegenProfile（从 IR 派生编译选项，对标 ABP CLI）
        var skeletonSnap = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? context.Snapshot.Find(IrFragmentTypes.Skeleton);
        SkeletonPayload? skeletonPayload = null;
        if (skeletonSnap != null)
        {
            try { skeletonPayload = SkeletonPayload.Parse(skeletonSnap.Payload); }
            catch (JsonException ex) { _logger.LogWarning(ex, "SkeletonPayload 解析容错"); }
        }
        var profile = CodegenProfile.FromIr(skeletonPayload, architecture: null);

        // 2. 选择编译后端（单体/微服务/云原生）
        var backend = _backendRegistry.Resolve(profile);
        var templates = backend.SelectTemplates(profile);
        _logger.LogInformation(
            "Developer skill 多实体编译 project={ProjectId} backend={Backend} entities={EntityCount} templates={TemplateCount}",
            context.ProjectId, backend.DisplayName, profile.EntityCount, templates.Count);

        // 3. P9-S5：投影一次计算 → 持久化 + 编译复用
        var projectionOptions = new EntityDesignProjectionOptions
        {
            TenantId = context.TenantId,
            ProjectId = context.ProjectId,
            PipelineId = context.PipelineId.ToString(),
        };
        var projection = EntityDesignProjector.Project(context.Snapshot, projectionOptions);
        await _entityDesignRepo.PersistAsync(projection, ct);

        // 4. 多实体编译循环
        IReadOnlyList<Ir2CodegenContext> codegenContexts;
        try
        {
            codegenContexts = _contextBuilder.BuildAllFromSkillContext(context, projection);
        }
        catch (TemplateContextBuildException ex)
        {
            throw Oops.Bah(ex.Message);
        }

        if (codegenContexts.Count == 0)
            throw Oops.Bah("多实体编译产出 0 个实体上下文（Skeleton 无可用 entityDrafts）");

        var renderer = VmTemplateRenderer.CreateDefault(_templateRoot);
        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(context.TenantId, context.ProjectId);
        var allTemplateVersions = new List<CodegenManifestBuilder.TemplateVersionEntry>();
        var totalFilesWritten = 0;
        var syntaxErrors = new List<string>();

        foreach (var codegenContext in codegenContexts)
        {
            ct.ThrowIfCancellationRequested();

            var rendered = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var templateId in templates)
            {
                var content = renderer.Render(templateId, codegenContext);
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("模板 {Template} 渲染结果为空，跳过 entity={Entity}", templateId, codegenContext.ClassName);
                    continue;
                }

                try
                {
                    CodegenSyntaxValidator.EnsureValidSyntax(content, $"{codegenContext.ClassName}-{templateId}");
                }
                catch (Exception ex)
                {
                    var errMsg = $"entity={codegenContext.ClassName} template={templateId}: {ex.Message}";
                    _logger.LogWarning("语法校验失败: {Error}", errMsg);
                    syntaxErrors.Add(errMsg);
                    continue; // 跳过此模板，不写入 workspace
                }
                rendered[templateId] = content;
            }

            if (rendered.Count > 0)
            {
                _workspaceWriter.WriteGenerated(backendRoot, codegenContext, rendered);
                totalFilesWritten += rendered.Count;

                var entityVersions = CodegenManifestBuilder.BuildTemplateVersions(codegenContext, rendered);
                allTemplateVersions.AddRange(entityVersions);
            }
        }

        if (syntaxErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"代码生成语法校验失败 ({syntaxErrors.Count} errors): {string.Join("; ", syntaxErrors)}");
        }

        _logger.LogInformation(
            "Developer skill 多实体落盘完成 project={ProjectId} entities={EntityCount} files={FileCount} path={Path}",
            context.ProjectId, codegenContexts.Count, totalFilesWritten, backendRoot);

        // P9-S2-⑥：SQL DDL 输出（从 IR2_DDL 提取 rawSql，写入 workspace/sql/init.sql）
        var ddlWritten = await WriteSqlDdlAsync(context, backendRoot);
        if (ddlWritten) totalFilesWritten++;

        // P9-S3a：模块骨架打包（壳文件：csproj/Module/menus.json/frontend 壳）
        var skeletonFiles = await WriteModuleSkeletonAsync(context, skeletonPayload, profile, backendRoot);
        totalFilesWritten += skeletonFiles;

        // 4. 生成 IR 事件（CodeGenerated + DeveloperSkillCompleted）
        var firstContext = codegenContexts[0];
        var fragmentId = $"codegen:{context.ProjectId}";
        // P9-S2：多实体摘要 payload
        var multiEntityPayload = JsonSerializer.Serialize(new
        {
            projectId = context.ProjectId,
            entityCount = codegenContexts.Count,
            fileCount = totalFilesWritten,
            backendKey = backend.BackendKey,
            entityNames = codegenContexts.Select(c => c.ClassName).ToList(),
            artifactRoot = CodegenWorkspacePaths.ToArtifactRootRelative(context.TenantId, context.ProjectId),
        }, JsonOptions);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.CodeGenerated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            FragmentVersion = 1,
            Payload = multiEntityPayload,
            SkillId = SkillId,
        };

        var completedPayload = JsonSerializer.Serialize(new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            projectId = context.ProjectId,
            artifactRoot = CodegenWorkspacePaths.ToArtifactRootRelative(context.TenantId, context.ProjectId),
            entityCount = codegenContexts.Count,
            fileCount = totalFilesWritten,
            backend = backend.BackendKey,
            channel = "stable",
        }, JsonOptions);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.DeveloperSkillCompleted,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            FragmentVersion = 1,
            Payload = completedPayload,
            SkillId = SkillId,
        };

    }

    public Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events,
        CancellationToken ct = default)
    {
        _ = ct;
        if (events.Count != 2)
            return Task.FromResult(SkillValidationResult.Fail("developer-skill 必须产出 CodeGenerated + DeveloperSkillCompleted"));

        if (events[0].EventType != IrEventTypes.CodeGenerated
            || events[1].EventType != IrEventTypes.DeveloperSkillCompleted)
        {
            return Task.FromResult(SkillValidationResult.Fail("事件顺序或类型不正确"));
        }

        if (events[0].FragmentType != IrFragmentTypes.GeneratedCode)
            return Task.FromResult(SkillValidationResult.Fail("CodeGenerated 须绑定 IR3_GeneratedCode"));

        try
        {
            using var doc = JsonDocument.Parse(events[0].Payload);
            var root = doc.RootElement;

            // P9-S2：多实体 payload 有 entityCount/fileCount（非旧 stabilityState/templateVersions）
            if (root.TryGetProperty("entityCount", out var ecEl))
            {
                var entityCount = ecEl.GetInt32();
                if (entityCount <= 0)
                    return Task.FromResult(SkillValidationResult.Fail("CodeGenerated entityCount 须 > 0"));
                // 多实体模式校验通过
                return Task.FromResult(SkillValidationResult.Ok());
            }

            // 旧格式兼容：stabilityState=draft + templateVersions 非空
            if (!root.TryGetProperty("stabilityState", out var st)
                || st.GetString() != IrStabilityStates.Draft)
            {
                return Task.FromResult(SkillValidationResult.Fail("CodeGenerated payload 须 stabilityState=draft"));
            }

            if (!root.TryGetProperty("templateVersions", out var tv) || tv.GetArrayLength() == 0)
                return Task.FromResult(SkillValidationResult.Fail("CodeGenerated 缺少 templateVersions"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(SkillValidationResult.Fail($"CodeGenerated payload JSON 无效: {ex.Message}"));
        }

        return Task.FromResult(SkillValidationResult.Ok());
    }

    /// <summary>
    /// P9-S2-⑥：从 IR2_DDL 提取 rawSql，写入 workspace/sql/init.sql。
    /// 确定性输出（零 LLM），直接复用 DbDesignSkill 已产出的 DDL。
    /// </summary>
    private async Task<bool> WriteSqlDdlAsync(SkillContext context, string backendRoot)
    {
        try
        {
            var ddlSnap = context.Snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)
                ?? context.Snapshot.Find(IrFragmentTypes.DDL);
            if (ddlSnap == null) return false;

            var ddl = DdlPayload.Parse(ddlSnap.Payload);

            // 优先用结构化 tables 生成 SQL；无则用 rawSql
            string sqlContent;
            if (ddl.Tables.Count > 0)
            {
                sqlContent = GenerateSqlFromTables(ddl.Tables);
            }
            else if (!string.IsNullOrEmpty(ddl.RawSql))
            {
                sqlContent = ddl.RawSql;
            }
            else
            {
                return false;
            }

            var sqlDir = Path.Combine(backendRoot, "sql");
            Directory.CreateDirectory(sqlDir);
            var sqlPath = Path.Combine(sqlDir, "init.sql");

            var header = $"-- <auto-generated> project={context.ProjectId} template=ddl-init{Environment.NewLine}";
            await File.WriteAllTextAsync(sqlPath, header + sqlContent);

            _logger.LogInformation("SQL DDL 写入 project={ProjectId} path={Path}", context.ProjectId, sqlPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL DDL 写入失败（非阻断）");
            return false;
        }
    }

    /// <summary>从结构化 TableDefinition[] 生成 CREATE TABLE SQL（确定性，零 LLM）。</summary>
    private static string GenerateSqlFromTables(List<TableDefinition> tables)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var table in tables)
        {
            sb.AppendLine($"CREATE TABLE [{table.TableName}] (");

            var columnDefs = new List<string>();
            foreach (var col in table.Columns)
            {
                var parts = new List<string> { $"[{col.Name}]", col.DataType };
                if (col.IsIdentity) parts.Add("IDENTITY(1,1)");
                if (!col.IsNullable) parts.Add("NOT NULL");
                if (col.IsPrimaryKey) parts.Add("PRIMARY KEY");
                columnDefs.Add("    " + string.Join(" ", parts));
            }

            // 外键
            foreach (var fk in table.ForeignKeys)
            {
                columnDefs.Add($"    FOREIGN KEY ([{fk.ColumnName}]) REFERENCES [{fk.ReferencesTable}] ([{fk.ReferencesColumn}])");
            }

            // 索引
            foreach (var idx in table.Indexes)
            {
                var cols = string.Join(", ", idx.Columns.Select(c => $"[{c}]"));
                columnDefs.Add($"    {(idx.IsUnique ? "UNIQUE " : "")}INDEX [{idx.IndexName}] ({cols})");
            }

            sb.AppendLine(string.Join(",\n", columnDefs));
            sb.AppendLine(");");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// P9-S3a：模块骨架打包 — 调 ModuleSkeletonGenerator 生成壳文件（csproj/Module/menus/frontend 壳），
    /// 写入 workspace 根目录（与实体文件同级）。
    /// </summary>
    private async Task<int> WriteModuleSkeletonAsync(
        SkillContext context,
        SkeletonPayload? skeleton,
        CodegenProfile profile,
        string backendRoot)
    {
        if (skeleton == null)
        {
            _logger.LogWarning("模块骨架打包跳过：无 SkeletonPayload");
            return 0;
        }

        try
        {
            var files = ModuleSkeletonGenerator.Generate(skeleton, profile);
            foreach (var (relativePath, content) in files)
            {
                var fullPath = Path.Combine(backendRoot, relativePath);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(fullPath, content);
            }

            _logger.LogInformation(
                "模块骨架打包完成 project={ProjectId} files={FileCount}",
                context.ProjectId, files.Count);

            return files.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "模块骨架打包失败（非阻断）");
            return 0;
        }
    }
}
