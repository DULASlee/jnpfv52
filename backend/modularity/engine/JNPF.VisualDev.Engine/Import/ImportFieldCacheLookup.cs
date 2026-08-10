using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Resolve option/cache dictionaries for a field during import assemble.
/// </summary>
public static class ImportFieldCacheLookup
{
    public static List<Dictionary<string, string>> Resolve(
        FieldsModel vModel,
        Dictionary<string, List<Dictionary<string, string>>> cDataList)
    {
        var dicList = new List<Dictionary<string, string>>();
        if (vModel?.__config__ == null || cDataList == null)
            return dicList;

        if (cDataList.ContainsKey(vModel.__config__.jnpfKey))
            dicList = cDataList[vModel.__config__.jnpfKey];
        if ((dicList == null || !dicList.Any()) && cDataList.ContainsKey(vModel.__vModel__))
            dicList = cDataList[vModel.__vModel__];

        return dicList ?? new List<Dictionary<string, string>>();
    }
}
