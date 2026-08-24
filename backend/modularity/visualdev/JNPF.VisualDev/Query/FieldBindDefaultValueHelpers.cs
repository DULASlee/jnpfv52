using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Systems.Entitys.Permission;

namespace JNPF.VisualDev.Query;

/// <summary>
/// Bind "defaultCurrent" user/org selectors on form field JSON (FieldBindDefaultValue).
/// preferredPositionId replaces RunService's _userManager.User.PositionId dependency.
/// D1 拆分（战役 D1 · 规格 §2.2）：纯结构重构，行为不变量 I1-I9 由
/// FieldBindDefaultValueHelpersTests 19 用例全分支锁定（含 Q4/Q5 怪异保真，注释以 I 编号标注）。
/// </summary>
public static class FieldBindDefaultValueHelpers
{
    /// <summary>
    /// 默认值上下文（收敛 7 参数透传；只读快照语义）.
    /// </summary>
    private sealed class BindDefaults
    {
        public string UserId { get; init; } = string.Empty;
        public string DepId { get; init; } = string.Empty;
        public List<string> PosIds { get; init; } = new();
        public List<string> RoleIds { get; init; } = new();
        public List<string> GroupIds { get; init; } = new();
        public List<UserRelationEntity> UserRelationList { get; init; } = new();
        public string? PreferredPositionId { get; init; }
    }

    /// <summary>
    /// 门面：遍历 + 主表分派 + TABLE 子表分派 + children 递归（签名与变异顺序不变，I8）.
    /// </summary>
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
        var ctx = new BindDefaults
        {
            UserId = defaultUserId,
            DepId = defaultDepId,
            PosIds = defaultPosIds,
            RoleIds = defaultRoleIds,
            GroupIds = defaultGroupIds,
            UserRelationList = userRelationList,
            PreferredPositionId = preferredPositionId,
        };

        foreach (var item in dicFieldsModelList)
        {
            var obj = item["__config__"].ToObject<Dictionary<string, object>>();

            TryBindMainSelector(item, obj, ctx);

            // 子表控件
            if (obj.ContainsKey("jnpfKey") && obj["jnpfKey"].Equals(JnpfKeyConst.TABLE))
            {
                BindTableChildren(item, obj, ctx);
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

    /// <summary>
    /// I1 主表门（六选择器 + defaultCurrent）+ 按键分派到解析器.
    /// </summary>
    private static void TryBindMainSelector(Dictionary<string, object> item, Dictionary<string, object> obj, BindDefaults ctx)
    {
        if (!obj.ContainsKey("jnpfKey")
            || !(obj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.DEPSELECT)
                || obj["jnpfKey"].Equals(JnpfKeyConst.POSSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.ROLESELECT)
                || obj["jnpfKey"].Equals(JnpfKeyConst.GROUPSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.USERSSELECT))
            || !obj["defaultCurrent"].Equals(true))
            return;

        switch (obj["jnpfKey"])
        {
            case JnpfKeyConst.USERSSELECT:
            case JnpfKeyConst.USERSELECT:
                ResolveUserDefault(item, obj, ctx);
                break;
            case JnpfKeyConst.DEPSELECT:
                ResolveDepDefault(item, obj, ctx);
                break;
            case JnpfKeyConst.POSSELECT:
                ResolvePosDefault(item, obj, ctx);
                break;
            case JnpfKeyConst.ROLESELECT:
                ResolveRoleDefault(item, obj, ctx);
                break;
            case JnpfKeyConst.GROUPSELECT:
                ResolveGroupDefault(item, obj, ctx);
                break;
        }
    }

    /// <summary>
    /// I2+I3：USERSSELECT/USERSELECT — custom 五 able 集合过滤 + 单多选装配.
    /// I3 实测形态：--user 后缀为多选分支专属（USERSSELECT），单选均裸 userId.
    /// </summary>
    private static void ResolveUserDefault(Dictionary<string, object> item, Dictionary<string, object> obj, BindDefaults ctx)
    {
        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
        {
            var ableDepIds = item["ableDepIds"].ToObject<List<string>>() ?? new List<string>();
            var ablePosIds = item["ablePosIds"].ToObject<List<string>>() ?? new List<string>();
            var ableUserIds = item["ableUserIds"].ToObject<List<string>>() ?? new List<string>();
            var ableRoleIds = item["ableRoleIds"].ToObject<List<string>>() ?? new List<string>();
            var ableGroupIds = item["ableGroupIds"].ToObject<List<string>>() ?? new List<string>();
            var userIdList = ctx.UserRelationList.Where(x => ableUserIds.Contains(x.UserId) || ableDepIds.Contains(x.ObjectId)
                || ablePosIds.Contains(x.ObjectId) || ableRoleIds.Contains(x.ObjectId) || ableGroupIds.Contains(x.ObjectId)).Select(x => x.UserId).ToList();
            if (!userIdList.Contains(ctx.UserId))
            {
                obj["defaultValue"] = null;
                return;
            }
        }

        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
        {
            if (obj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT))
                obj["defaultValue"] = new List<string> { ctx.UserId };
            else
                obj["defaultValue"] = new List<string> { string.Format("{0}--user", ctx.UserId) };
        }
        else
        {
            obj["defaultValue"] = ctx.UserId;
        }
    }

