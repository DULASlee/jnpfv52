namespace JNPF.InteAssistant.Skills.Cognitive;

/// <summary>
/// ToT 温度梯度规划——纯函数，独立可单测（施工包 21 §3.5）。
/// </summary>
public static class TreeSearchPlanner
{
    public const int MinBranches = 2;
    public const int MaxBranches = 6;

    /// <summary>
    /// 生成 N 路分支温度表：base、base+step、base+2*step…，收敛到 [0, 2]（OpenAI 兼容范围）。
    /// 分支数越界自动夹取到 [2, 6]，保证 ToT 永远是真"多路"探索。
    /// </summary>
    public static double[] BuildTemperatureSchedule(int branchCount, double baseTemperature, double temperatureStep)
    {
        var branches = Math.Clamp(branchCount, MinBranches, MaxBranches);
        var start = Math.Clamp(baseTemperature, 0d, 2d);
        var step = Math.Clamp(temperatureStep, 0.05d, 1d);

        var schedule = new double[branches];
        for (var i = 0; i < branches; i++)
            schedule[i] = Math.Round(Math.Clamp(start + i * step, 0d, 2d), 2);

        return schedule;
    }
}
