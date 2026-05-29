using JNPF.Common.Const;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成查询控件归类帮助类.
/// </summary>
public class CodeGenQueryControlClassificationHelper
{
    /// <summary>
    /// 列表查询控件.
    /// </summary>
    /// <param name="type">1-Web设计,2-App设计,3-流程表单,4-Web表单,5-App表单.</param>
    /// <returns></returns>
    public static Dictionary<string, List<string>> ListQueryControl(int type)
    {
        Dictionary<string, List<string>> listQueryControl = new Dictionary<string, List<string>>();
        switch (type)
        {
            case 4:
                {
                    var useInputList = new List<string>();
                    useInputList.Add(JnpfKeyConst.COMINPUT);
                    useInputList.Add(JnpfKeyConst.LOCATION);
                    useInputList.Add(JnpfKeyConst.TEXTAREA);
                    useInputList.Add(JnpfKeyConst.JNPFTEXT);
                    useInputList.Add(JnpfKeyConst.BILLRULE);
                    listQueryControl["inputList"] = useInputList;

                    var useDateList = new List<string>();
                    useDateList.Add(JnpfKeyConst.CREATETIME);
                    useDateList.Add(JnpfKeyConst.MODIFYTIME);
                    listQueryControl["dateList"] = useDateList;

                    var useSelectList = new List<string>();
                    useSelectList.Add(JnpfKeyConst.SELECT);
                    useSelectList.Add(JnpfKeyConst.RADIO);
                    useSelectList.Add("checkbox");
                    listQueryControl["selectList"] = useSelectList;

                    var timePickerList = new List<string>();
                    timePickerList.Add(JnpfKeyConst.TIME);
                    listQueryControl["timePickerList"] = timePickerList;

                    var numRangeList = new List<string>();
                    numRangeList.Add(JnpfKeyConst.NUMINPUT);
                    numRangeList.Add(JnpfKeyConst.CALCULATE);
                    numRangeList.Add(JnpfKeyConst.RATE);
                    numRangeList.Add(JnpfKeyConst.SLIDER);
                    listQueryControl["numRangeList"] = numRangeList;

                    var datePickerList = new List<string>();
                    datePickerList.Add(JnpfKeyConst.DATE);
                    listQueryControl["datePickerList"] = datePickerList;

                    var userSelectList = new List<string>();
                    userSelectList.Add(JnpfKeyConst.CREATEUSER);
                    userSelectList.Add(JnpfKeyConst.MODIFYUSER);
                    userSelectList.Add(JnpfKeyConst.USERSELECT);
                    listQueryControl["userSelectList"] = userSelectList;

                    var usersSelectList = new List<string>();
                    usersSelectList.Add(JnpfKeyConst.USERSSELECT);
                    listQueryControl["usersSelectList"] = usersSelectList;

                    var comSelectList = new List<string>();
                    comSelectList.Add(JnpfKeyConst.COMSELECT);
                    comSelectList.Add(JnpfKeyConst.CURRORGANIZE);
                    listQueryControl["comSelectList"] = comSelectList;

                    var depSelectList = new List<string>();
                    depSelectList.Add(JnpfKeyConst.CURRDEPT);
                    depSelectList.Add(JnpfKeyConst.DEPSELECT);
                    listQueryControl["depSelectList"] = depSelectList;

                    var posSelectList = new List<string>();
                    posSelectList.Add(JnpfKeyConst.CURRPOSITION);
                    posSelectList.Add(JnpfKeyConst.POSSELECT);
                    listQueryControl["posSelectList"] = posSelectList;

                    var useCascaderList = new List<string>();
                    useCascaderList.Add(JnpfKeyConst.CASCADER);
                    listQueryControl["useCascaderList"] = useCascaderList;

                    var jNPFAddressList = new List<string>();
                    jNPFAddressList.Add(JnpfKeyConst.ADDRESS);
                    listQueryControl["JNPFAddressList"] = jNPFAddressList;

                    var treeSelectList = new List<string>();
                    treeSelectList.Add(JnpfKeyConst.TREESELECT);
                    listQueryControl["treeSelectList"] = treeSelectList;

                    var autoCompleteList = new List<string>();
                    autoCompleteList.Add(JnpfKeyConst.AUTOCOMPLETE);
                    listQueryControl["autoCompleteList"] = autoCompleteList;
                }

                break;
            case 5:
                {
                    var inputList = new List<string>();
                    inputList.Add(JnpfKeyConst.COMINPUT);
                    inputList.Add(JnpfKeyConst.LOCATION);
                    inputList.Add(JnpfKeyConst.TEXTAREA);
                    inputList.Add(JnpfKeyConst.JNPFTEXT);
                    inputList.Add(JnpfKeyConst.BILLRULE);
                    inputList.Add(JnpfKeyConst.CALCULATE);
                    listQueryControl["input"] = inputList;

                    var numRangeList = new List<string>();
                    numRangeList.Add(JnpfKeyConst.NUMINPUT);
                    numRangeList.Add(JnpfKeyConst.RATE);
                    numRangeList.Add(JnpfKeyConst.SLIDER);
                    listQueryControl["numRange"] = numRangeList;

                    var switchList = new List<string>();
                    switchList.Add(JnpfKeyConst.SWITCH);
                    listQueryControl["switch"] = switchList;

                    var selectList = new List<string>();
                    selectList.Add(JnpfKeyConst.RADIO);
                    selectList.Add(JnpfKeyConst.CHECKBOX);
                    selectList.Add(JnpfKeyConst.SELECT);
                    listQueryControl["select"] = selectList;

                    var cascaderList = new List<string>();
                    cascaderList.Add(JnpfKeyConst.CASCADER);
                    listQueryControl["cascader"] = cascaderList;

                    var timeList = new List<string>();
                    timeList.Add(JnpfKeyConst.TIME);
                    listQueryControl["timePicker"] = timeList;

                    var dateList = new List<string>();
                    dateList.Add(JnpfKeyConst.DATE);
                    dateList.Add(JnpfKeyConst.CREATETIME);
                    dateList.Add(JnpfKeyConst.MODIFYTIME);
                    listQueryControl["datePicker"] = dateList;

                    var comSelectList = new List<string>();
                    comSelectList.Add(JnpfKeyConst.COMSELECT);
                    comSelectList.Add(JnpfKeyConst.CURRORGANIZE);
                    listQueryControl["organizeSelect"] = comSelectList;

                    var depSelectList = new List<string>();
                    depSelectList.Add(JnpfKeyConst.DEPSELECT);
                    depSelectList.Add(JnpfKeyConst.CURRDEPT);
                    listQueryControl["depSelect"] = depSelectList;

                    var posSelectList = new List<string>();
                    posSelectList.Add(JnpfKeyConst.POSSELECT);
                    posSelectList.Add(JnpfKeyConst.CURRPOSITION);
                    listQueryControl["posSelect"] = posSelectList;

                    var userSelectList = new List<string>();
                    userSelectList.Add(JnpfKeyConst.USERSELECT);
                    userSelectList.Add(JnpfKeyConst.CREATEUSER);
                    userSelectList.Add(JnpfKeyConst.MODIFYUSER);
                    listQueryControl["userSelect"] = userSelectList;

                    var usersSelectList = new List<string>();
                    usersSelectList.Add(JnpfKeyConst.USERSSELECT);
                    listQueryControl["usersSelect"] = usersSelectList;

                    var treeSelectList = new List<string>();
                    treeSelectList.Add(JnpfKeyConst.TREESELECT);
                    listQueryControl["treeSelect"] = treeSelectList;

                    var addressList = new List<string>();
                    addressList.Add(JnpfKeyConst.ADDRESS);
                    listQueryControl["areaSelect"] = addressList;

                    listQueryControl["autoComplete"] = new List<string> { JnpfKeyConst.AUTOCOMPLETE };

                    listQueryControl["groupSelect"] = new List<string>() { JnpfKeyConst.GROUPSELECT };

                    listQueryControl["roleSelect"] = new List<string>() { JnpfKeyConst.ROLESELECT };
                }

                break;
        }

        return listQueryControl;
    }
}