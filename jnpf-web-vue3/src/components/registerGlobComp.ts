import type { App } from 'vue';
import { defineAsyncComponent } from 'vue';
import { Button } from './Button';
import {
  Input,
  InputNumber,
  Layout,
  Form,
  Switch,
  Dropdown,
  Menu,
  Select,
  Table,
  Checkbox,
  Tabs,
  Collapse,
  Card,
  Tooltip,
  Row,
  Col,
  Popconfirm,
  Divider,
  Alert,
  AutoComplete,
  Cascader,
  Rate,
  Slider,
  Avatar,
  Tag,
  Space,
  Steps,
  Popover,
  Radio,
  Progress,
  Image,
  Upload,
  Pagination,
  Spin,
  Modal,
} from 'ant-design-vue';

import { BasicHelp, BasicCaption } from '/@/components/Basic';

// 性能优化（2026-08-09）：Jnpf 表单控件全部懒注册。
// 之前 registerGlobComp 静态 import 80 个控件 → 每个页面（无论是否使用表单）
// 都要拉取全部控件模块（dev 下 638 个模块请求之一大块）。
// 懒注册后，组件模块在模板首次渲染 <jnpf-xxx> 时才动态加载。
type ComponentLoader = () => Promise<Record<string, any>>;
function lazyComponent(loader: ComponentLoader, ...names: string[]) {
  return {
    install: (app: App) => {
      for (const name of names) {
        app.component(
          name,
          defineAsyncComponent(() => loader().then(m => m[name])),
        );
      }
    },
  };
}

const JnpfAlert = lazyComponent(() => import('/@/components/Jnpf/Alert'), 'JnpfAlert');
const JnpfAreaSelect = lazyComponent(() => import('/@/components/Jnpf/AreaSelect'), 'JnpfAreaSelect');
const JnpfAutoComplete = lazyComponent(() => import('/@/components/Jnpf/AutoComplete'), 'JnpfAutoComplete');
const JnpfButton = lazyComponent(() => import('/@/components/Jnpf/Button'), 'JnpfButton');
const JnpfCron = lazyComponent(() => import('/@/components/Jnpf/Cron'), 'JnpfCron');
const JnpfCascader = lazyComponent(() => import('/@/components/Jnpf/Cascader'), 'JnpfCascader');
const JnpfCheckbox = lazyComponent(() => import('/@/components/Jnpf/Checkbox'), 'JnpfCheckbox', 'JnpfCheckboxSingle');
const JnpfColorPicker = lazyComponent(() => import('/@/components/Jnpf/ColorPicker'), 'JnpfColorPicker');
const JnpfDatePicker = lazyComponent(() => import('/@/components/Jnpf/DatePicker'), 'JnpfDatePicker', 'JnpfDateRange', 'JnpfTimePicker', 'JnpfTimeRange');
const JnpfDivider = lazyComponent(() => import('/@/components/Jnpf/Divider'), 'JnpfDivider');
const JnpfEmpty = lazyComponent(() => import('/@/components/Jnpf/Empty'), 'JnpfEmpty');
const JnpfIconPicker = lazyComponent(() => import('/@/components/Jnpf/IconPicker'), 'JnpfIconPicker');
const JnpfInput = lazyComponent(() => import('/@/components/Jnpf/Input'), 'JnpfInput', 'JnpfTextarea');
const JnpfInputNumber = lazyComponent(() => import('/@/components/Jnpf/InputNumber'), 'JnpfInputNumber');
const JnpfLink = lazyComponent(() => import('/@/components/Jnpf/Link'), 'JnpfLink');
const JnpfOpenData = lazyComponent(() => import('/@/components/Jnpf/OpenData'), 'JnpfOpenData');
const JnpfOrganizeSelect = lazyComponent(
  () => import('/@/components/Jnpf/Organize'),
  'JnpfOrganizeSelect',
  'JnpfDepSelect',
  'JnpfPosSelect',
  'JnpfGroupSelect',
  'JnpfRoleSelect',
  'JnpfUserSelect',
  'JnpfUsersSelect',
);
const JnpfQrcode = lazyComponent(() => import('/@/components/Jnpf/Qrcode'), 'JnpfQrcode');
const JnpfBarcode = lazyComponent(() => import('/@/components/Jnpf/Barcode'), 'JnpfBarcode');
const JnpfRadio = lazyComponent(() => import('/@/components/Jnpf/Radio'), 'JnpfRadio');
const JnpfSelect = lazyComponent(() => import('/@/components/Jnpf/Select'), 'JnpfSelect');
const JnpfRate = lazyComponent(() => import('/@/components/Jnpf/Rate'), 'JnpfRate');
const JnpfSlider = lazyComponent(() => import('/@/components/Jnpf/Slider'), 'JnpfSlider');
const JnpfSign = lazyComponent(() => import('/@/components/Jnpf/Sign'), 'JnpfSign');
const JnpfSignature = lazyComponent(() => import('/@/components/Jnpf/Signature'), 'JnpfSignature');
const JnpfSwitch = lazyComponent(() => import('/@/components/Jnpf/Switch'), 'JnpfSwitch');
const JnpfText = lazyComponent(() => import('/@/components/Jnpf/Text'), 'JnpfText');
const JnpfTreeSelect = lazyComponent(() => import('/@/components/Jnpf/TreeSelect'), 'JnpfTreeSelect');
const JnpfUpload = lazyComponent(() => import('/@/components/Jnpf/Upload'), 'JnpfUploadFile', 'JnpfUploadImg', 'JnpfUploadImgSingle', 'JnpfUploadBtn');
const JnpfNumberRange = lazyComponent(() => import('/@/components/Jnpf/NumberRange'), 'JnpfNumberRange');
const JnpfRelationFormAttr = lazyComponent(() => import('/@/components/Jnpf/RelationFormAttr'), 'JnpfRelationFormAttr');
const JnpfPopupSelect = lazyComponent(() => import('/@/components/Jnpf/PopupSelect'), 'JnpfPopupSelect', 'JnpfPopupTableSelect');
const JnpfPopupAttr = lazyComponent(() => import('/@/components/Jnpf/PopupAttr'), 'JnpfPopupAttr');
const JnpfCalculate = lazyComponent(() => import('/@/components/Jnpf/Calculate'), 'JnpfCalculate');
const JnpfLocation = lazyComponent(() => import('/@/components/Jnpf/Location'), 'JnpfLocation');
const JnpfIframe = lazyComponent(() => import('/@/components/Jnpf/Iframe'), 'JnpfIframe');

