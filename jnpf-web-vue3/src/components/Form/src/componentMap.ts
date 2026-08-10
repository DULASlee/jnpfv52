import type { Component } from 'vue';
import type { ComponentType } from './types/index';
import { defineAsyncComponent } from 'vue';
import { Input, DatePicker } from 'ant-design-vue';

/**
 * Form 控件注册表。
 * 列表检索等场景只需要 Input/Select 等轻控件；
 * Tinymce / 组织选择 / 上传 / 关联表单等重组件改为异步，避免 BasicTable→BasicForm 首进拖垮 Vite。
 */

import { StrengthMeter } from '/@/components/StrengthMeter';
import { CountdownInput } from '/@/components/CountDown';
import { BasicCaption } from '/@/components/Basic';

// ── 轻量：同步（检索表单 / 常见字段）──
import { JnpfAlert } from '/@/components/Jnpf/Alert';
import { JnpfAutoComplete } from '/@/components/Jnpf/AutoComplete';
import { JnpfButton } from '/@/components/Jnpf/Button';
import { JnpfCascader } from '/@/components/Jnpf/Cascader';
import { JnpfCheckbox, JnpfCheckboxSingle } from '/@/components/Jnpf/Checkbox';
import { JnpfColorPicker } from '/@/components/Jnpf/ColorPicker';
import { JnpfDatePicker, JnpfDateRange, JnpfTimePicker, JnpfTimeRange } from '/@/components/Jnpf/DatePicker';
import { JnpfDivider } from '/@/components/Jnpf/Divider';
import { JnpfInput, JnpfTextarea } from '/@/components/Jnpf/Input';
import { JnpfInputNumber } from '/@/components/Jnpf/InputNumber';
import { JnpfLink } from '/@/components/Jnpf/Link';
import { JnpfOpenData } from '/@/components/Jnpf/OpenData';
import { JnpfRadio } from '/@/components/Jnpf/Radio';
import { JnpfSelect } from '/@/components/Jnpf/Select';
import { JnpfRate } from '/@/components/Jnpf/Rate';
import { JnpfSlider } from '/@/components/Jnpf/Slider';
import { JnpfSwitch } from '/@/components/Jnpf/Switch';
import { JnpfText } from '/@/components/Jnpf/Text';
import { JnpfTreeSelect } from '/@/components/Jnpf/TreeSelect';
import { JnpfNumberRange } from '/@/components/Jnpf/NumberRange';

const JnpfInputPassword = Input.Password;
JnpfInputPassword.name = 'JnpfInputPassword';
const JnpfInputGroup = Input.Group;
JnpfInputGroup.name = 'JnpfInputGroup';
const JnpfInputSearch = Input.Search;
JnpfInputSearch.name = 'JnpfInputSearch';
const JnpfGroupTitle = BasicCaption;
JnpfGroupTitle.name = 'JnpfGroupTitle';
const JnpfMonthPicker = DatePicker.MonthPicker;
JnpfMonthPicker.name = 'JnpfMonthPicker';
const JnpfWeekPicker = DatePicker.WeekPicker;
JnpfWeekPicker.name = 'JnpfWeekPicker';

function lazy(loader: () => Promise<any>, name?: string): Component {
  const comp = defineAsyncComponent({
    loader,
    delay: 0,
    timeout: import.meta.env.DEV ? 120_000 : 30_000,
  });
  if (name) (comp as any).name = name;
  return comp;
}

// ── 重型：异步（首进列表页不再整包编译）──
const JnpfEditor = lazy(() => import('/@/components/Tinymce/index').then(m => m.Tinymce), 'JnpfEditor');
const JnpfAreaSelect = lazy(() => import('/@/components/Jnpf/AreaSelect').then(m => m.JnpfAreaSelect));
const JnpfCron = lazy(() => import('/@/components/Jnpf/Cron').then(m => m.JnpfCron));
const JnpfIconPicker = lazy(() => import('/@/components/Jnpf/IconPicker').then(m => m.JnpfIconPicker));
const JnpfOrganizeSelect = lazy(() => import('/@/components/Jnpf/Organize').then(m => m.JnpfOrganizeSelect));
const JnpfDepSelect = lazy(() => import('/@/components/Jnpf/Organize').then(m => m.JnpfDepSelect));
const JnpfPosSelect = lazy(() => import('/@/components/Jnpf/Organize').then(m => m.JnpfPosSelect));
const JnpfGroupSelect = lazy(() => import('/@/components/Jnpf/Organize').then(m => m.JnpfGroupSelect));
const JnpfRoleSelect = lazy(() => import('/@/components/Jnpf/Organize').then(m => m.JnpfRoleSelect));
const JnpfUserSelect = lazy(() => import('/@/components/Jnpf/Organize').then(m => m.JnpfUserSelect));
const JnpfUsersSelect = lazy(() => import('/@/components/Jnpf/Organize').then(m => m.JnpfUsersSelect));
const JnpfQrcode = lazy(() => import('/@/components/Jnpf/Qrcode').then(m => m.JnpfQrcode));
const JnpfBarcode = lazy(() => import('/@/components/Jnpf/Barcode').then(m => m.JnpfBarcode));
const JnpfSign = lazy(() => import('/@/components/Jnpf/Sign').then(m => m.JnpfSign));
const JnpfSignature = lazy(() => import('/@/components/Jnpf/Signature').then(m => m.JnpfSignature));
const JnpfUploadFile = lazy(() => import('/@/components/Jnpf/Upload').then(m => m.JnpfUploadFile));
const JnpfUploadImg = lazy(() => import('/@/components/Jnpf/Upload').then(m => m.JnpfUploadImg));
const JnpfUploadImgSingle = lazy(() => import('/@/components/Jnpf/Upload').then(m => m.JnpfUploadImgSingle));
const JnpfRelationForm = lazy(() => import('/@/components/Jnpf/RelationForm').then(m => m.JnpfRelationForm));
const JnpfRelationFormAttr = lazy(() => import('/@/components/Jnpf/RelationFormAttr').then(m => m.JnpfRelationFormAttr));
const JnpfPopupSelect = lazy(() => import('/@/components/Jnpf/PopupSelect').then(m => m.JnpfPopupSelect));
const JnpfPopupTableSelect = lazy(() => import('/@/components/Jnpf/PopupSelect').then(m => m.JnpfPopupTableSelect));
const JnpfPopupAttr = lazy(() => import('/@/components/Jnpf/PopupAttr').then(m => m.JnpfPopupAttr));
const JnpfCalculate = lazy(() => import('/@/components/Jnpf/Calculate').then(m => m.JnpfCalculate));
const JnpfInputTable = lazy(() => import('/@/components/Jnpf/InputTable').then(m => m.JnpfInputTable));
const JnpfLocation = lazy(() => import('/@/components/Jnpf/Location').then(m => m.JnpfLocation));
const JnpfIframe = lazy(() => import('/@/components/Jnpf/Iframe').then(m => m.JnpfIframe));

