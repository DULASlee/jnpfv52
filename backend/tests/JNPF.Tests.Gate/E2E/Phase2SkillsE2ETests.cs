using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JNPF.InteAssistant.Sa;
using JNPF.Tests.Gate.Auth;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace JNPF.Tests.Gate.E2E;

/// <summary>
/// 阶段二 Skill 链路 E2E（API + 轮询，无需手点浏览器）
///
/// 前置：start-dev 已启动 (:5000)；Phase1+Phase2 DDL 已执行
/// 运行：dotnet test backend/tests/JNPF.Tests.Gate --filter "FullyQualifiedName~Phase2SkillsE2E"
/// </summary>
public class Phase2SkillsE2ETests
{
    private readonly ITestOutputHelper? _output;

    public Phase2SkillsE2ETests(ITestOutputHelper? output = null) => _output = output;

    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("TEST_API_URL") ?? "http://localhost:5000"),
        Timeout = TimeSpan.FromMinutes(10),
    };

    private static async Task<bool> IsServerRunning()
    {
        try
        {
            using var quick = new HttpClient { BaseAddress = Client.BaseAddress, Timeout = TimeSpan.FromSeconds(3) };
            var resp = await quick.GetAsync("/api/health");
            // 403 = 服务在跑但 health 未放行；502/000 = 未启动
            return resp.StatusCode is System.Net.HttpStatusCode.OK
                or System.Net.HttpStatusCode.Forbidden
                or System.Net.HttpStatusCode.Unauthorized;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> GetTokenAsync()
        => await JnpfTestAuthHelper.GetTokenAsync(Client);

    private void Log(string msg) => _output?.WriteLine(msg);

    private static void SetAuth(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<long> CreatePipelineAsync(string name, string requirement)
    {
        var resp = await Client.PostAsJsonAsync("/api/studio/pipeline/execute/create", new { name, userRequirement = requirement });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.TryGetProperty("data", out var d) ? d : body;
        if (data.TryGetProperty("pipelineId", out var pid))
            return pid.GetInt64();
        if (data.TryGetProperty("PipelineId", out var pid2))
            return pid2.GetInt64();
        throw new InvalidOperationException($"create pipeline missing pipelineId: {body}");
    }

    private static async Task SimulateAsync(long pipelineId, string eventType)
    {
        var resp = await Client.PostAsJsonAsync($"/api/studio/ir/{pipelineId}/simulate", new { eventType });
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"simulate {eventType} failed: {(int)resp.StatusCode} {err}");
        }
    }

    private static async Task ConfirmSkeletonAsync(long pipelineId, bool autoRunAnalyst = false)
    {
        var resp = await Client.PostAsJsonAsync(
            $"/api/studio/skills/pm/{pipelineId}/confirm-skeleton",
            new { autoRunAnalyst });
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"confirm-skeleton failed: {(int)resp.StatusCode} {err}");
        }
    }

    private static int GetJnpfCode(HttpResponseMessage resp, string bodyText)
    {
        try
        {
            using var doc = JsonDocument.Parse(bodyText);
            if (doc.RootElement.TryGetProperty("code", out var codeEl) && codeEl.TryGetInt32(out var code))
                return code;
        }
        catch { /* fall through */ }
        return (int)resp.StatusCode;
    }

    private static async Task<HttpResponseMessage> RunSkillRawAsync(long pipelineId, string skill)
    {
        var path = skill == "pm-skill"
            ? $"/api/studio/skills/pm/{pipelineId}/run"
            : $"/api/studio/skills/analyst/{pipelineId}/run";
        var resp = await Client.PostAsJsonAsync(path, new { });
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"run {skill} failed: {(int)resp.StatusCode} {err}");
        }
        return resp;
    }

    private static async Task RunSkillAsync(long pipelineId, string skill)
    {
        await RunSkillRawAsync(pipelineId, skill);
    }

    private static async Task<List<JsonElement>> GetIrEventsAsync(long pipelineId)
    {
        var resp = await Client.GetAsync($"/api/studio/ir/{pipelineId}/events");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        if (body.ValueKind == JsonValueKind.Array)
            return body.EnumerateArray().ToList();

        if (body.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return data.EnumerateArray().ToList();

        return new List<JsonElement>();
    }

    private static async Task<List<JsonElement>> GetSkillRunsAsync(long pipelineId)
    {
        var resp = await Client.GetAsync($"/api/studio/skills/{pipelineId}/runs");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        if (body.ValueKind == JsonValueKind.Array)
            return body.EnumerateArray().ToList();
        if (body.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return data.EnumerateArray().ToList();
        return new List<JsonElement>();
    }

    private static async Task WaitForEventAsync(long pipelineId, string eventType, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var events = await GetIrEventsAsync(pipelineId);
            if (events.Any(e => e.TryGetProperty("eventType", out var t) && t.GetString() == eventType))
                return;
            await Task.Delay(1500);
        }
        throw new TimeoutException($"等待 IR 事件 {eventType} 超时 ({timeout.TotalSeconds}s)");
    }

    private static async Task WaitForSkillRunAsync(long pipelineId, string skillId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var runs = await GetSkillRunsAsync(pipelineId);
            var match = runs.FirstOrDefault(r =>
                r.TryGetProperty("skillId", out var s) && s.GetString() == skillId
                && r.TryGetProperty("status", out var st) && st.GetString() is "completed" or "failed");
            if (match.ValueKind != JsonValueKind.Undefined)
            {
                var status = match.GetProperty("status").GetString();
                if (status == "failed")
                {
                    var err = match.TryGetProperty("errorMessage", out var em) ? em.GetString() : "unknown";
                    throw new InvalidOperationException($"{skillId} run failed: {err}");
                }
                return;
            }
            await Task.Delay(1500);
        }
        throw new TimeoutException($"等待 {skillId} 完成超时 ({timeout.TotalSeconds}s)");
    }

    /// <summary>D1-D7：simulate 骨架 → 确认 → Analyst 全链路</summary>
    [Fact]
    public async Task D1_D7_Simulate骨架_确认_Analyst_至AnalysisCompleted()
    {
        if (!await IsServerRunning())
        {
            Log("SKIP: 后端未启动 (http://localhost:5000)");
            return;
        }

        var token = await GetTokenAsync();
        SetAuth(token);

        var requirement = new string('测', 850) + "请假管理系统：员工提交请假、主管审批、HR归档。";
        var pipelineId = await CreatePipelineAsync("Phase2-E2E-Skills", requirement);
        Log($"pipelineId={pipelineId}");

        await SimulateAsync(pipelineId, "SkeletonCreated");
        await WaitForEventAsync(pipelineId, "SkeletonCreated", TimeSpan.FromSeconds(30));

        await ConfirmSkeletonAsync(pipelineId, autoRunAnalyst: false);
        await WaitForEventAsync(pipelineId, "StageConfirmed", TimeSpan.FromSeconds(30));
        await WaitForEventAsync(pipelineId, "FragmentStabilized", TimeSpan.FromSeconds(30));

        await RunSkillAsync(pipelineId, "analyst-skill");
        await WaitForSkillRunAsync(pipelineId, "analyst-skill", TimeSpan.FromMinutes(5));

        var events = await GetIrEventsAsync(pipelineId);
        var types = events.Select(e => e.GetProperty("eventType").GetString()).ToList();
        Log("IR events: " + string.Join(", ", types.Distinct()));

        Assert.Contains("SkeletonCreated", types);
        Assert.Contains("StageConfirmed", types);
        Assert.Contains("SA_Step_Completed", types);
        Assert.Contains("EventSpecConfirmed", types);
        Assert.Contains("AnalysisCompleted", types);

        var saCount = types.Count(t => t == "SA_Step_Completed");
        Assert.True(saCount >= 9, $"SA 步骤应 ≥9，实际 {saCount}");

        var snapshotsResp = await Client.GetAsync($"/api/studio/ir/{pipelineId}/snapshots");
        snapshotsResp.EnsureSuccessStatusCode();
        var snapshots = await snapshotsResp.Content.ReadFromJsonAsync<JsonElement>();
        var snapList = snapshots.ValueKind == JsonValueKind.Array
            ? snapshots.EnumerateArray().ToList()
            : snapshots.GetProperty("data").EnumerateArray().ToList();

        var eventSpecs = snapList.Where(s =>
            s.TryGetProperty("fragmentType", out var ft) && ft.GetString() == "IR1_EventSpec").ToList();
        Assert.NotEmpty(eventSpecs);
        foreach (var spec in eventSpecs)
        {
            var state = spec.GetProperty("stabilityState").GetString();
            Assert.True(state is "stable" or "locked", $"EventSpec 应 stable，实际 {state}");
        }
    }

    /// <summary>D14：同 pipeline 重复 run → 409</summary>
    [Fact]
    public async Task D14_同Pipeline重复RunSkill_返回409()
    {
        if (!await IsServerRunning()) return;

        var token = await GetTokenAsync();
        SetAuth(token);

        var pipelineId = await CreatePipelineAsync("Phase2-E2E-Mutex", new string('x', 800));
        await SimulateAsync(pipelineId, "SkeletonCreated");
        await ConfirmSkeletonAsync(pipelineId);

        await RunSkillAsync(pipelineId, "analyst-skill");

        var second = await RunSkillRawAsync(pipelineId, "analyst-skill");
        var secondBody = await second.Content.ReadAsStringAsync();
        var secondCode = GetJnpfCode(second, secondBody);
        if (secondCode == 429)
        {
            Log("SKIP D14: 租户 quota 已满（请先重启 backend 或单独运行本用例）");
            return;
        }

        Assert.Equal(409, secondCode);
        Log($"第二次 run 业务码: {secondCode} (期望 409)");
    }

    /// <summary>D11：EventSpecRevised 后仅重跑受影响 SA 步骤</summary>
    [Fact]
    public async Task D11_Revise后重跑受影响步骤_恢复九步完成()
    {
        if (!await IsServerRunning()) return;

        var token = await GetTokenAsync();
        SetAuth(token);

        var pipelineId = await CreatePipelineAsync("Phase2-E2E-Revise", new string('修', 800) + "请假修订测试");
        await SimulateAsync(pipelineId, "SkeletonCreated");
        await ConfirmSkeletonAsync(pipelineId);
        await RunSkillAsync(pipelineId, "analyst-skill");
        await WaitForSkillRunAsync(pipelineId, "analyst-skill", TimeSpan.FromMinutes(8));

        const string fragmentId = "eventspec:BE-001";
        var saBefore = (await GetIrEventsAsync(pipelineId)).Count(e =>
            e.TryGetProperty("eventType", out var t) && t.GetString() == "SA_Step_Completed");

        var reviseResp = await Client.PostAsJsonAsync(
            $"/api/studio/ir/{pipelineId}/events/{fragmentId}/revise",
            new
            {
                revisionType = "fieldTypeOrConstraint",
                payloadPatch = """{"fieldPatch":"duration:int"}""",
                autoRerunAffected = true,
            });
        reviseResp.EnsureSuccessStatusCode();
        var reviseBody = await reviseResp.Content.ReadFromJsonAsync<JsonElement>();
        var affected = reviseBody.GetProperty("data").GetProperty("affectedSteps");
        Assert.Equal(2, affected.GetArrayLength());

        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(5);
        while (DateTime.UtcNow < deadline)
        {
            var events = await GetIrEventsAsync(pipelineId);
            var types = events.Select(e => e.GetProperty("eventType").GetString()).ToList();
            var saCount = types.Count(t => t == "SA_Step_Completed");
            if (saCount > saBefore && types.Count(t => t == "EventSpecConfirmed") >= 2)
                break;
            await Task.Delay(2000);
        }

        var finalEvents = await GetIrEventsAsync(pipelineId);
        var finalTypes = finalEvents.Select(e => e.GetProperty("eventType").GetString()).ToList();
        Assert.Contains("EventSpecRevised", finalTypes);
        Assert.True(finalTypes.Count(t => t == "SA_Step_Completed") > saBefore,
            "修订后应追加受影响步骤的 SA_Step_Completed");
        Assert.True(finalTypes.Count(t => t == "EventSpecConfirmed") >= 2,
            "九步完成后应再次 EventSpecConfirmed");
    }

    /// <summary>G2：同租户第 4 条 pipeline 并行 Analyst 被配额拒绝</summary>
    [Fact]
    public async Task G2_租户Pipeline配额_第4条被拒绝()
    {
        if (!await IsServerRunning()) return;

        var token = await GetTokenAsync();
        SetAuth(token);

        var ids = new List<long>();
        for (var i = 0; i < 4; i++)
        {
            var id = await CreatePipelineAsync($"P25-Quota-{i}", new string('配', 800) + i);
            await SimulateAsync(id, "SkeletonCreated");
            await ConfirmSkeletonAsync(id);
            ids.Add(id);
        }

        var codes = new List<int>();
        foreach (var id in ids)
        {
            var resp = await Client.PostAsJsonAsync($"/api/studio/skills/analyst/{id}/run", new { });
            var body = await resp.Content.ReadAsStringAsync();
            codes.Add(GetJnpfCode(resp, body));
        }

        var blocked = codes.Count(c => c == 429);
        var okCount = codes.Count(c => c == 200);

        Log($"G2 codes=[{string.Join(',', codes)}] ok={okCount} blocked429={blocked}");
        Assert.True(blocked >= 1 && okCount <= 3,
            $"期望第4条 code=429，实际 codes=[{string.Join(',', codes)}]");
    }

    /// <summary>D15：完整性门禁 — 单元级（无 HTTP）</summary>
    [Fact]
    public void D15_完整性门禁_缺EventSpec_应拒绝()
    {
        var gate = new JNPF.InteAssistant.Ir.AnalysisCompletedCompletenessGate(null!, Microsoft.Extensions.Options.Options.Create(new SaPipelineOptions()));
        var snapshot = new JNPF.InteAssistant.Skills.IrSnapshot
        {
            Fragments = new List<JNPF.InteAssistant.Skills.IrSnapshotFragment>
            {
                new()
                {
                    FragmentId = "skeleton:SK-001",
                    FragmentType = JNPF.InteAssistant.Entitys.Ir.IrFragmentTypes.Skeleton,
                    StabilityState = JNPF.InteAssistant.Entitys.Ir.IrStabilityStates.Stable,
                    Payload = """{"businessEvents":[{"eventId":"BE-001","eventName":"Test"}]}""",
                },
            },
        };

        var result = gate.ValidateAsync("t1", "100", snapshot, excludeRunId: "current-run").GetAwaiter().GetResult();
        Assert.False(result.IsValid);
        Assert.Contains("EventSpec", result.ErrorMessage ?? "");
    }
}