// 重型组件懒加载：富文本编辑器不进首屏
const JnpfEditorAsync = defineAsyncComponent(() => import('/@/components/Tinymce/index').then(m => m.Tinymce));
const JnpfEditor = { install: (app: App) => app.component('JnpfEditor', JnpfEditorAsync) };
const JnpfGroupTitle = BasicCaption;
JnpfGroupTitle.name = 'JnpfGroupTitle';

export function registerGlobComp(app: App) {
  app
    .use(Input)
    .use(InputNumber)
    .use(Button)
    .use(Layout)
    .use(Form)
    .use(Switch)
    .use(Dropdown)
    .use(Menu)
    .use(Select)
    .use(Table)
    .use(Checkbox)
    .use(Tabs)
    .use(Card)
    .use(Collapse)
    .use(Tooltip)
    .use(Row)
    .use(Col)
    .use(Popconfirm)
    .use(Popover)
    .use(Divider)
    .use(Slider)
    .use(Rate)
    .use(Alert)
    .use(AutoComplete)
    .use(Cascader)
    .use(Avatar)
    .use(Tag)
    .use(Space)
    .use(Steps)
    .use(Radio)
    .use(Progress)
    .use(Image)
    .use(Upload)
    .use(Pagination)
    .use(Spin)
    .use(Modal)
    .use(BasicHelp)
    .use(JnpfAlert)
    .use(JnpfRate)
    .use(JnpfSlider)
    .use(JnpfAreaSelect)
    .use(JnpfAutoComplete)
    .use(JnpfButton)
    .use(JnpfCron)
    .use(JnpfCascader)
    .use(JnpfCheckbox)
    .use(JnpfColorPicker)
    .use(JnpfDatePicker)
    .use(JnpfDivider)
    .use(JnpfEmpty)
    .use(JnpfGroupTitle)
    .use(JnpfIconPicker)
    .use(JnpfInput)
    .use(JnpfInputNumber)
    .use(JnpfLink)
    .use(JnpfOrganizeSelect)
    .use(JnpfOpenData)
    .use(JnpfQrcode)
    .use(JnpfBarcode)
    .use(JnpfRadio)
    .use(JnpfSelect)
    .use(JnpfSign)
    .use(JnpfSignature)
    .use(JnpfSwitch)
    .use(JnpfText)
    .use(JnpfTreeSelect)
    .use(JnpfEditor)
    .use(JnpfRelationFormAttr)
    .use(JnpfPopupSelect)
    .use(JnpfPopupAttr)
    .use(JnpfNumberRange)
    .use(JnpfCalculate)
    .use(JnpfUpload)
    .use(JnpfLocation)
    .use(JnpfIframe);
}
