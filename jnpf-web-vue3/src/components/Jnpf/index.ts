import { Input, DatePicker } from 'ant-design-vue';
import { defineAsyncComponent } from 'vue';

// jnpf 组件
import { BasicCaption } from '/@/components/Basic';

// 性能优化（2026-08-09）：本桶被 FormItem/BasicForm 等入口模块引用。
// 所有 Jnpf 表单控件改为懒加载，避免把 80 个控件及其依赖静态拖进每个页面。
const lazy = (loader: () => Promise<Record<string, any>>, name: string) => defineAsyncComponent(() => loader().then(m => m[name]));

const JnpfAlert = lazy(() => import('/@/components/Jnpf/Alert'), 'JnpfAlert');
const JnpfAreaSelect = lazy(() => import('/@/components/Jnpf/AreaSelect'), 'JnpfAreaSelect');
const JnpfAutoComplete = lazy(() => import('/@/components/Jnpf/AutoComplete'), 'JnpfAutoComplete');
const JnpfButton = lazy(() => import('/@/components/Jnpf/Button'), 'JnpfButton');
const JnpfCron = lazy(() => import('/@/components/Jnpf/Cron'), 'JnpfCron');
const JnpfCascader = lazy(() => import('/@/components/Jnpf/Cascader'), 'JnpfCascader');
const JnpfColorPicker = lazy(() => import('/@/components/Jnpf/ColorPicker'), 'JnpfColorPicker');
const JnpfCheckbox = lazy(() => import('/@/components/Jnpf/Checkbox'), 'JnpfCheckbox');
const JnpfCheckboxSingle = lazy(() => import('/@/components/Jnpf/Checkbox'), 'JnpfCheckboxSingle');
const JnpfDatePicker = lazy(() => import('/@/components/Jnpf/DatePicker'), 'JnpfDatePicker');
const JnpfDateRange = lazy(() => import('/@/components/Jnpf/DatePicker'), 'JnpfDateRange');
const JnpfTimePicker = lazy(() => import('/@/components/Jnpf/DatePicker'), 'JnpfTimePicker');
const JnpfTimeRange = lazy(() => import('/@/components/Jnpf/DatePicker'), 'JnpfTimeRange');
const JnpfDivider = lazy(() => import('/@/components/Jnpf/Divider'), 'JnpfDivider');
const JnpfIconPicker = lazy(() => import('/@/components/Jnpf/IconPicker'), 'JnpfIconPicker');
const JnpfInput = lazy(() => import('/@/components/Jnpf/Input'), 'JnpfInput');
const JnpfTextarea = lazy(() => import('/@/components/Jnpf/Input'), 'JnpfTextarea');
const JnpfInputNumber = lazy(() => import('/@/components/Jnpf/InputNumber'), 'JnpfInputNumber');
const JnpfLink = lazy(() => import('/@/components/Jnpf/Link'), 'JnpfLink');
const JnpfOpenData = lazy(() => import('/@/components/Jnpf/OpenData'), 'JnpfOpenData');
const JnpfOrganizeSelect = lazy(() => import('/@/components/Jnpf/Organize'), 'JnpfOrganizeSelect');
const JnpfDepSelect = lazy(() => import('/@/components/Jnpf/Organize'), 'JnpfDepSelect');
const JnpfPosSelect = lazy(() => import('/@/components/Jnpf/Organize'), 'JnpfPosSelect');
const JnpfGroupSelect = lazy(() => import('/@/components/Jnpf/Organize'), 'JnpfGroupSelect');
const JnpfRoleSelect = lazy(() => import('/@/components/Jnpf/Organize'), 'JnpfRoleSelect');
const JnpfUserSelect = lazy(() => import('/@/components/Jnpf/Organize'), 'JnpfUserSelect');
const JnpfUsersSelect = lazy(() => import('/@/components/Jnpf/Organize'), 'JnpfUsersSelect');
const JnpfQrcode = lazy(() => import('/@/components/Jnpf/Qrcode'), 'JnpfQrcode');
const JnpfBarcode = lazy(() => import('/@/components/Jnpf/Barcode'), 'JnpfBarcode');
const JnpfRadio = lazy(() => import('/@/components/Jnpf/Radio'), 'JnpfRadio');
const JnpfRate = lazy(() => import('/@/components/Jnpf/Rate'), 'JnpfRate');
const JnpfSelect = lazy(() => import('/@/components/Jnpf/Select'), 'JnpfSelect');
const JnpfSlider = lazy(() => import('/@/components/Jnpf/Slider'), 'JnpfSlider');
const JnpfSign = lazy(() => import('/@/components/Jnpf/Sign'), 'JnpfSign');
const JnpfSignature = lazy(() => import('/@/components/Jnpf/Signature'), 'JnpfSignature');
const JnpfSwitch = lazy(() => import('/@/components/Jnpf/Switch'), 'JnpfSwitch');
const JnpfText = lazy(() => import('/@/components/Jnpf/Text'), 'JnpfText');
const JnpfTreeSelect = lazy(() => import('/@/components/Jnpf/TreeSelect'), 'JnpfTreeSelect');
const JnpfUploadFile = lazy(() => import('/@/components/Jnpf/Upload'), 'JnpfUploadFile');
const JnpfUploadImg = lazy(() => import('/@/components/Jnpf/Upload'), 'JnpfUploadImg');
const JnpfUploadImgSingle = lazy(() => import('/@/components/Jnpf/Upload'), 'JnpfUploadImgSingle');
const JnpfRelationForm = lazy(() => import('/@/components/Jnpf/RelationForm'), 'JnpfRelationForm');
const JnpfRelationFormAttr = lazy(() => import('/@/components/Jnpf/RelationFormAttr'), 'JnpfRelationFormAttr');
const JnpfPopupSelect = lazy(() => import('/@/components/Jnpf/PopupSelect'), 'JnpfPopupSelect');
const JnpfPopupTableSelect = lazy(() => import('/@/components/Jnpf/PopupSelect'), 'JnpfPopupTableSelect');
const JnpfPopupAttr = lazy(() => import('/@/components/Jnpf/PopupAttr'), 'JnpfPopupAttr');
const JnpfNumberRange = lazy(() => import('/@/components/Jnpf/NumberRange'), 'JnpfNumberRange');
const JnpfCalculate = lazy(() => import('/@/components/Jnpf/Calculate'), 'JnpfCalculate');
const JnpfInputTable = lazy(() => import('/@/components/Jnpf/InputTable'), 'JnpfInputTable');
const JnpfLocation = lazy(() => import('/@/components/Jnpf/Location'), 'JnpfLocation');
const JnpfIframe = lazy(() => import('/@/components/Jnpf/Iframe'), 'JnpfIframe');

