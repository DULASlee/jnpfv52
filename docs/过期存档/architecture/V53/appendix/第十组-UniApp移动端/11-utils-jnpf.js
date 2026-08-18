import CryptoJS from 'crypto-js'
import request from '@/utils/request'
import define from '@/utils/define'

const jnpf = {
	goBack() {
		uni.navigateBack()
	},
	// 跳链接
	jumpLink(urlAddress, pageTitle = "") {
		if (!urlAddress) return
		let url = '/pages/apply/externalLink/index?url=' + encodeURIComponent(urlAddress) + '&fullName= ' +
			pageTitle
		uni.navigateTo({
			url: url,
		})
	},
	/* 传参字符串转义 */
	encodeContent(key) {
		const encodeArr = [{
			code: '%',
			encode: '%25'
		}, {
			code: '?',
			encode: '%3F'
		}, {
			code: '#',
			encode: '%23'
		}, {
			code: '&',
			encode: '%26'
		}, {
			code: '=',
			encode: '%3D'
		}];
		return key.replace(/[%?#&=]/g, ($, index, str) => {
			for (const k of encodeArr) {
				if (k.code === $) {
					return k.encode;
				}
			}
		});
	},
	solveAddressParam(item, config) {
		let jnpfKey = config.jnpfKey
		item.urlAddress = item.urlAddress ? item.urlAddress : config.option.appUrlAddress
		if (!item.urlAddress) return
		let urlAddress = item.urlAddress
		const regex = /\${[^{}]+}/g;
		urlAddress = urlAddress.replace(regex, match => {
			const key = match.slice(2, -1);
			let value = '';
			if (jnpfKey === 'text') {
				return key == 'Field' || key == 'field' ? item.value : match
			}
			if (jnpfKey === 'timeAxis') {
				if (key == 'name') value = item['content'];
				if (key == 'timestamp') value = item['timestamp'];
			}
			if (jnpfKey === 'tableList') {
				return item[key] || match
			}
			if (jnpfKey === 'dataBoard') {
				return key == 'Field' || key == 'field' ? item.num : match
			}
			if (jnpfKey === 'rankList') {
				if (key === 'name') value = item.label;
				if (key === 'value') value = item.value.replace("¥ ", "");
			}
			// 图标替换
			if (jnpfKey === 'barChart' || jnpfKey === 'lineChart' || jnpfKey === 'pieChart' ||
				jnpfKey == 'radarChart' || jnpfKey === 'mapChart') {
				if (key === 'name') value = item.name;
				if (key === 'long') value = item.long;
				if (key === 'lat') value = item.lat;
				if (key === 'value') value = item.value;
				if (key === 'type') value = item.type;
			}
			return value || match;
		});
		item.urlAddress = urlAddress
	},
	handelFormat(format) {
		let formatObj = {
			'yyyy': 'yyyy',
			'yyyy-MM': 'yyyy-MM',
			'yyyy-MM-dd': 'yyyy-MM-dd',
			'yyyy-MM-dd HH:mm': 'yyyy-MM-dd HH:mm',
			'yyyy-MM-dd HH:mm:ss': 'yyyy-MM-dd HH:mm:ss',
			'HH:mm:ss': 'HH:mm:ss',
			"HH:mm": "HH:mm",
			'YYYY': 'yyyy',
			'YYYY-MM': 'yyyy-MM',
			'YYYY-MM-DD': 'yyyy-MM-dd',
			'YYYY-MM-DD HH:mm': 'yyyy-MM-dd HH:mm',
			'YYYY-MM-DD HH:mm:ss': 'yyyy-MM-dd HH:mm:ss',
		}
		return formatObj[format]
	},
	toDate(v, format) {
		format = format ? format : "yyyy-MM-dd HH:mm:ss"
		if (!v) return "";
		var d = v;
		if (typeof v === 'string') {
			if (v.indexOf("/Date(") > -1)
				d = new Date(parseInt(v.replace("/Date(", "").replace(")/", ""), 10));
			else
				d = new Date(Date.parse(v.replace(/-/g, "/").replace("T", " ").split(".")[0]));
		} else {
			d = new Date(v)
		}
		var o = {
			"M+": d.getMonth() + 1,
			"d+": d.getDate(),
			"h+": d.getHours(),
			"H+": d.getHours(),
			"m+": d.getMinutes(),
			"s+": d.getSeconds(),
			"q+": Math.floor((d.getMonth() + 3) / 3),
			"S": d.getMilliseconds()
		};
		if (/(y+)/.test(format)) {
			format = format.replace(RegExp.$1, (d.getFullYear() + "").substr(4 - RegExp.$1.length));
		}
		for (var k in o) {
			if (new RegExp("(" + k + ")").test(format)) {
				format = format.replace(RegExp.$1, RegExp.$1.length == 1 ? o[k] : ("00" + o[k]).substr(("" +
						o[k])
					.length));
			}
		}
		return format;
	},
	toFileSize(size) {
		if (size == null || size == "") {
			return "";
		}
		if (size < 1024.00)
			return jnpf.toDecimal(size) + " 字节";
		else if (size >= 1024.00 && size < 1048576)
			return jnpf.toDecimal(size / 1024.00) + " KB";
		else if (size >= 1048576 && size < 1073741824)
			return jnpf.toDecimal(size / 1024.00 / 1024.00) + " MB";
		else if (size >= 1073741824)
			return jnpf.toDecimal(size / 1024.00 / 1024.00 / 1024.00) + " GB";
	},
	toDecimal(num) {
		if (num == null) {
			num = "0";
		}
		num = num.toString().replace(/\$|\,/g, '');
		if (isNaN(num))
			num = "0";
		var sign = (num == (num = Math.abs(num)));
		num = Math.floor(num * 100 + 0.50000000001);
		var cents = num % 100;
		num = Math.floor(num / 100).toString();
		if (cents < 10)
			cents = "0" + cents;
		for (var i = 0; i < Math.floor((num.length - (1 + i)) / 3); i++)
			num = num.substring(0, num.length - (4 * i + 3)) + '' +
			num.substring(num.length - (4 * i + 3));
		return (((sign) ? '' : '-') + num + '.' + cents);
	},
	getScriptFunc(str) {
		// #ifndef MP
		let func = null
		try {
			func = eval(str)
			if (Object.prototype.toString.call(func) !== '[object Function]') return false;
			return func
		} catch (error) {
			console.log(error);
			return false
		}
		// #endif
		// #ifdef MP
		return false
		// #endif
	},
	interfaceDataHandler(data) {
		if (!data.dataProcessing) return data.data
		const dataHandler = this.getScriptFunc(data.dataProcessing)
		if (!dataHandler) return data.data
		return dataHandler(data.data)
	},
	toDateText(dateTimeStamp) {
		if (!dateTimeStamp) return ''
		let result = ''
		let minute = 1000 * 60; //把分，时，天，周，半个月，一个月用毫秒表示
		let hour = minute * 60;
		let day = hour * 24;
		let week = day * 7;
		let halfamonth = day * 15;
		let month = day * 30;
		let now = new Date().getTime(); //获取当前时间毫秒
		let diffValue = now - dateTimeStamp; //时间差
		if (diffValue < 0) return "刚刚"
		let minC = diffValue / minute; //计算时间差的分，时，天，周，月
		let hourC = diffValue / hour;
		let dayC = diffValue / day;
		let weekC = diffValue / week;
		let monthC = diffValue / month;
		if (monthC >= 1 && monthC <= 3) {
			result = " " + parseInt(monthC) + "月前"
		} else if (weekC >= 1 && weekC <= 3) {
			result = " " + parseInt(weekC) + "周前"
		} else if (dayC >= 1 && dayC <= 6) {
			result = " " + parseInt(dayC) + "天前"
		} else if (hourC >= 1 && hourC <= 23) {
			result = " " + parseInt(hourC) + "小时前"
		} else if (minC >= 1 && minC <= 59) {
			result = " " + parseInt(minC) + "分钟前"
		} else if (diffValue >= 0 && diffValue <= minute) {
			result = "刚刚"
		} else {
			let datetime = new Date();
			datetime.setTime(dateTimeStamp);
			let Nyear = datetime.getFullYear();
			let Nmonth = datetime.getMonth() + 1 < 10 ? "0" + (datetime.getMonth() + 1) : datetime
				.getMonth() + 1;
			let Ndate = datetime.getDate() < 10 ? "0" + datetime.getDate() : datetime.getDate();
			let Nhour = datetime.getHours() < 10 ? "0" + datetime.getHours() : datetime.getHours();
			let Nminute = datetime.getMinutes() < 10 ? "0" + datetime.getMinutes() : datetime.getMinutes();
			let Nsecond = datetime.getSeconds() < 10 ? "0" + datetime.getSeconds() : datetime.getSeconds();
			result = Nyear + "-" + Nmonth + "-" + Ndate
		}
		return result;
	},
	//取整
	toRound(number) {
		var bite = 0;
		if (number < 10) {
			return 10;
		}
		if (number > 10 && number < 50) {
			return 50;
		}
		while (number >= 10) {
			number /= 10;
			bite += 1;
		}
		return Math.ceil(number) * Math.pow(10, bite);
	},
	getAmountChinese(val) {
		if (!val && val !== 0) return '';
		if (val == 0) return '零元整';
		const regExp = /[a-zA-Z]/;
		if (regExp.test(val)) return '数字较大溢出';
		let amount = +val;
		if (isNaN(amount)) return '';
		if (amount < 0) amount = Number(amount.toString().split('-')[1]);
		const NUMBER = ['零', '壹', '贰', '叁', '肆', '伍', '陆', '柒', '捌', '玖'];
		const N_UNIT1 = ['', '拾', '佰', '仟'];
		const N_UNIT2 = ['', '万', '亿', '兆'];
		const D_UNIT = ['角', '分', '厘', '毫'];
		let [integer, decimal] = amount.toString().split('.');
		if (integer && (integer.length > 15 || integer.indexOf('e') > -1)) return '数字较大溢出';
		let res = '';
		// 整数部分
		if (integer) {
			let zeroCount = 0;
			for (let i = 0, len = integer.length; i < len; i++) {
				const num = integer.charAt(i);
				const pos = len - i - 1; // 排除个位后 所处的索引位置
				const q = pos / 4;
				const m = pos % 4;
				if (num === '0') {
					zeroCount++;
				} else {
					if (zeroCount > 0 && m !== 3) res += NUMBER[0];
					zeroCount = 0;
					res += NUMBER[parseInt(num)] + N_UNIT1[m];
				}
				if (m == 0 && zeroCount < 4) res += N_UNIT2[Math.floor(q)];
			}
		}
		if (Number(integer) != 0) res += '元';
		// 小数部分
		if (parseInt(decimal)) {
			for (let i = 0; i < 4; i++) {
				const num = decimal.charAt(i);
				if (parseInt(num)) res += NUMBER[num] + D_UNIT[i];
			}
		} else {
			res += '整';
		}
		if (val < 0) res = '负数' + res;
		return res;
	},
	thousandsFormat(num) {
		if (num === 0) return '0';
		if (!num) return '';
		const numArr = num.toString().split('.');
		numArr[0] = numArr[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
		return numArr.join('.');
	},
	base64: {
		encode(str, isEncodeURIComponent = true) {
			if (!str) return ''
			const newStr = CryptoJS.enc.Base64.stringify(CryptoJS.enc.Utf8.parse(str))
			return isEncodeURIComponent ? encodeURIComponent(newStr) : newStr
		},
		decode(str, isDecodeURIComponent = true) {
			if (!str) return ''
			const newStr = isDecodeURIComponent ? decodeURIComponent(str) : str
			const result = CryptoJS.enc.Utf8.stringify(CryptoJS.enc.Base64.parse(newStr))
			return result
		}
	},
	idGenerator() {
		let quotient = (new Date() - new Date('2020-08-01'))
		quotient += Math.ceil(Math.random() * 1000)
		const chars = '0123456789ABCDEFGHIGKLMNOPQRSTUVWXYZabcdefghigklmnopqrstuvwxyz';
		const charArr = chars.split("")
		const radix = chars.length;
		const res = []
		do {
			let mod = quotient % radix;
			quotient = (quotient - mod) / radix;
			res.push(charArr[mod])
		} while (quotient);
		return res.join('')
	},
	dynamicText(value, options) {
		if (!value) return ''
		if (Array.isArray(value)) {
			if (!options || !Array.isArray(options)) return value.join()
			let textList = []
			for (let i = 0; i < value.length; i++) {
				let item = options.filter(o => o.id == value[i])[0]
				if (!item || !item.fullName) {
					textList.push(value[i])
				} else {
					textList.push(item.fullName)
				}
			}
			return textList.join()
		}
		if (!options || !Array.isArray(options)) return value
		let item = options.filter(o => o.id == value)[0]
		if (!item || !item.fullName) return value
		return item.fullName
	},
	treeToArray(treeData, type = '') {
		let list = []
		const loop = (treeData) => {
			for (let i = 0; i < treeData.length; i++) {
				const item = treeData[i]
				if (!type || item.type === type) list.push(item)
				if (item.children && Array.isArray(item.children)) loop(item.children)
			}
		}
		loop(treeData)
		return list
	},
	dynamicTreeText(value, options) {
		if (!value) return ''
		const transfer = (data, partition) => {
			let textList = []
			const loop = (data, id) => {
				for (let i = 0; i < data.length; i++) {
					if (data[i].id === id) {
						textList.push(data[i].fullName)
						break
					}
					if (data[i].children) loop(data[i].children, id)
				}
			}
			for (let i = 0; i < data.length; i++) {
				if (Array.isArray(data[i])) {
					textList.push(transfer(data[i], "/"))
				} else {
					loop(options, data[i])
				}
			}
			return textList.join(partition)
		}
		if (!options || !Array.isArray(options)) return Array.isArray(value) ? value.join() : value
		if (Array.isArray(value)) return transfer(value)
		return transfer(value.split())
	},
	onlineUtils: {
		// 获取用户信息
		getUserInfo() {
			const userInfo = uni.getStorageSync('userInfo') || {};
			userInfo.token = uni.getStorageSync('token') || '';
			return userInfo;
		},
		// 获取设备信息
		getDeviceInfo() {
			const deviceInfo = {
				vueVersion: '3',
				origin: 'app'
			};
			return deviceInfo;
		},
		// 请求
		request(url, method, data, header) {
			return request({
				url: url,
				method: method || 'GET',
				data: data || {},
				header: header || {},
				options: {
					load: false
				}
			})
		},
		// 路由跳转
		route(url, type = 'navigateTo') {
			if (!url) return;
			uni.$u.route({
				url,
				type
			})
		},
		// 消息提示
		toast(message, _type = 'info', duration = 3000) {
			uni.$u.toast(message, duration)
		},
	},
	aesEncryption: {
		decrypt(str, cipherKey = '') {
			if (!cipherKey) cipherKey = define.cipherKey
			const hexStr = CryptoJS.enc.Base64.stringify(CryptoJS.enc.Hex.parse(str))
			const decryptedData = CryptoJS.AES.decrypt(hexStr, CryptoJS.enc.Utf8.parse(cipherKey), {
				mode: CryptoJS.mode.ECB,
				padding: CryptoJS.pad.Pkcs7
			}).toString(CryptoJS.enc.Utf8);
			return decryptedData
		},
		encrypt(str, cipherKey = '') {
			if (!cipherKey) cipherKey = define.cipherKey
			const encryptedData = CryptoJS.AES.encrypt(str, CryptoJS.enc.Utf8.parse(cipherKey), {
				mode: CryptoJS.mode.ECB,
				padding: CryptoJS.pad.Pkcs7
			}).toString();
			const result = CryptoJS.enc.Hex.stringify(CryptoJS.enc.Base64.parse(encryptedData))
			return result
		}
	},
	getDistance(lat1, lon1, lat2, lon2) {
		const toRadians = (degrees) => {
			return degrees * (Math.PI / 180);
		}
		const R = 6371;
		const dLat = toRadians(lat2 - lat1);
		const dLon = toRadians(lon2 - lon1);
		const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
			Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) *
			Math.sin(dLon / 2) * Math.sin(dLon / 2);
		const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
		const distance = R * c;
		return distance * 1000;
	},
	getParamList(templateJson, data, rowKey = 'id') {
		if (!templateJson || !templateJson.length) return [];
		for (let i = 0; i < templateJson.length; i++) {
			const e = templateJson[i];
			if (e.sourceType == 1 && data) {
				e.defaultValue = data[e.relationField] || data[e.relationField] == 0 || data[e
						.relationField] ==
					false ? data[e.relationField] : '';
			}
			if (e.sourceType == 4 && e.relationField == '@formId') e.defaultValue = data[rowKey] || '';
		}
		return templateJson;
	},
	getBatchParamList(templateJson, data) {
		if (!templateJson?.length) return [];
		for (let i = 0; i < templateJson.length; i++) {
			const e = templateJson[i];
			if (e.sourceType == 1 && data.items?.length) {
				e.defaultValue = data.items.map(o => (data.webType == '4' ? o[e.relationField] : o[e
					.relationField + '_jnpfId']));
			}
			if (e.sourceType == 4 && e.relationField == '@formId') e.defaultValue = data.items.map(o => o.id);
		}
		return templateJson;
		// if (!templateJson || !templateJson.length) return [];
		// for (let i = 0; i < templateJson.length; i++) {
		// 	const e = templateJson[i];
		// 	let defaultValue = [];
		// 	if (e.sourceType === 1 && data.length) {
		// 		data.forEach(o => {
		// 			if (type != 1 && o.hasOwnProperty(e.relationField + '_jnpfId')) {
		// 				defaultValue.push(o[e.relationField + '_jnpfId']);
		// 			} else {
		// 				defaultValue.push(o[e.relationField]);
		// 			}
		// 		});
		// 	}
		// 	e.defaultValue = defaultValue;
		// 	if (e.sourceType === 4 && e.relationField === '@formId' && data.id !== undefined) {
		// 		e.defaultValue = [data.id];
		// 	} else if (e.sourceType !== 1) {
		// 		e.defaultValue = [];
		// 	}
		// }
		// return templateJson;
	},
	getLaunchFlowParamList(transferList, data, rowKey = 'id') {
		transferList = JSON.parse(JSON.stringify(transferList))
		if (!transferList?.length) return [];
		for (let i = 0; i < transferList.length; i++) {
			const e = transferList[i];
			if (e.sourceType == 1) {
				if (e.sourceValue == '@formId') {
					e.sourceValue = data[rowKey] || '';
				} else {
					e.sourceValue = data[e.sourceValue] || data[e.sourceValue] == 0 || data[e.sourceValue] ==
						false ? data[e.sourceValue] : '';
				}
			}
		}
		return transferList;
	}
}
export default jnpf