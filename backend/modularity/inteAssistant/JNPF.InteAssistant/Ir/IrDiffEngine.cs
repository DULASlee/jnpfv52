using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using SqlSugar;

namespace JNPF.InteAssistant.Ir;

public interface IIrDiffEngine
{
    /// <summary>
    /// 对比两个事件序列点之间的 IR 快照差异（fromSequence &lt; toSequence）。
    /// </summary>
    Task<IrDiffResult> CompareAsync(
        string projectId,
        string tenantId,
        int fromSequence,
        int toSequence,
        IrDiffOptions? options = null,
        CancellationToken ct = default);
}

/// <summary>
/// 阶段五 P5-B01 — 事件溯源片段 diff → added/changed/invalidated。
/// </summary>
public sealed class IrDiffEngine : IIrDiffEngine, ITransient
{
    private readonly ISqlSugarClient _db;

    public IrDiffEngine(ISqlSugarClient db) => _db = db;

    public async Task<IrDiffResult> CompareAsync(
        string projectId,
        string tenantId,
        int fromSequence,
        int toSequence,
        IrDiffOptions? options = null,
        CancellationToken ct = default)
    {
        if (fromSequence < 0 || toSequence < 0)
            throw new ArgumentOutOfRangeException(nameof(fromSequence), "sequence 须 ≥ 0");

        if (fromSequence > toSequence)
            throw new ArgumentException("fromSequence 不得大于 toSequence");

        options ??= new IrDiffOptions();
        var sw = Stopwatch.StartNew();

        var events = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .OrderBy(x => x.Sequence)
            .ToListAsync(ct);

        var fromMap = await ProjectSnapshotsAsync(events, fromSequence, ct);
        var toMap = await ProjectSnapshotsAsync(events, toSequence, ct);

        var added = new List<string>();
        var changed = new List<string>();
        var invalidated = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (fragmentId, toState) in toMap)
        {
            if (!fromMap.TryGetValue(fragmentId, out var fromState))
            {
                added.Add(fragmentId);
                continue;
            }

            if (!StatesEqual(fromState, toState))
            {
                if (IsProtectedLocked(fromState) && !options.ForceUnlock)
                    continue;

                changed.Add(fragmentId);
            }
        }

        foreach (var (fragmentId, fromState) in fromMap)
        {
            if (toMap.ContainsKey(fragmentId))
                continue;

            if (IsProtectedLocked(fromState) && !options.ForceUnlock)
                continue;

            invalidated.Add(fragmentId);
        }

        foreach (var fragmentId in changed)
        {
            if (!toMap.TryGetValue(fragmentId, out var state))
                continue;

            if (state.StabilityState is IrStabilityStates.Invalidated or IrStabilityStates.InProgress)
                invalidated.Add(fragmentId);
        }

        if (options.PropagateDownstream)
        {
            foreach (var fragmentId in changed)
            {
                if (!toMap.TryGetValue(fragmentId, out var source))
                    continue;

                PropagateDownstream(toMap, source.FragmentType, invalidated, options.ForceUnlock);
            }
        }

        foreach (var id in changed)
            invalidated.Remove(id);

        sw.Stop();
        return new IrDiffResult
        {
            ProjectId = projectId,
            TenantId = tenantId,
            FromSequence = fromSequence,
            ToSequence = toSequence,
            Added = added,
            Changed = changed,
            Invalidated = invalidated.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            ElapsedMs = sw.ElapsedMilliseconds,
        };
    }

    private static void PropagateDownstream(
        Dictionary<string, FragmentSnapshotState> snapshotMap,
        string sourceFragmentType,
        HashSet<string> invalidated,
        bool forceUnlock)
    {
        var downstreamTypes = IrFragmentDependencyMap.GetDownstreamFragmentTypes(sourceFragmentType);
        if (downstreamTypes.Count == 0)
            return;

        var typeSet = downstreamTypes.ToHashSet(StringComparer.Ordinal);
        foreach (var (fragmentId, state) in snapshotMap)
        {
            if (!typeSet.Contains(state.FragmentType))
                continue;

            if (IsProtectedLocked(state) && !forceUnlock)
                continue;

            invalidated.Add(fragmentId);
        }
    }

    private static bool IsProtectedLocked(FragmentSnapshotState state)
        => state.StabilityState == IrStabilityStates.Locked;

    private static bool StatesEqual(FragmentSnapshotState a, FragmentSnapshotState b)
        => a.StabilityState == b.StabilityState
           && a.CurrentVersion == b.CurrentVersion
           && string.Equals(a.ContentHash, b.ContentHash, StringComparison.Ordinal);

    private static async Task<Dictionary<string, FragmentSnapshotState>> ProjectSnapshotsAsync(
        IReadOnlyList<AiIrEventEntity> events,
        int maxSequence,
        CancellationToken ct)
    {
        using var db = CreateEphemeralSqlite();
        var projection = new IrProjectionEngine(db);

        foreach (var evt in events.Where(e => e.Sequence <= maxSequence).OrderBy(e => e.Sequence))
            await projection.ProjectEventAsync(evt, ct);

        var rows = await db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => !x.DeleteMark)
            .ToListAsync(ct);

        return rows.ToDictionary(
            r => r.FragmentId,
            r => new FragmentSnapshotState(
                r.FragmentId,
                r.FragmentType,
                r.StabilityState,
                r.CurrentVersion,
                HashContent(r.IrContent, r.StabilityState, r.CurrentVersion)),
            StringComparer.Ordinal);
    }

    private static string HashContent(string content, string stability, int version)
    {
        var raw = $"{stability}|{version}|{content}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static SqlSugarClient CreateEphemeralSqlite()
    {
        var client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = "DataSource=:memory:",
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute,
        });
        client.Open();
        client.Ado.ExecuteCommand("""
            CREATE TABLE ai_ir_events (
                F_Id TEXT PRIMARY KEY,
                F_ProjectId TEXT NOT NULL,
                F_TenantId TEXT NOT NULL,
                F_EventType TEXT NOT NULL,
                F_FragmentType TEXT,
                F_FragmentId TEXT,
                F_FragmentVersion INTEGER NOT NULL DEFAULT 1,
                F_Payload TEXT NOT NULL,
                F_SkillId TEXT,
                F_SAStepName TEXT,
                F_Sequence INTEGER NOT NULL,
                F_CreatedAt TEXT NOT NULL,
                F_IsRollback INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE ai_ir_fragment_snapshots (
                F_Id TEXT PRIMARY KEY,
                F_ProjectId TEXT NOT NULL,
                F_TenantId TEXT NOT NULL,
                F_FragmentId TEXT NOT NULL,
                F_FragmentType TEXT NOT NULL,
                F_CurrentVersion INTEGER NOT NULL,
                F_StabilityState TEXT NOT NULL DEFAULT 'draft',
                F_IrContent TEXT NOT NULL,
                F_SAStepsCompleted TEXT,
                F_LastEventId TEXT NOT NULL,
                F_UpdatedAt TEXT NOT NULL,
                F_DeleteMark INTEGER NOT NULL DEFAULT 0
            );
            """);
        return client;
    }

    private sealed record FragmentSnapshotState(
        string FragmentId,
        string FragmentType,
        string StabilityState,
        int CurrentVersion,
        string ContentHash);
}
