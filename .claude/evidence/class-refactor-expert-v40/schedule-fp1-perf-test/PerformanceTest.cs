// F-P1 Performance Test — ScheduleService Delete N+1 Query
// 受控实测：当前实现 vs 批量方案
//
// 使用方法：
//   1. 设置环境变量 JNPF_CONNECTION_SQLSERVER
//   2. dotnet run --project PerformanceTest.csproj
//   3. 查看输出结果

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;

namespace JNPF.Tests.Performance;

//

public class ScheduleTestEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GroupId { get; set; } = Guid.NewGuid().ToString();
    public string CreatorUserId { get; set; } = "test-user";
    public DateTime StartDay { get; set; }
    public DateTime EndDay { get; set; }
    public int DeleteMark { get; set; }
}

public class ScheduleUserTestEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ScheduleId { get; set; } = "";
    public string ToUserId { get; set; } = "";
    public int DeleteMark { get; set; }
}

public class PerformanceTest
{
    public static async Task Main(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("JNPF_CONNECTION_SQLSERVER");
        if (string.IsNullOrEmpty(connStr))
        {
            Console.WriteLine("SKIP: 未设置 JNPF_CONNECTION_SQLSERVER 环境变量");
            Console.WriteLine("无法运行受控性能实测");
            return;
        }

        Console.WriteLine("══════════ F-P1 性能实测 ══════════");
        Console.WriteLine($"测试目标：ScheduleService.Delete N+1 查询");
        Console.WriteLine($"数据库：{connStr.Split(';').FirstOrDefault()}");
        Console.WriteLine();

        // 测试 N = 10, 100
        // 注：1000 可能需要较大数据集，本环境可能不支持
        int[] testSizes = { 10, 100 };

        foreach (var n in testSizes)
        {
            Console.WriteLine($"──────────────── N = {n} ────────────────");

            // 1. 准备测试数据
            var testData = await PrepareTestData(connStr, n);
            Console.WriteLine($"✓ 准备 {n} 条测试数据");

            // 2. 测试当前实现（N+1）
            var nPlusOneTime = await TestCurrentImplementation(connStr, testData);
            Console.WriteLine($"  当前实现 (N+1): {nPlusOneTime.TotalMilliseconds:F2} ms");

            // 3. 测试批量方案
            var batchTime = await TestBatchImplementation(connStr, testData);
            Console.WriteLine($"  批量方案: {batchTime.TotalMilliseconds:F2} ms");

            // 4. 性能对比
            var speedup = nPlusOneTime.TotalMilliseconds / batchTime.TotalMilliseconds;
            var improvement = (1 - batchTime.TotalMilliseconds / nPlusOneTime.TotalMilliseconds) * 100;
            Console.WriteLine($"  加速比: {speedup:F2}x");
            Console.WriteLine($"  性能提升: {improvement:F1}%");

            // 5. 结果集一致性验证
            var isConsistent = await VerifyResultConsistency(connStr, testData);
            Console.WriteLine($"  结果集一致性: {(isConsistent ? "✅ 一致" : "❌ 不一致")}");

            // 6. 清理测试数据
            await CleanupTestData(connStr, testData);
            Console.WriteLine($"✓ 清理测试数据");
            Console.WriteLine();
        }

        Console.WriteLine("══════════ 实测完成 ══════════");
    }

    static async Task<TestData> PrepareTestData(string connStr, int n)
    {
        using var client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connStr,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        // 确保表存在
        client.CodeFirst.InitTables(typeof(ScheduleTestEntity), typeof(ScheduleUserTestEntity));

        var groupId = Guid.NewGuid().ToString();
        var schedules = new List<ScheduleTestEntity>();
        var users = new List<ScheduleUserTestEntity>();

        for (int i = 0; i < n; i++)
        {
            var schedule = new ScheduleTestEntity
            {
                GroupId = groupId,
                StartDay = DateTime.Now.AddDays(i),
                EndDay = DateTime.Now.AddDays(i).AddHours(1)
            };
            schedules.Add(schedule);

            // 每个日程 5 个参与人
            for (int j = 0; j < 5; j++)
            {
                users.Add(new ScheduleUserTestEntity
                {
                    ScheduleId = schedule.Id,
                    ToUserId = $"user-{j}"
                });
            }
        }

        await client.Insertable(schedules).ExecuteCommandAsync();
        await client.Insertable(users).ExecuteCommandAsync();

        return new TestData { GroupId = groupId, Count = n };
    }

