using JNPF.Common.Const;
using JNPF.Common.Extension;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Pure builders for GetCDataList option caches (SWITCH / static / dictionary / POPUP add).
/// VisualDev SELECT dictionary uses exact propsValue "id"|"enCode";
/// CodeGen SELECT / TREESELECT dictionary uses ToLower()=="encode".
/// </summary>
public static class ImportCacheBagHelpers
{
    public enum DictionaryPropsMode
    {
        /// <summary>VisualDev SELECT/RADIO/CHECKBOX: Equals("id") / Equals("enCode"); empty dict still added.</summary>
        VisualDevExact,

        /// <summary>CodeGen SELECT + both TREESELECT paths: ToLower() == "encode" else Id.</summary>
        EncodeCaseInsensitive,
    }

    public static bool TryAdd(
        Dictionary<string, List<Dictionary<string, string>>> resData,
        string vModel,
        List<Dictionary<string, string>>? data)
    {
        if (resData == null || vModel.IsNullOrEmpty() || data == null || resData.ContainsKey(vModel))
            return false;
        resData.Add(vModel, data);
        return true;
    }

    public static List<Dictionary<string, string>> BuildSwitchPairs(string? activeTxt, string? inactiveTxt) =>
        new()
        {
            new() { ["1"] = activeTxt },
            new() { ["0"] = inactiveTxt },
        };

    public static List<Dictionary<string, string>> BuildStaticOptionPairs(
        IEnumerable<Dictionary<string, object>>? options,
        string propsValue,
        string propsLabel)
    {
        var addItem = new List<Dictionary<string, string>>();
        if (options == null) return addItem;
        foreach (var option in options)
        {
            addItem.Add(new Dictionary<string, string>
            {
                [option[propsValue].ToString()!] = option[propsLabel].ToString()!,
            });
        }

        return addItem;
    }

    public static List<Dictionary<string, string>> BuildDictionaryPairs(
        IEnumerable<(string Id, string EnCode, string FullName)> rows,
        string? propsValue,
        DictionaryPropsMode mode)
    {
        var addItem = new List<Dictionary<string, string>>();
        if (rows == null) return addItem;

        if (mode == DictionaryPropsMode.EncodeCaseInsensitive)
        {
            var useEncode = propsValue != null && propsValue.ToLower().Equals("encode");
            foreach (var it in rows)
            {
                addItem.Add(new Dictionary<string, string>
                {
                    [useEncode ? it.EnCode : it.Id] = it.FullName,
                });
            }

            return addItem;
        }

        foreach (var it in rows)
        {
            var dictionary = new Dictionary<string, string>();
            if (propsValue != null && propsValue.Equals("id")) dictionary.Add(it.Id, it.FullName);
            if (propsValue != null && propsValue.Equals("enCode")) dictionary.Add(it.EnCode, it.FullName);
            addItem.Add(dictionary);
        }

        return addItem;
    }

    /// <summary>RELATIONFORM redis key used by VisualDev GetCDataList (legacy string concat).</summary>
    public static string BuildRelationFormRedisKey(string tenantId, string jnpfKey, string? renderKey)
        => CommonConst.VISUALDEV + tenantId + "_" + jnpfKey + "_" + renderKey;
}
