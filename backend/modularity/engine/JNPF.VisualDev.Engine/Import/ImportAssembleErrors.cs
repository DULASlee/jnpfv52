namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Shared error aggregation for ImportDataAssemble (VisualDev + CodeGen).
/// </summary>
public static class ImportAssembleErrors
{
    public const string ErrorKey = "errorsInfo";

    public static void Append(Dictionary<string, object> row, string message)
    {
        if (row == null || string.IsNullOrEmpty(message))
            return;
        if (row.ContainsKey(ErrorKey))
            row[ErrorKey] = row[ErrorKey] + "," + message;
        else
            row.Add(ErrorKey, message);
    }

    public static void AppendMismatch(Dictionary<string, object> row, string label)
        => Append(row, label + ": 值无法匹配");
}
