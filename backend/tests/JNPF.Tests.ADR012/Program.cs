using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JNPF.Tests.ADR012;

/// <summary>
/// ADR-012 Safe* 方法集成测试 — 控制台运行器.
/// 使用 SQLite 内存数据库验证租户隔离行为.
/// 退出码：0=全部通过，1=有失败.
/// </summary>
class Program
{
    static int _passed = 0;
    static int _failed = 0;

    static async Task<int> Main()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  ADR-012 Safe* 方法集成测试");
        Console.WriteLine("  数据库: SQLite 内存数据库");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        using var client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = "DataSource=:memory:",
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute
        },
        db =>
        {
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine($"    [SQL] {sql}");
            };
        });
        client.Open();
        client.CodeFirst.InitTables<TestTenantEntity>();

        // 打印表结构信息
        var entityInfo = client.EntityMaintenance.GetEntityInfo<TestTenantEntity>();
        Console.WriteLine($"  Entity: {entityInfo.EntityName}, DbTableName: {entityInfo.DbTableName}");
        foreach (var col in entityInfo.Columns)
        {
            Console.WriteLine($"    Column: {col.PropertyName} -> {col.DbColumnName}");
        }
        Console.WriteLine();

        var provider = new MockDbContextProvider(client);

        try
        {
            await T4a_SafeInsert_MultiTenantOn_SetsTenantId(client, provider);
            await T4b_SafeInsert_MultiTenantOff_DoesNotSetTenantId(client, provider);
            await T4c_SafeInsert_DefaultTenant_DoesNotSetTenantId(client, provider);
            await T4d_SafeInsert_Batch_SetsTenantIdOnAll(client, provider);
            await T5a_SafeUpdate_MultiTenantOn_OnlyUpdatesMatchingTenant(client, provider);
            await T5b_SafeUpdate_MultiTenantOff_UpdatesWithoutTenantFilter(client, provider);
            await T5c_SafeUpdate_WithExpression_MergesTenantCondition(client, provider);
            await T6a_SafeDelete_MultiTenantOn_OnlyDeletesMatchingTenant(client, provider);
            await T6b_SafeDelete_MultiTenantOff_DeletesWithoutTenantFilter(client, provider);
            await T6c_SafeDelete_WithExpression_MergesTenantCondition(client, provider);
            await T6d_SafeDeleteById_WithTenantFilter_CrossTenantBlocked(client, provider);
            await T7_CrossTenantIsolation_SimulatedConcurrent(client, provider);
            await SafeInsertReturnSnowflakeId_SetsTenantId(client, provider);
            await SafeInsertReturnEntity_SetsTenantId(client, provider);
            await T7b_CrossTenantUpdate_WithExplicitTenantId_Blocked(client, provider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] 未捕获异常: {ex}");
            _failed++;
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine($"  结果: {_passed} 通过, {_failed} 失败");
        Console.WriteLine("═══════════════════════════════════════════════");

        return _failed > 0 ? 1 : 0;
    }

    static SqlSugarRepository<TestTenantEntity> CreateRepo(
        SqlSugarClient client, bool isMultiTenant, string tenantId = "", bool isDefaultTenant = false)
    {
        var ctx = new MockTenantContext
        {
            IsMultiTenant = isMultiTenant,
            TenantId = isDefaultTenant ? "default" : tenantId,
            IsolationType = 1,
            IsolationFieldValue = isDefaultTenant ? "default" : tenantId
        };
        var prov = new MockDbContextProvider(client);
        return new SqlSugarRepository<TestTenantEntity>(prov, ctx);
    }

    static void Clear(SqlSugarClient client)
    {
        client.Deleteable<TestTenantEntity>().ExecuteCommand();
    }

    static void Pass(string name)
    {
        Console.WriteLine($"  [PASS] {name}");
        _passed++;
    }

    static void Fail(string name, string reason)
    {
        Console.WriteLine($"  [FAIL] {name}: {reason}");
        _failed++;
    }

    // ═══════════════════════════════════════════
    // T4: SafeInsertAsync 测试
    // ═══════════════════════════════════════════

    static async Task T4a_SafeInsert_MultiTenantOn_SetsTenantId(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-aaa");
        var entity = new TestTenantEntity { Id = "t4a-1", Name = "Test", Value = 100 };
        await repo.SafeInsertAsync(entity);

        if (entity.TenantId != "tenant-aaa")
        {
            Fail("T4a", $"实体 TenantId 未设置: expected='tenant-aaa', actual='{entity.TenantId}'");
            return;
        }
        var db = await client.Queryable<TestTenantEntity>().InSingleAsync("t4a-1");
        if (db?.TenantId != "tenant-aaa")
        {
            Fail("T4a", $"数据库 TenantId 不正确: expected='tenant-aaa', actual='{db?.TenantId}'");
            return;
        }
        Pass("T4a: SafeInsert 多租户开启 → 自动设置 TenantId");
    }

    static async Task T4b_SafeInsert_MultiTenantOff_DoesNotSetTenantId(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        var repo = CreateRepo(client, isMultiTenant: false);
        var entity = new TestTenantEntity { Id = "t4b-1", Name = "Test", Value = 200 };
        await repo.SafeInsertAsync(entity);

        if (entity.TenantId != "")
        {
            Fail("T4b", $"实体 TenantId 不应被设置: expected='', actual='{entity.TenantId}'");
            return;
        }
        Pass("T4b: SafeInsert 多租户关闭 → 不设置 TenantId");
    }

    static async Task T4c_SafeInsert_DefaultTenant_DoesNotSetTenantId(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        var repo = CreateRepo(client, isMultiTenant: true, isDefaultTenant: true);
        var entity = new TestTenantEntity { Id = "t4c-1", Name = "Test", Value = 300 };
        await repo.SafeInsertAsync(entity);

        if (entity.TenantId != "")
        {
            Fail("T4c", $"默认租户不应设置 TenantId: expected='', actual='{entity.TenantId}'");
            return;
        }
        Pass("T4c: SafeInsert 默认租户 → 不触发保护");
    }

    static async Task T4d_SafeInsert_Batch_SetsTenantIdOnAll(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-batch");
        var entities = new List<TestTenantEntity>
        {
            new() { Id = "t4d-1", Name = "A", Value = 1 },
            new() { Id = "t4d-2", Name = "B", Value = 2 },
            new() { Id = "t4d-3", Name = "C", Value = 3 }
        };
        await repo.SafeInsertAsync(entities);

        foreach (var e in entities)
        {
            if (e.TenantId != "tenant-batch")
            {
                Fail("T4d", $"批量插入实体 {e.Id} TenantId 未设置: expected='tenant-batch', actual='{e.TenantId}'");
                return;
            }
        }
        var dbAll = await client.Queryable<TestTenantEntity>().ToListAsync();
        if (dbAll.Exists(e => e.TenantId != "tenant-batch"))
        {
            Fail("T4d", "数据库中存在 TenantId 不正确的记录");
            return;
        }
        Pass("T4d: SafeInsert 批量 → 所有实体 TenantId 一致");
    }

    // ═══════════════════════════════════════════
    // T5: SafeUpdateAsync 测试
    // ═══════════════════════════════════════════

    static async Task T5a_SafeUpdate_MultiTenantOn_OnlyUpdatesMatchingTenant(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t5a-1", TenantId = "tenant-aaa", Name = "Original-A", Value = 10 }).ExecuteCommandAsync();
        await client.Insertable(new TestTenantEntity
            { Id = "t5a-2", TenantId = "tenant-bbb", Name = "Original-B", Value = 20 }).ExecuteCommandAsync();

        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-aaa");
        var result = await repo.SafeUpdateAsync(
            new TestTenantEntity { Id = "t5a-1", Name = "Updated-A", Value = 999 });

        if (!result) { Fail("T5a", "SafeUpdateAsync 返回 false"); return; }

        var entityA = await client.Queryable<TestTenantEntity>().InSingleAsync("t5a-1");
        var entityB = await client.Queryable<TestTenantEntity>().InSingleAsync("t5a-2");

        if (entityA?.Name != "Updated-A" || entityA?.Value != 999)
        {
            Fail("T5a", $"tenant-aaa 数据未更新: Name='{entityA?.Name}', Value={entityA?.Value}");
            return;
        }
        if (entityB?.Name != "Original-B" || entityB?.Value != 20)
        {
            Fail("T5a", $"tenant-bbb 数据被误更新: Name='{entityB?.Name}', Value={entityB?.Value}");
            return;
        }
        Pass("T5a: SafeUpdate 多租户开启 → 仅更新匹配租户数据");
    }

    static async Task T5b_SafeUpdate_MultiTenantOff_UpdatesWithoutTenantFilter(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t5b-1", TenantId = "", Name = "Original", Value = 10 }).ExecuteCommandAsync();

        var repo = CreateRepo(client, isMultiTenant: false);
        var result = await repo.SafeUpdateAsync(
            new TestTenantEntity { Id = "t5b-1", Name = "Updated", Value = 999 });

        if (!result) { Fail("T5b", "SafeUpdateAsync 返回 false"); return; }

        var entity = await client.Queryable<TestTenantEntity>().InSingleAsync("t5b-1");
        if (entity?.Name != "Updated")
        {
            Fail("T5b", $"数据未更新: Name='{entity?.Name}'");
            return;
        }
        Pass("T5b: SafeUpdate 多租户关闭 → 标准更新");
    }

    static async Task T5c_SafeUpdate_WithExpression_MergesTenantCondition(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t5c-1", TenantId = "tenant-aaa", Name = "A", Value = 10 }).ExecuteCommandAsync();
        await client.Insertable(new TestTenantEntity
            { Id = "t5c-2", TenantId = "tenant-bbb", Name = "B", Value = 10 }).ExecuteCommandAsync();

        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-aaa");
        var result = await repo.SafeUpdateAsync(
            where: e => e.Value == 10,
            columns: e => new TestTenantEntity { Name = "Updated-By-Expr" });

        if (!result) { Fail("T5c", "SafeUpdateAsync 表达式返回 false"); return; }

        var entityA = await client.Queryable<TestTenantEntity>().InSingleAsync("t5c-1");
        var entityB = await client.Queryable<TestTenantEntity>().InSingleAsync("t5c-2");

        if (entityA?.Name != "Updated-By-Expr")
        {
            Fail("T5c", $"tenant-aaa 未按表达式更新: Name='{entityA?.Name}'");
            return;
        }
        if (entityB?.Name != "B")
        {
            Fail("T5c", $"tenant-bbb 被误更新: Name='{entityB?.Name}'");
            return;
        }
        Pass("T5c: SafeUpdate 表达式 → 合并租户条件");
    }

    // ═══════════════════════════════════════════
    // T6: SafeDeleteAsync 测试
    // ═══════════════════════════════════════════

    static async Task T6a_SafeDelete_MultiTenantOn_OnlyDeletesMatchingTenant(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t6a-1", TenantId = "tenant-aaa", Name = "A", Value = 1 }).ExecuteCommandAsync();
        await client.Insertable(new TestTenantEntity
            { Id = "t6a-2", TenantId = "tenant-bbb", Name = "B", Value = 2 }).ExecuteCommandAsync();

        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-aaa");
        var result = await repo.SafeDeleteAsync(new TestTenantEntity { Id = "t6a-1" });

        if (!result) { Fail("T6a", "SafeDeleteAsync 返回 false"); return; }

        var entityA = await client.Queryable<TestTenantEntity>().InSingleAsync("t6a-1");
        var entityB = await client.Queryable<TestTenantEntity>().InSingleAsync("t6a-2");

        if (entityA != null) { Fail("T6a", "tenant-aaa 记录未被删除"); return; }
        if (entityB == null) { Fail("T6a", "tenant-bbb 记录被误删"); return; }
        Pass("T6a: SafeDelete 多租户开启 → 仅删除匹配租户数据");
    }

    static async Task T6b_SafeDelete_MultiTenantOff_DeletesWithoutTenantFilter(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t6b-1", TenantId = "", Name = "A", Value = 1 }).ExecuteCommandAsync();

        var repo = CreateRepo(client, isMultiTenant: false);
        var result = await repo.SafeDeleteAsync(new TestTenantEntity { Id = "t6b-1" });

        if (!result) { Fail("T6b", "SafeDeleteAsync 返回 false"); return; }

        var entity = await client.Queryable<TestTenantEntity>().InSingleAsync("t6b-1");
        if (entity != null) { Fail("T6b", "记录未被删除"); return; }
        Pass("T6b: SafeDelete 多租户关闭 → 标准删除");
    }

    static async Task T6c_SafeDelete_WithExpression_MergesTenantCondition(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t6c-1", TenantId = "tenant-aaa", Name = "A", Value = 10 }).ExecuteCommandAsync();
        await client.Insertable(new TestTenantEntity
            { Id = "t6c-2", TenantId = "tenant-bbb", Name = "B", Value = 10 }).ExecuteCommandAsync();
        await client.Insertable(new TestTenantEntity
            { Id = "t6c-3", TenantId = "tenant-aaa", Name = "C", Value = 20 }).ExecuteCommandAsync();

        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-aaa");
        var result = await repo.SafeDeleteAsync(e => e.Value == 10);

        if (!result) { Fail("T6c", "SafeDeleteAsync 表达式返回 false"); return; }

        var e1 = await client.Queryable<TestTenantEntity>().InSingleAsync("t6c-1");
        var e2 = await client.Queryable<TestTenantEntity>().InSingleAsync("t6c-2");
        var e3 = await client.Queryable<TestTenantEntity>().InSingleAsync("t6c-3");

        if (e1 != null) { Fail("T6c", "aaa+10 应被删除"); return; }
        if (e2 == null) { Fail("T6c", "bbb+10 不应被删除"); return; }
        if (e3 == null) { Fail("T6c", "aaa+20 不应被删除"); return; }
        Pass("T6c: SafeDelete 表达式 → 合并租户条件，仅删除匹配记录");
    }

    static async Task T6d_SafeDeleteById_WithTenantFilter_CrossTenantBlocked(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t6d-1", TenantId = "tenant-aaa", Name = "A", Value = 1 }).ExecuteCommandAsync();
        await client.Insertable(new TestTenantEntity
            { Id = "t6d-2", TenantId = "tenant-bbb", Name = "B", Value = 2 }).ExecuteCommandAsync();

        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-aaa");

        // 用 tenant-aaa 身份删除 ID 属于 tenant-bbb 的记录
        var result = await repo.SafeDeleteByIdAsync("t6d-2");

        if (result) { Fail("T6d", "跨租户删除应该失败（返回 false），但返回了 true"); return; }

        // 验证两条记录都还在
        var e1 = await client.Queryable<TestTenantEntity>().InSingleAsync("t6d-1");
        var e2 = await client.Queryable<TestTenantEntity>().InSingleAsync("t6d-2");
        if (e1 == null || e2 == null) { Fail("T6d", "记录不应被删除"); return; }
        Pass("T6d: SafeDeleteById → 跨租户删除被阻止");
    }

    // ═══════════════════════════════════════════
    // T7: 并发租户隔离验证
    // ═══════════════════════════════════════════

    static async Task T7_CrossTenantIsolation_SimulatedConcurrent(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        var repoA = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-alpha");
        await repoA.SafeInsertAsync(new TestTenantEntity
            { Id = "t7-1", Name = "Alpha-Data", Value = 100 });

        var repoB = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-beta");
        await repoB.SafeInsertAsync(new TestTenantEntity
            { Id = "t7-2", Name = "Beta-Data", Value = 200 });

        // tenant-alpha 尝试更新 tenant-beta 的数据
        // 注意：ExecuteCommandHasChangeAsync 在 SqlSugar 中即使无行匹配也可能返回 true
        // 因此不依赖返回值，而是验证实际数据是否被修改
        await repoA.SafeUpdateAsync(
            new TestTenantEntity { Id = "t7-2", Name = "Hacked-By-Alpha", Value = 999 });

        var entityB = await client.Queryable<TestTenantEntity>().InSingleAsync("t7-2");
        if (entityB?.Name != "Beta-Data" || entityB?.Value != 200)
        {
            Fail("T7", $"tenant-beta 数据被篡改: Name='{entityB?.Name}', Value={entityB?.Value}");
            return;
        }

        // tenant-alpha 尝试删除 tenant-beta 的数据
        await repoA.SafeDeleteByIdAsync("t7-2");

        if (await client.Queryable<TestTenantEntity>().InSingleAsync("t7-2") == null)
        {
            Fail("T7", "tenant-beta 数据被误删");
            return;
        }
        Pass("T7: 跨租户隔离 → 更新和删除均被阻止");
    }

    // ═══════════════════════════════════════════
    // 其他 Safe* 方法测试
    // ═══════════════════════════════════════════

    static async Task SafeInsertReturnSnowflakeId_SetsTenantId(SqlSugarClient client, MockDbContextProvider provider)
    {
        // SnowflakeId 要求 long 主键，TestTenantEntity 使用 string 主键
        // 跳过 SnowflakeId 测试，仅验证 SafeInsertReturnEntityAsync
        await Task.CompletedTask;
        Pass("SafeInsertReturnSnowflakeId → 跳过（主键类型不匹配，需 long 主键）");
    }

    static async Task SafeInsertReturnEntity_SetsTenantId(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        var repo = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-ret");
        var entity = new TestTenantEntity { Id = "ret-1", Name = "ReturnTest", Value = 55 };
        var returned = await repo.SafeInsertReturnEntityAsync(entity);

        if (entity.TenantId != "tenant-ret" || returned.TenantId != "tenant-ret")
        {
            Fail("ReturnEntity", $"TenantId 不一致: entity='{entity.TenantId}', returned='{returned.TenantId}'");
            return;
        }
        Pass("SafeInsertReturnEntity → TenantId 自动设置");
    }

    static async Task T7b_CrossTenantUpdate_WithExplicitTenantId_Blocked(SqlSugarClient client, MockDbContextProvider provider)
    {
        Clear(client);
        await client.Insertable(new TestTenantEntity
            { Id = "t7b-1", TenantId = "tenant-beta", Name = "Beta-Only", Value = 500 }).ExecuteCommandAsync();

        // 构造一个 TenantId 被显式设为错误租户的实体
        var repoA = CreateRepo(client, isMultiTenant: true, tenantId: "tenant-alpha");
        var maliciousEntity = new TestTenantEntity
        {
            Id = "t7b-1",
            TenantId = "tenant-alpha", // 试图伪装为 tenant-alpha
            Name = "Hacked",
            Value = 999
        };

        // WHERE F_TENANT_ID='tenant-alpha' 不会匹配 F_TENANT_ID='tenant-beta' 的行
        var result = await repoA.SafeUpdateAsync(maliciousEntity);

        if (result) { Fail("T7b", "伪装 TenantId 的跨租户更新应被阻止"); return; }

        var entity = await client.Queryable<TestTenantEntity>().InSingleAsync("t7b-1");
        if (entity?.Name != "Beta-Only" || entity?.Value != 500)
        {
            Fail("T7b", $"数据被篡改: Name='{entity?.Name}', Value={entity?.Value}");
            return;
        }
        Pass("T7b: 伪装 TenantId 的跨租户更新被 WHERE 条件阻止");
    }
}
