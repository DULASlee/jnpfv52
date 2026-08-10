using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Transfer;

/// <summary>
/// Compatibility rules for transferring values between flow-form field types.
/// Extracted from RunService.DataTransferVerify — behavior-preserving; split by field family for CC.
/// </summary>
public static class FlowFormDataTransferRules
{
    public static bool CanTransfer(FieldsModel oldModel, FieldsModel newModel)
    {
        var key = oldModel.__config__.jnpfKey;
        return key switch
        {
            JnpfKeyConst.COMINPUT or JnpfKeyConst.TEXTAREA or JnpfKeyConst.RADIO or JnpfKeyConst.EDITOR
                => AcceptsTextLike(newModel),
            JnpfKeyConst.CHECKBOX
                => AcceptsFromCheckbox(newModel),
            JnpfKeyConst.NUMINPUT or JnpfKeyConst.DATE or JnpfKeyConst.TIME or JnpfKeyConst.UPLOADFZ
                or JnpfKeyConst.UPLOADIMG or JnpfKeyConst.COLORPICKER or JnpfKeyConst.RATE or JnpfKeyConst.SLIDER
                => SameExactKey(oldModel, newModel),
            JnpfKeyConst.COMSELECT or JnpfKeyConst.DEPSELECT or JnpfKeyConst.POSSELECT or JnpfKeyConst.USERSELECT
                or JnpfKeyConst.ROLESELECT or JnpfKeyConst.GROUPSELECT or JnpfKeyConst.ADDRESS
                => SameKeyAndMultiple(oldModel, newModel),
            JnpfKeyConst.TREESELECT
                => oldModel.multiple ? AcceptsFromTreeOrPopupMulti(newModel) : AcceptsTreeSingleTarget(newModel),
            JnpfKeyConst.POPUPTABLESELECT
                => oldModel.multiple ? AcceptsFromTreeOrPopupMulti(newModel) : AcceptsPopupSingleTarget(newModel),
            JnpfKeyConst.POPUPSELECT or JnpfKeyConst.RELATIONFORM
                => AcceptsRelationLike(newModel),
            _ => true,
        };
    }

    private static bool AcceptsTextLike(FieldsModel neu)
        => neu.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.TEXTAREA)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.RADIO)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && !neu.multiple)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.EDITOR);

    /// <summary>Legacy CHECKBOX branch — includes CHECKBOX itself.</summary>
    private static bool AcceptsFromCheckbox(FieldsModel neu)
        => (neu.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && neu.multiple)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && neu.multiple)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && neu.multiple)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.CHECKBOX)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER);

    /// <summary>
    /// Legacy TREESELECT/POPUPTABLESELECT multiple branches — same set as checkbox targets
    /// but WITHOUT CHECKBOX (legacy did not allow tree/popup multi → checkbox).
    /// </summary>
    private static bool AcceptsFromTreeOrPopupMulti(FieldsModel neu)
        => (neu.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && neu.multiple)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && neu.multiple)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && neu.multiple)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER);

    private static bool SameExactKey(FieldsModel oldModel, FieldsModel newModel)
        => oldModel.__config__.jnpfKey.Equals(newModel.__config__.jnpfKey);

    private static bool SameKeyAndMultiple(FieldsModel oldModel, FieldsModel newModel)
        => oldModel.__config__.jnpfKey.Equals(newModel.__config__.jnpfKey)
           && oldModel.multiple.Equals(newModel.multiple);

    private static bool AcceptsTreeSingleTarget(FieldsModel neu)
        => neu.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.TEXTAREA)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.RADIO)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && !neu.multiple)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && !neu.multiple)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.EDITOR);

    private static bool AcceptsPopupSingleTarget(FieldsModel neu)
        => (neu.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && !neu.multiple)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT);

    private static bool AcceptsRelationLike(FieldsModel neu)
        => neu.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM)
           || neu.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT)
           || (neu.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && !neu.multiple);
}
