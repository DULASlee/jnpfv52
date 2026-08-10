using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// RATE / SLIDER / NUMINPUT import bounds validation.
/// VisualDev and CodeGen keep distinct parse/error/empty semantics via <see cref="ImportNumericSemantics"/>.
/// </summary>
public enum ImportNumericSemantics
{
    VisualDev = 0,
    CodeGen = 1,
}

public static class ImportNumericFieldAssembler
{
    public static void MapRate(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        Dictionary<string, object> newDataItems,
        ImportNumericSemantics semantics)
    {
        if (!rawValue.IsNotEmptyOrNull())
        {
            if (semantics == ImportNumericSemantics.CodeGen)
                newDataItems[fieldKey] = null;
            return;
        }

        try
        {
            if (semantics == ImportNumericSemantics.VisualDev)
            {
                var value = double.Parse(rawValue.ToString()!);
                if (value < 0)
                    throw new Exception(string.Empty);

                if (vModel.allowHalf)
                {
                    if (value % 0.5 != 0)
                        throw new Exception(string.Empty);
                }
                else if (value % 1 != 0)
                {
                    throw new Exception(string.Empty);
                }

                if (vModel.count != null && vModel.count < value)
                    ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 评分超过设置的最大值");
            }
            else
            {
                var value = int.Parse(rawValue.ToString()!);
                if (vModel.max != null && vModel.max < value)
                    ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 评分超过设置的最大值");
            }
        }
        catch
        {
            var msg = semantics == ImportNumericSemantics.VisualDev
                ? vModel.__config__.label + ": 值不正确"
                : vModel.__config__.label + ": 评分格式错误";
            ImportAssembleErrors.Append(newDataItems, msg);
        }
    }

    public static void MapSlider(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        Dictionary<string, object> newDataItems,
        ImportNumericSemantics semantics)
    {
        if (!rawValue.IsNotEmptyOrNull())
        {
            if (semantics == ImportNumericSemantics.CodeGen)
                newDataItems[fieldKey] = null;
            return;
        }

        try
        {
            if (semantics == ImportNumericSemantics.VisualDev)
            {
                var value = decimal.Parse(rawValue.ToString()!);
                AppendMinMax(vModel, value, newDataItems, "滑块");
            }
            else
            {
                var value = int.Parse(rawValue.ToString()!);
                AppendMinMax(vModel, value, newDataItems, "滑块");
            }
        }
        catch
        {
            var msg = semantics == ImportNumericSemantics.VisualDev
                ? vModel.__config__.label + ": 值不正确"
                : vModel.__config__.label + ": 滑块格式错误";
            ImportAssembleErrors.Append(newDataItems, msg);
        }
    }

    public static void MapNumberInput(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        Dictionary<string, object> newDataItems,
        ImportNumericSemantics semantics)
    {
        if (!rawValue.IsNotEmptyOrNull())
        {
            if (semantics == ImportNumericSemantics.CodeGen)
                newDataItems[fieldKey] = null;
            return;
        }

        try
        {
            var value = decimal.Parse(rawValue.ToString()!);
            AppendMinMax(vModel, value, newDataItems, "数字输入");
        }
        catch
        {
            var msg = semantics == ImportNumericSemantics.VisualDev
                ? vModel.__config__.label + ": 值不正确"
                : vModel.__config__.label + ": 数字输入格式错误";
            ImportAssembleErrors.Append(newDataItems, msg);
        }
    }

    private static void AppendMinMax(FieldsModel vModel, decimal value, Dictionary<string, object> row, string controlName)
    {
        if (vModel.max != null && vModel.max < value)
            ImportAssembleErrors.Append(row, vModel.__config__.label + $": {controlName}超过设置的最大值");
        if (vModel.min != null && vModel.min > value)
            ImportAssembleErrors.Append(row, vModel.__config__.label + $": {controlName}超过设置的最小值");
    }

    private static void AppendMinMax(FieldsModel vModel, int value, Dictionary<string, object> row, string controlName)
    {
        if (vModel.max != null && vModel.max < value)
            ImportAssembleErrors.Append(row, vModel.__config__.label + $": {controlName}超过设置的最大值");
        if (vModel.min != null && vModel.min > value)
            ImportAssembleErrors.Append(row, vModel.__config__.label + $": {controlName}超过设置的最小值");
    }
}
