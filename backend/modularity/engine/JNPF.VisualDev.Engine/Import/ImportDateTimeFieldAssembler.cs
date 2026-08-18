using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;
using System.Globalization;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Shared DATE/TIME import assemble (parse + bound check + write).
/// Extracted from VisualDevModelDataService / ExportImportDataHelper ImportDataAssemble.
/// </summary>
public static class ImportDateTimeFieldAssembler
{
    public static void MapDate(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        IReadOnlyDictionary<string, object> dataItems,
        Dictionary<string, object> newDataItems,
        ImportDateTimeSemantics semantics)
    {
        try
        {
            if (rawValue.IsNotEmptyOrNull())
            {
                var value = DateTime.ParseExact(
                    rawValue.ToString()!.TrimEnd(),
                    vModel.format,
                    CultureInfo.CurrentCulture);

                if (ShouldApplyStartRule(vModel, semantics))
                {
                    var minDate = string.Format("{0:" + vModel.format + "}", DateTime.Now).ParseToDateTime();
                    minDate = ResolveDateBound(
                        minDate,
                        vModel.__config__.startTimeType,
                        vModel.__config__.startTimeTarget,
                        vModel.__config__.startTimeValue,
                        vModel.__config__.startRelationField,
                        dataItems,
                        semantics,
                        useTimestampForFixed: true);

                    if (IsBelowMin(value, minDate, semantics))
                        ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 日期选择值不在范围内");
                }

                if (ShouldApplyEndRule(vModel, semantics))
                {
                    var maxDate = string.Format("{0:" + vModel.format + "}", DateTime.Now).ParseToDateTime();
                    maxDate = ResolveDateBound(
                        maxDate,
                        vModel.__config__.endTimeType,
                        vModel.__config__.startTimeTarget,
                        vModel.__config__.endTimeValue,
                        vModel.__config__.endRelationField,
                        dataItems,
                        semantics,
                        useTimestampForFixed: true);

                    if (IsAboveMax(value, maxDate, semantics))
                        ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 日期选择值不在范围内");
                }

                newDataItems[fieldKey] = value.ParseToUnixTime();
            }
            else if (semantics == ImportDateTimeSemantics.CodeGen)
            {
                newDataItems[fieldKey] = null;
            }
        }
        catch
        {
            ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 值不正确");
        }
    }

    public static void MapTime(
        FieldsModel vModel,
        string fieldKey,
        object? rawValue,
        IReadOnlyDictionary<string, object> dataItems,
        Dictionary<string, object> newDataItems,
        ImportDateTimeSemantics semantics)
    {
        try
        {
            if (rawValue.IsNotEmptyOrNull())
            {
                var value = DateTime.ParseExact(
                    rawValue.ToString()!.TrimEnd(),
                    vModel.format,
                    CultureInfo.CurrentCulture);

                if (ShouldApplyStartRule(vModel, semantics))
                {
                    var minTime = value;
                    minTime = ResolveTimeBound(
                        minTime,
                        vModel.__config__.startTimeType,
                        vModel.__config__.startTimeTarget,
                        vModel.__config__.startTimeValue,
                        vModel.__config__.startRelationField,
                        dataItems,
                        semantics);

                    if (IsBelowMin(value, minTime, semantics))
                        ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 时间选择值不在范围内");
                }

                if (ShouldApplyEndRule(vModel, semantics))
                {
                    var maxTime = value;
                    maxTime = ResolveTimeBound(
                        maxTime,
                        vModel.__config__.endTimeType,
                        vModel.__config__.startTimeTarget,
                        vModel.__config__.endTimeValue,
                        vModel.__config__.endRelationField,
                        dataItems,
                        semantics);

                    if (IsAboveMax(value, maxTime, semantics))
                        ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 时间选择值不在范围内");
                }
            }
            else if (semantics == ImportDateTimeSemantics.CodeGen)
            {
                newDataItems[fieldKey] = null;
            }
        }
        catch
        {
            ImportAssembleErrors.Append(newDataItems, vModel.__config__.label + ": 值不正确");
        }
    }

    private static bool ShouldApplyStartRule(FieldsModel vModel, ImportDateTimeSemantics semantics)
        => semantics == ImportDateTimeSemantics.VisualDev
            ? vModel.__config__.startTimeRule
            : vModel.__config__.startTimeRule && vModel.__config__.startTimeValue.IsNotEmptyOrNull();

