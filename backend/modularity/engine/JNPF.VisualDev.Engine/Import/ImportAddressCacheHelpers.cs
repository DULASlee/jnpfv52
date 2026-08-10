using JNPF.Common.Extension;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;

namespace JNPF.VisualDev.Engine.Import;

/// <summary>
/// Pure builders for GetCDataList ADDRESS / COMSELECT / Id-EnCode caches
/// shared by VisualDev + CodeGen. DB/cache I/O stays at call sites.
/// </summary>
/// <remarks>
/// <see cref="GetAddressIdByPList"/> rewrites <c>ProvinceEntity.Id</c> in place while walking
/// parents (legacy). Callers must not reuse the same list for subsequent Id lookups.
/// </remarks>
public static class ImportAddressCacheHelpers
{
    public const string CacheKey = "Import_Address";

    /// <summary>
    /// Build import address lookup pairs from province rows.
    /// Mutates QuickQuery/Description (and Id for no-Type recursion) like legacy.
    /// Typed levels are added during the walk and again in the final ForEach (legacy duplicates).
    /// </summary>
    public static List<Dictionary<string, string>> BuildPairs(List<ProvinceEntity> dataList)
    {
        var addItem = new List<Dictionary<string, string>>();
        if (dataList == null || dataList.Count == 0)
            return addItem;

        dataList.Where(x => x.Type == "1").ToList().ForEach(item =>
        {
            item.QuickQuery = item.FullName;
            item.Description = item.Id;
            Dictionary<string, string> address = new Dictionary<string, string>();
            address.Add(item.Description, item.QuickQuery);
            addItem.Add(address);
        });
        dataList.Where(x => x.Type == "2").ToList().ForEach(item =>
        {
            item.QuickQuery = dataList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName;
            item.Description = dataList.Find(x => x.Id == item.ParentId).Description + "," + item.Id;
            Dictionary<string, string> address = new Dictionary<string, string>();
            address.Add(item.Description, item.QuickQuery);
            addItem.Add(address);
        });
        dataList.Where(x => x.Type == "3").ToList().ForEach(item =>
        {
            item.QuickQuery = dataList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName;
            item.Description = dataList.Find(x => x.Id == item.ParentId).Description + "," + item.Id;
            Dictionary<string, string> address = new Dictionary<string, string>();
            address.Add(item.Description, item.QuickQuery);
            addItem.Add(address);
        });
        dataList.Where(x => x.Type == "4").ToList().ForEach(item =>
        {
            ProvinceEntity? it = dataList.Find(x => x.Id == item.ParentId);
            if (it != null)
            {
                item.QuickQuery = it.QuickQuery + "/" + item.FullName;
                item.Description = it.Description + "," + item.Id;
                Dictionary<string, string> address = new Dictionary<string, string>();
                address.Add(item.Description, item.QuickQuery);
                addItem.Add(address);
            }
        });
        dataList.ForEach(it =>
        {
            if (it.Description.IsNotEmptyOrNull())
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add(it.Description, it.QuickQuery);
                addItem.Add(dictionary);
            }
        });

        var noTypeList = dataList.Where(x => x.Type.IsNullOrWhiteSpace()).ToList();
        foreach (var it in noTypeList)
        {
            it.QuickQuery = GetAddressByPList(noTypeList, it);
            it.Description = GetAddressIdByPList(noTypeList, it);
        }
        foreach (var it in noTypeList)
        {
            Dictionary<string, string> address = new Dictionary<string, string>();
            address.Add(it.Description, it.QuickQuery);
            addItem.Add(address);
        }

        return addItem;
    }

    public static string GetAddressByPList(List<ProvinceEntity> addressEntityList, ProvinceEntity pEntity)
    {
        if (pEntity.ParentId == null || pEntity.ParentId.Equals("-1"))
        {
            return pEntity.FullName;
        }

        var pItem = addressEntityList.Find(x => x.Id == pEntity.ParentId);
        if (pItem != null) pEntity.QuickQuery = GetAddressByPList(addressEntityList, pItem) + "/" + pEntity.FullName;
        else pEntity.QuickQuery = pEntity.FullName;
        return pEntity.QuickQuery;
    }

    public static string GetAddressIdByPList(List<ProvinceEntity> addressEntityList, ProvinceEntity pEntity)
    {
        if (pEntity.ParentId == null || pEntity.ParentId.Equals("-1"))
        {
            return pEntity.Id;
        }

        var pItem = addressEntityList.Find(x => x.Id == pEntity.ParentId);
        if (pItem != null) pEntity.Id = GetAddressIdByPList(addressEntityList, pItem) + "," + pEntity.Id;
        else pEntity.Id = pEntity.Id;
        return pEntity.Id;
    }

    /// <summary>
    /// COMSELECT: map OrganizeIdTree → FullName path using allDataList for name lookup.
    /// </summary>
    public static List<Dictionary<string, string>> BuildOrganizeTreePairs(
        List<OrganizeEntity> allDataList,
        List<OrganizeEntity> dataList)
    {
        var addItem = new List<Dictionary<string, string>>();
        if (allDataList == null || dataList == null)
            return addItem;

        foreach (var item in dataList)
        {
            if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
            var orgNameList = new List<string>();
            item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
            {
                var org = allDataList.Find(x => x.Id == it);
                if (org != null) orgNameList.Add(org.FullName);
            });
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add(item.OrganizeIdTree, string.Join("/", orgNameList));
            addItem.Add(dictionary);
        }

        return addItem;
    }

    public static List<Dictionary<string, string>> BuildIdEncodePairs(
        IEnumerable<(string Id, string EnCode)> rows)
    {
        var addItem = new List<Dictionary<string, string>>();
        if (rows == null)
            return addItem;
        foreach (var item in rows)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add(item.Id, item.EnCode);
            addItem.Add(dictionary);
        }

        return addItem;
    }
}