const JnpfInputPassword = Input.Password;
JnpfInputPassword.name = 'JnpfInputPassword';
const JnpfInputGroup = Input.Group;
JnpfInputGroup.name = 'JnpfInputGroup';
const JnpfInputSearch = Input.Search;
JnpfInputSearch.name = 'JnpfInputSearch';
// 懒加载：富文本编辑器不进入口静态链
const JnpfEditor = defineAsyncComponent(() => import('/@/components/Tinymce/index').then(m => m.Tinymce));
JnpfEditor.name = 'JnpfEditor';
const JnpfGroupTitle = BasicCaption;
JnpfGroupTitle.name = 'JnpfGroupTitle';
const JnpfMonthPicker = DatePicker.MonthPicker;
JnpfMonthPicker.name = 'JnpfMonthPicker';
const JnpfWeekPicker = DatePicker.WeekPicker;
JnpfWeekPicker.name = 'JnpfWeekPicker';

export {
  JnpfAlert,
  JnpfAreaSelect,
  JnpfAutoComplete,
  JnpfButton,
  JnpfCron,
  JnpfCascader,
  JnpfColorPicker,
  JnpfCheckbox,
  JnpfCheckboxSingle,
  JnpfDatePicker,
  JnpfDateRange,
  JnpfTimePicker,
  JnpfTimeRange,
  JnpfMonthPicker,
  JnpfWeekPicker,
  JnpfDivider,
  JnpfEditor,
  JnpfGroupTitle,
  JnpfIconPicker,
  JnpfInput,
  JnpfInputPassword,
  JnpfInputGroup,
  JnpfInputSearch,
  JnpfTextarea,
  JnpfInputNumber,
  JnpfLink,
  JnpfOpenData,
  JnpfOrganizeSelect,
  JnpfDepSelect,
  JnpfPosSelect,
  JnpfGroupSelect,
  JnpfRoleSelect,
  JnpfUserSelect,
  JnpfUsersSelect,
  JnpfQrcode,
  JnpfBarcode,
  JnpfRadio,
  JnpfRate,
  JnpfSelect,
  JnpfSlider,
  JnpfSign,
  JnpfSignature,
  JnpfSwitch,
  JnpfText,
  JnpfTreeSelect,
  JnpfUploadFile,
  JnpfUploadImg,
  JnpfUploadImgSingle,
  JnpfRelationForm,
  JnpfRelationFormAttr,
  JnpfPopupSelect,
  JnpfPopupTableSelect,
  JnpfPopupAttr,
  JnpfNumberRange,
  JnpfCalculate,
  JnpfInputTable,
  JnpfLocation,
  JnpfIframe,
};
