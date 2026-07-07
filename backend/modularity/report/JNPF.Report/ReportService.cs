using JNPF.Common.Filter;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extensions;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.FriendlyException;
using JNPF.Report.Entitys;
using JNPF.Report.Entitys.Dto;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Report;

/// <summary>
/// 报表管理服务 — 替代独立 Java ReportServer，集成到主 .NET 后端.
/// </summary>
[ApiDescriptionSettings(Tag = "Report", Name = "Data", Order = 200)]
[Route("ReportServer/[controller]")]
public class ReportService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<ReportEntity> _repository;

    public ReportService(ISqlSugarRepository<ReportEntity> repository)
    {
        _repository = repository;
    }

    // ═══════════════════════════════════════════════════════════════
    // 报表列表查询（分页 + 搜索 + 分类过滤）
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] ReportListQueryInput input)
    {
        // ── 顺序预加载引用数据 ──
        // 注意：SqlSugar ISqlSugarClient 非线程安全，禁止 Task.WhenAll 并发查询同一 client。
        var refClient = _repository.AsSugarClient();
        var dict = await refClient.Queryable<DictionaryDataEntity>().Where(d => d.DeleteMark == null).ToListAsync();
        var users = await refClient.Queryable<UserEntity>().Where(u => u.DeleteMark == null).ToListAsync();

        // 主查询：分页 + 过滤
        // 注意：ReportEntity 继承 IZxSystemFilter 但 BASE_REPORT 表无 F_ZX_SYSTEM_ID 列，必须 ClearFilter。
        var data = await _repository.AsQueryable()
            .ClearFilter<IZxSystemFilter>()
            .WhereIF(!string.IsNullOrEmpty(input.keyword),
                r => r.FullName.Contains(input.keyword) || r.EnCode.Contains(input.keyword))
            .WhereIF(!string.IsNullOrEmpty(input.category), r => r.Category == input.category)
            .WhereIF(input.enabledMark.HasValue, r => r.EnabledMark == input.enabledMark.Value)
            .Where(r => r.DeleteMark == null)
            .OrderBy(r => r.SortCode, OrderByType.Asc)
            .OrderBy(r => r.CreatorTime, OrderByType.Desc)
            .Select(r => new ReportListOutput
            {
                id = r.Id,
                fullName = r.FullName,
                enCode = r.EnCode,
                categoryId = r.Category,
                description = r.Description,
                enabledMark = r.EnabledMark,
                sortCode = r.SortCode,
                creatorTime = r.CreatorTime,
                reportFile = r.ReportFile,
                creatorUser = r.CreatorUserId,
                lastModifyTime = r.LastModifyTime,
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        // 内存拼接引用数据
        var dictMap = dict.ToDictionary(d => d.Id, d => d.FullName);
        var userMap = users.ToDictionary(u => u.Id, u => $"{u.RealName}/{u.Account}");

        foreach (var item in data.list)
        {
            item.category = dictMap.TryGetValue(item.categoryId ?? string.Empty, out var cat) ? cat : string.Empty;
            item.creatorUser = userMap.TryGetValue(item.creatorUser ?? string.Empty, out var cu) ? cu : string.Empty;
        }

        return PageResult<ReportListOutput>.SqlSugarPageResult(data);
    }

    // ═══════════════════════════════════════════════════════════════
    // 报表 CRUD
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var entity = await _repository.AsQueryable()
            .FirstAsync(r => r.Id == id && r.DeleteMark == null);
        return entity.Adapt<ReportListOutput>();
    }

    [HttpPost("")]
    public async Task Create([FromBody] ReportCrInput input)
    {
        if (await _repository.AsQueryable()
            .AnyAsync(r => r.EnCode == input.enCode && r.DeleteMark == null))
            throw Oops.Bah("报表编码已存在");

        var entity = input.Adapt<ReportEntity>();
        entity.EnabledMark = input.enabledMark ?? 1;

        // 保存报表 XML 到文件系统（兼容 UReport2）
        if (!string.IsNullOrEmpty(input.content))
        {
            var filePath = GetReportFilePath(entity.Id);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, input.content);
            entity.ReportFile = $"{entity.Id}.ureport.xml";
        }

        await _repository.InsertAsync(entity);
    }

    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ReportCrInput input)
    {
        var entity = await _repository.AsQueryable()
            .FirstAsync(r => r.Id == id && r.DeleteMark == null)
            ?? throw Oops.Bah("报表不存在");

        input.Adapt(entity);

        if (!string.IsNullOrEmpty(input.content))
        {
            var filePath = GetReportFilePath(id);
            await File.WriteAllTextAsync(filePath, input.content);
            entity.ReportFile = $"{id}.ureport.xml";
        }

        await _repository.UpdateAsync(entity);
    }

    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.AsQueryable()
            .FirstAsync(r => r.Id == id && r.DeleteMark == null)
            ?? throw Oops.Bah("报表不存在");

        // 软删除
        await _repository.AsSugarClient()
            .Updateable<ReportEntity>()
            .SetColumns(r => r.DeleteMark == 1)
            .Where(r => r.Id == id)
            .ExecuteCommandAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // 复制 / 启用禁用 / 下拉 / 导入
    // ═══════════════════════════════════════════════════════════════

    [HttpPost("{id}/Actions/Copy")]
    public async Task Copy(string id)
    {
        var entity = await _repository.AsQueryable()
            .FirstAsync(r => r.Id == id && r.DeleteMark == null)
            ?? throw Oops.Bah("报表不存在");

        var copy = entity.Adapt<ReportEntity>();
        copy.Id = Yitter.IdGenerator.YitIdHelper.NextId().ToString();
        copy.FullName = $"{entity.FullName}(副本)";
        copy.EnCode = $"{entity.EnCode}_copy";
        copy.CreatorTime = DateTime.Now;
        copy.LastModifyTime = DateTime.Now;

        await _repository.InsertAsync(copy);
    }

    [HttpPut("{id}/Actions/State")]
    public async Task ToggleState(string id)
    {
        var entity = await _repository.AsQueryable()
            .FirstAsync(r => r.Id == id && r.DeleteMark == null)
            ?? throw Oops.Bah("报表不存在");

        entity.EnabledMark = entity.EnabledMark == 1 ? 0 : 1;
        await _repository.UpdateAsync(entity);
    }

    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        var list = await _repository.AsQueryable()
            .Where(r => r.DeleteMark == null && r.EnabledMark == 1)
            .OrderBy(r => r.SortCode, OrderByType.Asc)
            .Select(r => new { r.Id, r.FullName, r.EnCode })
            .ToListAsync();
        return new { list };
    }

    [HttpPost("Actions/Import")]
    public async Task Import([FromBody] ReportCrInput input)
    {
        if (string.IsNullOrEmpty(input.fullName) || string.IsNullOrEmpty(input.content))
            throw Oops.Bah("报表名称和内容不能为空");

        var entity = input.Adapt<ReportEntity>();
        entity.EnabledMark = 1;

        var filePath = GetReportFilePath(entity.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, input.content);
        entity.ReportFile = $"{entity.Id}.ureport.xml";

        await _repository.InsertAsync(entity);
    }

    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> Export(string id)
    {
        var entity = await _repository.AsQueryable()
            .FirstAsync(r => r.Id == id && r.DeleteMark == null)
            ?? throw Oops.Bah("报表不存在");

        var filePath = GetReportFilePath(id);
        if (!File.Exists(filePath))
            throw Oops.Bah("报表文件不存在");

        var content = await File.ReadAllTextAsync(filePath);
        return new { entity.FullName, entity.EnCode, content, exportType = "report" };
    }

    // ═══════════════════════════════════════════════════════════════
    // 文件存储路径（兼容 UReport2 FileReportProvider）
    // ═══════════════════════════════════════════════════════════════

    private static string GetReportFilePath(string id)
    {
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportFiles");
        return Path.Combine(baseDir, $"{id}.ureport.xml");
    }
}
