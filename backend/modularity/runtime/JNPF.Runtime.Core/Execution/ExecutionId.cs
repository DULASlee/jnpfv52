namespace JNPF.Runtime.Core;

/// <summary>
/// Execution 唯一标识。
/// </summary>
public readonly struct ExecutionId : IEquatable<ExecutionId>
{
    /// <summary>
    /// 内部值。
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// 创建新的 ExecutionId。
    /// </summary>
    public ExecutionId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// 创建新的唯一 ExecutionId。
    /// </summary>
    public static ExecutionId New() => new(Guid.NewGuid());

    /// <summary>
    /// 空 ExecutionId。
    /// </summary>
    public static ExecutionId Empty => default;

    /// <summary>
    /// 判断是否为空。
    /// </summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public bool Equals(ExecutionId other) => Value.Equals(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ExecutionId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Value.ToString();

    /// <summary>
    ///相等运算符。
    /// </summary>
    public static bool operator ==(ExecutionId left, ExecutionId right) => left.Equals(right);

    /// <summary>
    ///不等运算符。
    /// </summary>
    public static bool operator !=(ExecutionId left, ExecutionId right) => !left.Equals(right);
}