    /// <summary>
    /// I4：DEPSELECT — custom 校验 + 单多选装配.
    /// </summary>
    private static void ResolveDepDefault(Dictionary<string, object> item, Dictionary<string, object> obj, BindDefaults ctx)
    {
        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
        {
            var defValue = item["ableDepIds"].ToObject<List<string>>();
            if (!defValue.Contains(ctx.DepId))
            {
                obj["defaultValue"] = null;
                return;
            }
        }

        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
            obj["defaultValue"] = new List<string> { ctx.DepId };
        else
            obj["defaultValue"] = ctx.DepId;
    }

    /// <summary>
    /// I5：POSSELECT — custom 校验 + preferredPositionId 单选优先（多选整表，不参与 preferred）.
    /// </summary>
    private static void ResolvePosDefault(Dictionary<string, object> item, Dictionary<string, object> obj, BindDefaults ctx)
    {
        var defaultPosId = ctx.PosIds.FirstOrDefault();
        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
        {
            var defValue = item["ablePosIds"].ToObject<List<string>>();
            if (!defValue.Contains(defaultPosId))
            {
                obj["defaultValue"] = null;
                return;
            }
        }

        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
        {
            obj["defaultValue"] = ctx.PosIds;
        }
        else
        {
            if (ctx.PosIds.Contains(ctx.PreferredPositionId))
                obj["defaultValue"] = ctx.PreferredPositionId;
            else
                obj["defaultValue"] = defaultPosId;
        }
    }

    /// <summary>
    /// I4：ROLESELECT — custom 校验 + 单多选装配.
    /// </summary>
    private static void ResolveRoleDefault(Dictionary<string, object> item, Dictionary<string, object> obj, BindDefaults ctx)
    {
        var defaultRoleId = ctx.RoleIds.FirstOrDefault();
        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
        {
            var defValue = item["ableRoleIds"].ToObject<List<string>>();
            if (!defValue.Contains(defaultRoleId))
            {
                obj["defaultValue"] = null;
                return;
            }
        }

        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
            obj["defaultValue"] = ctx.RoleIds;
        else
            obj["defaultValue"] = defaultRoleId;
    }

    /// <summary>
    /// I4：GROUPSELECT — custom 校验 + 单多选装配.
    /// </summary>
    private static void ResolveGroupDefault(Dictionary<string, object> item, Dictionary<string, object> obj, BindDefaults ctx)
    {
        var defaultGroupId = ctx.GroupIds.FirstOrDefault();
        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
        {
            var defValue = item["ableGroupIds"].ToObject<List<string>>();
            if (!defValue.Contains(defaultGroupId))
            {
                obj["defaultValue"] = null;
                return;
            }
        }

        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
            obj["defaultValue"] = ctx.GroupIds;
        else
            obj["defaultValue"] = defaultGroupId;
    }

    /// <summary>
    /// I6（Q4 怪异，保真）：子表分支复用父 TABLE 项的 multiple 标志（item），非子控件自身.
    /// I7（Q5 怪异，保真）：子表分支无 custom 限定域校验（与主表不一致）.
    /// I9：USERSSELECT 多选同样使用 --user 后缀.
    /// 注：children 随后经门面布局递归以子控件自身标志重绑（既有叠加效应，由测试锁定）。
    /// </summary>
    private static void BindTableChildren(Dictionary<string, object> item, Dictionary<string, object> obj, BindDefaults ctx)
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
                // Legacy: uses parent TABLE item's multiple flag
                var multiple = item.ContainsKey("multiple") && item["multiple"].Equals(true);
                switch (cObj["jnpfKey"])
                {
                    case JnpfKeyConst.USERSSELECT:
                    case JnpfKeyConst.USERSELECT:
                        if (multiple)
                        {
                            if (cObj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT))
                                cObj["defaultValue"] = new List<string> { ctx.UserId };
                            else
                                cObj["defaultValue"] = new List<string> { string.Format("{0}--user", ctx.UserId) };
                        }
                        else
                        {
                            cObj["defaultValue"] = ctx.UserId;
                        }

                        break;
                    case JnpfKeyConst.DEPSELECT:
                        cObj["defaultValue"] = multiple ? new List<string> { ctx.DepId } : ctx.DepId;
                        break;
                    case JnpfKeyConst.POSSELECT:
                        if (multiple)
                        {
                            cObj["defaultValue"] = ctx.PosIds;
                        }
                        else
                        {
                            if (ctx.PosIds.Contains(ctx.PreferredPositionId))
                                cObj["defaultValue"] = ctx.PreferredPositionId;
                            else
                                cObj["defaultValue"] = ctx.PosIds.FirstOrDefault();
                        }

                        break;
                    case JnpfKeyConst.ROLESELECT:
                        cObj["defaultValue"] = multiple ? ctx.RoleIds : ctx.RoleIds.FirstOrDefault();
                        break;
                    case JnpfKeyConst.GROUPSELECT:
                        cObj["defaultValue"] = multiple ? ctx.GroupIds : ctx.GroupIds.FirstOrDefault();
                        break;
                }
            }

            child["__config__"] = cObj;
        }

        obj["children"] = cList;
    }
}