    private static bool ShouldApplyEndRule(FieldsModel vModel, ImportDateTimeSemantics semantics)
        => semantics == ImportDateTimeSemantics.VisualDev
            ? vModel.__config__.endTimeRule
            : vModel.__config__.endTimeRule && vModel.__config__.endTimeValue.IsNotEmptyOrNull();

    private static bool IsBelowMin(DateTime value, DateTime min, ImportDateTimeSemantics semantics)
        => semantics == ImportDateTimeSemantics.VisualDev
            ? min > value && !min.Equals(DateTime.MinValue)
            : min > value;

    private static bool IsAboveMax(DateTime value, DateTime max, ImportDateTimeSemantics semantics)
        => semantics == ImportDateTimeSemantics.VisualDev
            ? max < value && !max.Equals(DateTime.MinValue)
            : max < value;

    private static DateTime ResolveDateBound(
        DateTime seed,
        int type,
        int target,
        string? fixedOrOffsetValue,
        string? relationField,
        IReadOnlyDictionary<string, object> dataItems,
        ImportDateTimeSemantics semantics,
        bool useTimestampForFixed)
    {
        switch (type)
        {
            case 1:
                if (fixedOrOffsetValue.IsNotEmptyOrNull())
                    return useTimestampForFixed
                        ? fixedOrOffsetValue.TimeStampToDateTime()
                        : DateTime.Parse(fixedOrOffsetValue);
                return seed;
            case 2:
                return ResolveRelationDate(seed, relationField, dataItems, semantics, parseAsDateTime: true, trimEnd: true);
            case 3:
                return seed;
            case 4:
                return ApplyDateOffset(seed, target, fixedOrOffsetValue, subtract: true);
            case 5:
                return ApplyDateOffset(seed, target, fixedOrOffsetValue, subtract: false);
            default:
                return seed;
        }
    }

    private static DateTime ResolveTimeBound(
        DateTime seed,
        int type,
        int target,
        string? fixedOrOffsetValue,
        string? relationField,
        IReadOnlyDictionary<string, object> dataItems,
        ImportDateTimeSemantics semantics)
    {
        switch (type)
        {
            case 1:
                if (fixedOrOffsetValue.IsNotEmptyOrNull())
                    return DateTime.Parse(fixedOrOffsetValue);
                return seed;
            case 2:
                // VisualDev TIME: ParseToDateTime without TrimEnd; CodeGen TIME: DateTime.Parse.
                return ResolveRelationDate(
                    seed,
                    relationField,
                    dataItems,
                    semantics,
                    parseAsDateTime: semantics == ImportDateTimeSemantics.VisualDev,
                    trimEnd: false);
            case 3:
                return seed;
            case 4:
                return ApplyTimeOffset(seed, target, fixedOrOffsetValue, subtract: true);
            case 5:
                return ApplyTimeOffset(seed, target, fixedOrOffsetValue, subtract: false);
            default:
                return seed;
        }
    }

    private static DateTime ResolveRelationDate(
        DateTime seed,
        string? relationField,
        IReadOnlyDictionary<string, object> dataItems,
        ImportDateTimeSemantics semantics,
        bool parseAsDateTime,
        bool trimEnd)
    {
        if (relationField.IsNullOrEmpty() || !dataItems.ContainsKey(relationField))
            return seed;

        if (semantics == ImportDateTimeSemantics.VisualDev)
        {
            if (dataItems[relationField] == null)
                return DateTime.MinValue;
            var data = dataItems[relationField].ToString() ?? string.Empty;
            if (trimEnd) data = data.TrimEnd();
            return parseAsDateTime ? data.ParseToDateTime() : DateTime.Parse(data);
        }

        // CodeGen: no null short-circuit (legacy).
        var raw = dataItems[relationField].ToString() ?? string.Empty;
        if (trimEnd) raw = raw.TrimEnd();
        return parseAsDateTime ? raw.ParseToDateTime() : DateTime.Parse(raw);
    }

    private static DateTime ApplyDateOffset(DateTime seed, int target, string? amount, bool subtract)
    {
        var n = amount.ParseToInt();
        if (subtract) n = -n;
        return target switch
        {
            1 => seed.AddYears(n),
            2 => seed.AddMonths(n),
            3 => seed.AddDays(n),
            _ => seed,
        };
    }

    private static DateTime ApplyTimeOffset(DateTime seed, int target, string? amount, bool subtract)
    {
        var n = amount.ParseToInt();
        if (subtract) n = -n;
        return target switch
        {
            1 => seed.AddHours(n),
            2 => seed.AddMinutes(n),
            3 => seed.AddSeconds(n),
            _ => seed,
        };
    }
}