const componentMap = new Map<ComponentType, Component>();

componentMap.set('StrengthMeter', StrengthMeter);
componentMap.set('InputCountDown', CountdownInput);

componentMap.set('InputGroup', JnpfInputGroup);
componentMap.set('InputSearch', JnpfInputSearch);
componentMap.set('MonthPicker', JnpfMonthPicker);
componentMap.set('WeekPicker', JnpfWeekPicker);

componentMap.set('Alert', JnpfAlert);
componentMap.set('AreaSelect', JnpfAreaSelect);
componentMap.set('AutoComplete', JnpfAutoComplete);
componentMap.set('Button', JnpfButton);
componentMap.set('Cron', JnpfCron);
componentMap.set('Cascader', JnpfCascader);
componentMap.set('ColorPicker', JnpfColorPicker);
componentMap.set('Checkbox', JnpfCheckbox);
componentMap.set('JnpfCheckboxSingle', JnpfCheckboxSingle);
componentMap.set('DatePicker', JnpfDatePicker);
componentMap.set('DateRange', JnpfDateRange);
componentMap.set('TimePicker', JnpfTimePicker);
componentMap.set('TimeRange', JnpfTimeRange);
componentMap.set('Divider', JnpfDivider);
componentMap.set('Editor', JnpfEditor);
componentMap.set('GroupTitle', JnpfGroupTitle);
componentMap.set('Input', JnpfInput);
componentMap.set('InputPassword', JnpfInputPassword);
componentMap.set('Textarea', JnpfTextarea);
componentMap.set('InputNumber', JnpfInputNumber);
componentMap.set('IconPicker', JnpfIconPicker);
componentMap.set('Link', JnpfLink);
componentMap.set('OrganizeSelect', JnpfOrganizeSelect);
componentMap.set('DepSelect', JnpfDepSelect);
componentMap.set('PosSelect', JnpfPosSelect);
componentMap.set('GroupSelect', JnpfGroupSelect);
componentMap.set('RoleSelect', JnpfRoleSelect);
componentMap.set('UserSelect', JnpfUserSelect);
componentMap.set('UsersSelect', JnpfUsersSelect);
componentMap.set('Qrcode', JnpfQrcode);
componentMap.set('Barcode', JnpfBarcode);
componentMap.set('Radio', JnpfRadio);
componentMap.set('Rate', JnpfRate);
componentMap.set('Select', JnpfSelect);
componentMap.set('Slider', JnpfSlider);
componentMap.set('Sign', JnpfSign);
componentMap.set('Signature', JnpfSignature);
componentMap.set('Switch', JnpfSwitch);
componentMap.set('Text', JnpfText);
componentMap.set('TreeSelect', JnpfTreeSelect);
componentMap.set('UploadFile', JnpfUploadFile);
componentMap.set('UploadImg', JnpfUploadImg);
componentMap.set('UploadImgSingle', JnpfUploadImgSingle);
componentMap.set('BillRule', JnpfInput);
componentMap.set('ModifyUser', JnpfInput);
componentMap.set('ModifyTime', JnpfInput);
componentMap.set('CreateUser', JnpfOpenData);
componentMap.set('CreateTime', JnpfOpenData);
componentMap.set('CurrOrganize', JnpfOpenData);
componentMap.set('CurrPosition', JnpfOpenData);
componentMap.set('RelationForm', JnpfRelationForm);
componentMap.set('RelationFormAttr', JnpfRelationFormAttr);
componentMap.set('PopupSelect', JnpfPopupSelect);
componentMap.set('PopupTableSelect', JnpfPopupTableSelect);
componentMap.set('PopupAttr', JnpfPopupAttr);
componentMap.set('NumberRange', JnpfNumberRange);
componentMap.set('Calculate', JnpfCalculate);
componentMap.set('InputTable', JnpfInputTable);
componentMap.set('Location', JnpfLocation);
componentMap.set('Iframe', JnpfIframe);

export function add(compName: ComponentType, component: Component) {
  componentMap.set(compName, component);
}

export function del(compName: ComponentType) {
  componentMap.delete(compName);
}

export { componentMap };
