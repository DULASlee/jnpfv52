using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine.Core;

/// <summary>
/// Filters form fields for short-link display — extracted from FormDataParsing.GetKeyData.
/// </summary>
public static class ShortLinkFormFieldFilter
{
    public static List<FieldsModel> Apply(IEnumerable<FieldsModel> formData)
    {
        return formData
            .Where(x => x.__config__.jnpfKey == JnpfKeyConst.COMINPUT || x.__config__.jnpfKey == JnpfKeyConst.TEXTAREA
                || x.__config__.jnpfKey == JnpfKeyConst.NUMINPUT
                || x.__config__.jnpfKey == JnpfKeyConst.SWITCH
                || (x.__config__.jnpfKey == JnpfKeyConst.RADIO && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.CHECKBOX && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.SELECT && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.CASCADER && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.TREESELECT && x.__config__.dataType.Equals("static"))
                || x.__config__.jnpfKey == JnpfKeyConst.DATE || x.__config__.jnpfKey == JnpfKeyConst.TIME || x.__config__.jnpfKey == JnpfKeyConst.COLORPICKER
                || x.__config__.jnpfKey == JnpfKeyConst.RATE || x.__config__.jnpfKey == JnpfKeyConst.SLIDER || x.__config__.jnpfKey == JnpfKeyConst.EDITOR
                || x.__config__.jnpfKey == JnpfKeyConst.LINK || x.__config__.jnpfKey == JnpfKeyConst.JNPFTEXT || x.__config__.jnpfKey == JnpfKeyConst.ALERT
                || x.__config__.jnpfKey == JnpfKeyConst.LOCATION)
            .Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT))
            .ToList();
    }
}