    static async Task<TimeSpan> TestCurrentImplementation(string connStr, TestData testData)
    {
        using var client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connStr,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        var sw = Stopwatch.StartNew();

        // 模拟 Delete case 2/3 当前实现
        var dataList = await client.Queryable<ScheduleTestEntity>()
            .Where(it => it.DeleteMark == 0 && it.GroupId == testData.GroupId)
            .ToListAsync();

        var currentResults = new List<List<ScheduleUserTestEntity>>();
        foreach (var item in dataList)
        {
            var dataUser = await client.Queryable<ScheduleUserTestEntity>()
                .Where(it => it.DeleteMark == 0 && it.ScheduleId == item.Id)
                .ToListAsync();
            currentResults.Add(dataUser);
        }

        sw.Stop();
        return sw.Elapsed;
    }

    static async Task<TimeSpan> TestBatchImplementation(string connStr, TestData testData)
    {
        using var client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connStr,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        var sw = Stopwatch.StartNew();

        // 模拟 Delete case 2/3 批量方案
        var dataList = await client.Queryable<ScheduleTestEntity>()
            .Where(it => it.DeleteMark == 0 && it.GroupId == testData.GroupId)
            .ToListAsync();

        // 批量查询所有参与人
        var allUsers = await client.Queryable<ScheduleUserTestEntity>()
            .Where(it => it.DeleteMark == 0 && dataList.Select(s => s.Id).Contains(it.ScheduleId))
            .ToListAsync();

        // 内存分组
        var userGroups = allUsers
            .GroupBy(u => u.ScheduleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var batchResults = new List<List<ScheduleUserTestEntity>>();
        foreach (var item in dataList)
        {
            var dataUser = userGroups.ContainsKey(item.Id) ? userGroups[item.Id] : new List<ScheduleUserTestEntity>();
            batchResults.Add(dataUser);
        }

        sw.Stop();
        return sw.Elapsed;
    }

    static async Task<bool> VerifyResultConsistency(string connStr, TestData testData)
    {
        using var client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connStr,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        var dataList = await client.Queryable<ScheduleTestEntity>()
            .Where(it => it.DeleteMark == 0 && it.GroupId == testData.GroupId)
            .ToListAsync();

        // 当前实现
        var currentResults = new Dictionary<string, int>();
        foreach (var item in dataList)
        {
            var dataUser = await client.Queryable<ScheduleUserTestEntity>()
                .Where(it => it.DeleteMark == 0 && it.ScheduleId == item.Id)
                .ToListAsync();
            currentResults[item.Id] = dataUser.Count;
        }

        // 批量方案
        var allUsers = await client.Queryable<ScheduleUserTestEntity>()
            .Where(it => it.DeleteMark == 0 && dataList.Select(s => s.Id).Contains(it.ScheduleId))
            .ToListAsync();

        var batchResults = new Dictionary<string, int>();
        foreach (var group in allUsers.GroupBy(u => u.ScheduleId))
        {
            batchResults[group.Key] = group.Count();
        }

        // 对比每个日程的参与人数
        foreach (var item in dataList)
        {
            if (!currentResults.ContainsKey(item.Id) || !batchResults.ContainsKey(item.Id))
                return false;
            if (currentResults[item.Id] != batchResults[item.Id])
                return false;
        }

        return true;
    }

    static async Task CleanupTestData(string connStr, TestData testData)
    {
        using var client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connStr,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        await client.Deleteable<ScheduleTestEntity>()
            .Where(it => it.GroupId == testData.GroupId)
            .ExecuteCommandAsync();

        await client.Deleteable<ScheduleUserTestEntity>()
            .Where(it => it.ScheduleId.Contains(testData.GroupId) || dataList.Any(s => s.GroupId == testData.GroupId))
            .ExecuteCommandAsync();
    }
}

public class TestData
{
    public string GroupId { get; set; } = "";
    public int Count { get; set; }
}