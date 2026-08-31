using JNPF.Runtime.Capability.Modes;

namespace JNPF.Runtime.Capability.Registry;

/// <summary>
/// 默认 Mode Registry：硬编码 4 种 Default Mode 的元数据。
///
/// 构造时即固化元数据，运行期不可修改（满足 M12 "Mode 必须可查询" + M18 "Open/Closed"）。
///
/// 不依赖 Runtime / Profile / 外部 IO。
/// </summary>
public sealed class DefaultModeRegistry : IModeRegistry
{
    private static readonly ModeDescriptor AuditDescriptor = new(
        ModeType.Audit,
        AuditMode.DefaultName,
        AuditMode.DefaultDescriptionText,
        AuditMode.DefaultCapabilities,
        AuditMode.DefaultConstraints);

    private static readonly ModeDescriptor VerifyDescriptor = new(
        ModeType.Verify,
        VerifyMode.DefaultName,
        VerifyMode.DefaultDescriptionText,
        VerifyMode.DefaultCapabilities,
        VerifyMode.DefaultConstraints);

    private static readonly ModeDescriptor ExecuteDescriptor = new(
        ModeType.Execute,
        ExecuteMode.DefaultName,
        ExecuteMode.DefaultDescriptionText,
        ExecuteMode.DefaultCapabilities,
        ExecuteMode.DefaultConstraints);

    private static readonly ModeDescriptor AssistDescriptor = new(
        ModeType.Assist,
        AssistMode.DefaultName,
        AssistMode.DefaultDescriptionText,
        AssistMode.DefaultCapabilities,
        AssistMode.DefaultConstraints);

    private readonly IReadOnlyDictionary<ModeType, ModeDescriptor> _descriptors;
    private readonly IReadOnlyCollection<ModeDescriptor> _all;

    public DefaultModeRegistry()
    {
        _descriptors = new Dictionary<ModeType, ModeDescriptor>
        {
            { ModeType.Audit, AuditDescriptor },
            { ModeType.Verify, VerifyDescriptor },
            { ModeType.Execute, ExecuteDescriptor },
            { ModeType.Assist, AssistDescriptor }
        };
        _all = _descriptors.Values.ToList();
    }

    public bool Contains(ModeType modeType) => _descriptors.ContainsKey(modeType);

    public ModeDescriptor GetDescriptor(ModeType modeType)
    {
        if (_descriptors.TryGetValue(modeType, out var descriptor))
        {
            return descriptor;
        }

        throw new KeyNotFoundException($"ModeType '{modeType}' is not registered.");
    }

    public IReadOnlyCollection<ModeDescriptor> All => _all;
}
