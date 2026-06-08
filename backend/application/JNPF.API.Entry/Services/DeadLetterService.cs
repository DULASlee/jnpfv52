using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.EventBus.Outbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.API.Entry.Services;

/// <summary>
/// 死信管理 API。
/// 提供死信查询、手动重试、统计等端点。
/// </summary>
[ApiDescriptionSettings(Tag = "EventBus", Name = "DeadLetter")]
[AllowAnonymous]
public class DeadLetterService : IDynamicApiController, ITransient
{
    private readonly SqlSugarEventOutboxStore _store;

    public DeadLetterService(SqlSugarEventOutboxStore store)
    {
        _store = store;
    }

    /// <summary>
    /// 分页查询死信消息。
    /// </summary>
    [HttpGet("/api/eventbus/deadletters")]
    public async Task<IList<EventOutboxMessage>> GetDeadLetters(int pageIndex = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _store.GetDeadLettersAsync(pageIndex, pageSize);
    }

    /// <summary>
    /// 手动重试单条死信。
    /// </summary>
    [HttpPost("/api/eventbus/deadletters/{id}/retry")]
    public async Task RetryDeadLetter(Guid id, CancellationToken cancellationToken = default)
    {
        await _store.RetryDeadLetterAsync(id);
    }

    /// <summary>
    /// 批量重试死信。
    /// </summary>
    [HttpPost("/api/eventbus/deadletters/batch-retry")]
    public async Task BatchRetryDeadLetters([FromBody] List<Guid> ids, CancellationToken cancellationToken = default)
    {
        foreach (var id in ids)
        {
            await _store.RetryDeadLetterAsync(id);
        }
    }

    /// <summary>
    /// Outbox 统计信息。
    /// </summary>
    [HttpGet("/api/eventbus/outbox/stats")]
    public async Task<EventOutboxStats> GetStats(CancellationToken cancellationToken = default)
    {
        return await _store.GetStatsAsync();
    }
}
