using System.Runtime.CompilerServices;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;

namespace JNPF.InteAssistant.Skills.Cognitive;

/// <summary>
/// 感知结果——PerceiveAsync 产出的结构化输入模型。
/// 派生技能可继承本类扩展自己的感知字段（类型安全放在技能内部，不进运行时契约）。
/// </summary>
public class SkillPerception
{
    public required SkillContext Context { get; init; }
}

/// <summary>
/// 认知技能兵器库——统一注入 LLM 网关、MCP 工具总线、IR 事件流、经验记录器，
/// 让派生技能构造函数保持单参数。
/// </summary>
public interface ICognitiveSkillToolkit
{
    ILlmGatewayService Llm { get; }
    IMcpClient Mcp { get; }
    IEventStream Events { get; }
    IExperienceRecorder Experience { get; }
}

public sealed class CognitiveSkillToolkit : ICognitiveSkillToolkit, ITransient
{
    public CognitiveSkillToolkit(
        ILlmGatewayService llm,
        IMcpClient mcp,
        IEventStream events,
        IExperienceRecorder experience)
    {
        Llm = llm;
        Mcp = mcp;
        Events = events;
        Experience = experience;
    }

    public ILlmGatewayService Llm { get; }
    public IMcpClient Mcp { get; }
    public IEventStream Events { get; }
    public IExperienceRecorder Experience { get; }
}

/// <summary>
/// 认知技能统一模具（施工包 21 §3）。
/// 非泛型基类实现 IBaseSkill——SkillRegistry / SkillHarness 原样收编与调度（红线 RL-3）；
/// 认知生命周期固化为 Perceive（感知）→ Think（思考，流式产出 IR 事件）→ 自动盖 SkillId 戳。
/// 并发闸、配额、日志等运行时职责仍归 SkillHarness，模具不重复承担。
/// </summary>
public abstract class CognitiveSkill : IBaseSkill
{
    private readonly ICognitiveSkillToolkit _toolkit;

    protected CognitiveSkill(ICognitiveSkillToolkit toolkit) => _toolkit = toolkit;

    // ── 骨架：分类学 ──

    public abstract string SkillId { get; }
    public abstract string Version { get; }

    /// <summary>决策层级（Decision / Refinement / Execution）。</summary>
    public abstract SkillLayer Layer { get; }

    /// <summary>使命类型——一个技能只承担一种使命。</summary>
    public abstract SkillMission Mission { get; }

    public abstract SkillInformationNeeds InformationNeeds { get; }
    public abstract SkillOutputDeclaration Outputs { get; }

    // ── 兵器：LLM 网关 + MCP 工具总线；血液：事件流 + 经验回流 ──

    protected ILlmGatewayService Llm => _toolkit.Llm;
    protected IMcpClient Mcp => _toolkit.Mcp;
    protected IEventStream Events => _toolkit.Events;
    protected IExperienceRecorder Experience => _toolkit.Experience;

    // ── 生命周期焊接：IBaseSkill.ReasonAsync = Perceive → Think → 盖戳 ──

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var perception = await PerceiveAsync(context, ct);
        await foreach (var evt in ThinkAsync(perception, ct))
        {
            // 溯源铁律：每个产出事件必须带 SkillId 戳
            yield return string.IsNullOrEmpty(evt.SkillId) ? evt with { SkillId = SkillId } : evt;
        }
    }

    /// <summary>
    /// 感知阶段：把 SkillContext 组织为本技能的结构化输入。
    /// 默认直接包裹上下文，派生技能可重写补充 MCP 检索等前置感知。
    /// </summary>
    protected virtual Task<SkillPerception> PerceiveAsync(SkillContext context, CancellationToken ct)
        => Task.FromResult(new SkillPerception { Context = context });

    /// <summary>
    /// 思考阶段：技能核心推理，流式产出 IR 事件。
    /// 失败必须抛异常或中止流（红线 RL-1：禁止 fallback 假输出）。
    /// </summary>
    protected abstract IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(SkillPerception perception, CancellationToken ct);

    // ── 质检：输入按 InformationNeeds 校验，输出按 Outputs 白名单校验 ──

    public virtual Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        foreach (var fragmentType in InformationNeeds.IrFragmentTypes)
        {
            if (snapshot.Find(fragmentType, InformationNeeds.RequiredStability) == null)
            {
                return Task.FromResult(SkillValidationResult.Fail(
                    $"{SkillId} 缺少前置 IR 片段: {fragmentType}（稳定度 ≥ {InformationNeeds.RequiredStability}）"));
            }
        }

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public virtual Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        var allowed = new HashSet<string>(Outputs.IrEventTypes, StringComparer.Ordinal);
        foreach (var evt in events)
        {
            if (!allowed.Contains(evt.EventType))
            {
                return Task.FromResult(SkillValidationResult.Fail(
                    $"{SkillId} 产出了未声明的事件类型: {evt.EventType}（Outputs 白名单: {string.Join(", ", allowed)}）"));
            }
        }

        return Task.FromResult(SkillValidationResult.Ok());
    }
}
