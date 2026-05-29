import { website } from '@/config.js'
import { loadScript } from '@/utils/utils'
import * as mqtt from 'mqtt/dist/mqtt.min';
export default (Vue) => {
	Vue.config.globalProperties.$libCreate = true;
	Vue.config.globalProperties.$website = website;
	window.mqtt = mqtt;
	window.$loadScript = loadScript;
}