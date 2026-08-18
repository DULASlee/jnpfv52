using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Systems.Entitys.Permission;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Bind "defaultCurrent" user/org selectors on form field JSON (FieldBindDefaultValue).
/// preferredPositionId replaces RunService's _userManager.User.PositionId dependency.
/// </summary>
public static class FieldBindDefaultValueHelpers
{
    public static void Bind(
        ref List<Dictionary<string, object>> dicFieldsModelList,
        string defaultUserId,
        string defaultDepId,
        List<string> defaultPosIds,
        List<string> defaultRoleIds,
        List<string> defaultGroupIds,
        List<UserRelationEntity> userRelationList,
        string? preferredPositionId)
    {
        foreach (var item in dicFieldsModelList)
        {
            var obj = item["__config__"].ToObject<Dictionary<string, object>>();

            if (obj.ContainsKey("jnpfKey")
                && (obj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.DEPSELECT)
                    || obj["jnpfKey"].Equals(JnpfKeyConst.POSSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.ROLESELECT)
                    || obj["jnpfKey"].Equals(JnpfKeyConst.GROUPSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.USERSSELECT))
                && obj["defaultCurrent"].Equals(true))
            {
                switch (obj["jnpfKey"])
                {
                    case JnpfKeyConst.USERSSELECT:
                    case JnpfKeyConst.USERSELECT:
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var ableDepIds = item["ableDepIds"].ToObject<List<string>>() ?? new List<string>();
                            var ablePosIds = item["ablePosIds"].ToObject<List<string>>() ?? new List<string>();
                            var ableUserIds = item["ableUserIds"].ToObject<List<string>>() ?? new List<string>();
                            var ableRoleIds = item["ableRoleIds"].ToObject<List<string>>() ?? new List<string>();
                            var ableGroupIds = item["ableGroupIds"].ToObject<List<string>>() ?? new List<string>();
                            var userIdList = userRelationList.Where(x => ableUserIds.Contains(x.UserId) || ableDepIds.Contains(x.ObjectId)
                                || ablePosIds.Contains(x.ObjectId) || ableRoleIds.Contains(x.ObjectId) || ableGroupIds.Contains(x.ObjectId)).Select(x => x.UserId).ToList();
                            if (!userIdList.Contains(defaultUserId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }

                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                        {
                            if (obj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT))
                                obj["defaultValue"] = new List<string> { defaultUserId };
                            else
                                obj["defaultValue"] = new List<string> { string.Format("{0}--user", defaultUserId) };
                        }
                        else
                        {
                            obj["defaultValue"] = defaultUserId;
                        }

                        break;
                    case JnpfKeyConst.DEPSELECT:
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var defValue = item["ableDepIds"].ToObject<List<string>>();
                            if (!defValue.Contains(defaultDepId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }

                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                            obj["defaultValue"] = new List<string> { defaultDepId };
                        else
                            obj["defaultValue"] = defaultDepId;
                        break;
                    case JnpfKeyConst.POSSELECT:
                        var defaultPosId = defaultPosIds.FirstOrDefault();
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var defValue = item["ablePosIds"].ToObject<List<string>>();
                            if (!defValue.Contains(defaultPosId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }

                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                        {
                            obj["defaultValue"] = defaultPosIds;
                        }
                        else
                        {
                            if (defaultPosIds.Contains(preferredPositionId))
                                obj["defaultValue"] = preferredPositionId;
                            else
                                obj["defaultValue"] = defaultPosId;
                        }

                        break;
                    case JnpfKeyConst.ROLESELECT:
                        var defaultRoleId = defaultRoleIds.FirstOrDefault();
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var defValue = item["ableRoleIds"].ToObject<List<string>>();
                            if (!defValue.Contains(defaultRoleId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }

                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                            obj["defaultValue"] = defaultRoleIds;
                        else
                            obj["defaultValue"] = defaultRoleId;
                        break;
                    case JnpfKeyConst.GROUPSELECT:
                        var defaultGroupId = defaultGroupIds.FirstOrDefault();
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var defValue = item["ableGroupIds"].ToObject<List<string>>();
                            if (!defValue.Contains(defaultGroupId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }

                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                            obj["defaultValue"] = defaultGroupIds;
                        else
                            obj["defaultValue"] = defaultGroupId;
                        break;
                }
            }

            // 子表控件
            if (obj.ContainsKey("jnpfKey") && obj["jnpfKey"].Equals(JnpfKeyConst.TABLE))
            {
                var cList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                foreach (var child in cList)
                {
                    var cObj = child["__config__"].ToObject<Dictionary<string, object>>();
                    if (cObj.ContainsKey("jnpfKey")
                        && (cObj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT) || cObj["jnpfKey"].Equals(JnpfKeyConst.DEPSELECT)
                            || cObj["jnpfKey"].Equals(JnpfKeyConst.POSSELECT) || cObj["jnpfKey"].Equals(JnpfKeyConst.ROLESELECT)
                            || cObj["jnpfKey"].Equals(JnpfKeyConst.GROUPSELECT) || cObj["jnpfKey"].Equals(JnpfKeyConst.USERSSELECT))
                        && cObj["defaultCurrent"].Equals(true))
                    {
                        switch (cObj["jnpfKey"])
                        {
                            case JnpfKeyConst.USERSSELECT:
                            case JnpfKeyConst.USERSELECT:
                                // Legacy: uses parent TABLE item's multiple flag
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                                {
                                    if (cObj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT))
                                        cObj["defaultValue"] = new List<string> { defaultUserId };
                                    else
                                        cObj["defaultValue"] = new List<string> { string.Format("{0}--user", defaultUserId) };
                                }
                                else
                                {
                                    cObj["defaultValue"] = defaultUserId;
                                }

                                break;
                            case JnpfKeyConst.DEPSELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                                    cObj["defaultValue"] = new List<string> { defaultDepId };
                                else
                                    cObj["defaultValue"] = defaultDepId;
                                break;
                            case JnpfKeyConst.POSSELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                                {
                                    cObj["defaultValue"] = defaultPosIds;
                                }
                                else
                                {
                                    if (defaultPosIds.Contains(preferredPositionId))
                                        cObj["defaultValue"] = preferredPositionId;
                                    else
                                        cObj["defaultValue"] = defaultPosIds.FirstOrDefault();
                                }

                                break;
                            case JnpfKeyConst.ROLESELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                                    cObj["defaultValue"] = defaultRoleIds;
                                else
                                    cObj["defaultValue"] = defaultRoleIds.FirstOrDefault();
                                break;
                            case JnpfKeyConst.GROUPSELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                                    cObj["defaultValue"] = defaultGroupIds;
                                else
                                    cObj["defaultValue"] = defaultGroupIds.FirstOrDefault();
                                break;
                        }
                    }

                    child["__config__"] = cObj;
                }

                obj["children"] = cList;
            }

            // 递归布局控件
            if (obj.ContainsKey("children"))
            {
                var fmList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                Bind(ref fmList, defaultUserId, defaultDepId, defaultPosIds, defaultRoleIds, defaultGroupIds, userRelationList, preferredPositionId);
                obj["children"] = fmList;
            }

            item["__config__"] = obj;
        }
    }
}
