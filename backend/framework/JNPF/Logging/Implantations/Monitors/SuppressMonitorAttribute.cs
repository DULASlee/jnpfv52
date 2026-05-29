namespace System;

/// <summary>
/// 控制跳过日志监视
/// </summary>
/// <remarks>作用于全局 <see cref="LoggingMonitorAttribute"/></remarks>
[SuppressSniffer, AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
public sealed class SuppressMonitorAttribute : Attribute
{
}