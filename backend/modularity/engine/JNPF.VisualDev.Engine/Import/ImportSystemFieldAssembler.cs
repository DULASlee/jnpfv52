using JNPF.Common.Const;
using JNPF.Common.Extension;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// System auto-generated control values during import (单据/创建人/时间/组织/岗位).
/// I/O (bill number, position lookup) stays at call site; this only applies resolved values.
/// </summary>
public readonly record struct ImportSystemFieldContext(
    string CreateUserId,
    string? OrganizeId,
    DateTime Now);

public static class ImportSystemFieldAssembler
{
    public const string MissingBillRuleMessage = "单据规则不存在";

    public static bool IsSystemAutoKey(string? jnpfKey)
        => jnpfKey is JnpfKeyConst.BILLRULE
            or JnpfKeyConst.MODIFYUSER
            or JnpfKeyConst.CREATEUSER
            or JnpfKeyConst.MODIFYTIME
            or JnpfKeyConst.CREATETIME
            or JnpfKeyConst.CURRPOSITION
            or JnpfKeyConst.CURRORGANIZE;

    /// <summary>
    /// Sync system fields (no I/O). Returns false for BILLRULE / CURRPOSITION (caller must resolve then Map*).
    /// </summary>
    public static bool TryMapStatic(
        string jnpfKey,
        string fieldKey,
        Dictionary<string, object> newDataItems,
        ImportSystemFieldContext ctx)
    {
        switch (jnpfKey)
        {
            case JnpfKeyConst.MODIFYUSER:
                newDataItems[fieldKey] = string.Empty;
                return true;
            case JnpfKeyConst.CREATEUSER:
                newDataItems[fieldKey] = ctx.CreateUserId;
                return true;
            case JnpfKeyConst.MODIFYTIME:
                newDataItems[fieldKey] = string.Empty;
                return true;
            case JnpfKeyConst.CREATETIME:
                newDataItems[fieldKey] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", ctx.Now);
                return true;
            case JnpfKeyConst.CURRORGANIZE:
                newDataItems[fieldKey] = ctx.OrganizeId != null ? ctx.OrganizeId : string.Empty;
                return true;
            default:
                return false;
        }
    }

    public static void MapBillRule(
        string fieldKey,
        string billNumber,
        Dictionary<string, object> newDataItems)
    {
        if (!MissingBillRuleMessage.Equals(billNumber))
            newDataItems[fieldKey] = billNumber;
        else
            newDataItems[fieldKey] = string.Empty;
    }

    public static void MapCurrPosition(
        string fieldKey,
        string? positionId,
        Dictionary<string, object> newDataItems)
    {
        newDataItems[fieldKey] = positionId.IsNotEmptyOrNull() ? positionId! : string.Empty;
    }
}
