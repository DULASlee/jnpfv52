import {
	getBillNumber
} from '@/api/common'

const includeList = ['crmOrder', 'salesOrder', 'leaveApply']
export default {
	props: {
		config: {
			type: Object,
			default: () => {}
		},
	},
	data() {
		return {
			flowUrgentOptions: [{
				id: 1,
				fullName: '普通'
			}, {
				id: 2,
				fullName: '重要'
			}, {
				id: 3,
				fullName: '紧急'
			}],
			fileList: [],
			setting: {},
			userInfo: {},
			eventType: '',
			paymentMethodOptions: [],
			requiredList: {},
			requiredObj: []
		}
	},
	mounted() {
		this.init(this.config)
		this.$refs.dataForm.setRules(this.rules)
		this.userInfo = uni.getStorageSync('userInfo') || {}
	},
	methods: {
		checkChildRule() {
			let list = {}
			this.requiredObj.forEach((data) => {
				if (data.required) {
					list[data.id] = data.name + '不能为空'
				}
			})
			let title = [];
			for (let k in list) {
				let num = k.split("-");
				let childKey = num[0];
				num.forEach((model, i) => {
					if (i == 1) {
						let childData = this.dataForm[childKey]
						childData.forEach((child, i) => {
							if (child[model] instanceof Array) {
								if (child[model].length == 0) {
									title.push(list[childKey + "-" + model])
								}
							} else {
								if (!child[model] && child[model] !== 0) {
									title.push(list[childKey + "-" + model])
								}
							}
						})
					}
				})
			}
			let _regList = this.regList
			for (let k in _regList) {
				let childData = this.dataForm[k]
				for (let n in _regList[k]) {
					for (let i = 0; i < _regList[k][n].length; i++) {
						const element = _regList[k][n][i]
						if (element.pattern) {
							element.pattern = element.pattern.toString()
							let start = element.pattern.indexOf('/')
							let stop = element.pattern.lastIndexOf('/')
							let str = element.pattern.substring(start + 1, stop)
							let reg = new RegExp(str)
							element.pattern = reg
						}
						childData.forEach((item, index) => {
							if (item[n] && !element.pattern.test(item[n])) {
								title.push(element.message)
							}
						})
					}
				}
			}
			if (title.length > 0) {
				return title[0]
			}
		},
		/* 初始化处理 */
		init(data) {
			this.dataForm.id = data.id || ''
			this.dataForm.flowId = data.flowId
			this.setting = data
			if (data.formEnCode !== "revoke") this.updateDataRule()
			this.$nextTick(() => {
				this.$refs.dataForm.resetFields()
				if (this.beforeInit) this.beforeInit()
				if (data.id) {
					let dataForm = JSON.parse(JSON.stringify(data.draftData)) || JSON.parse(JSON.stringify(data
						.formData))
					if (this.selfGetInfo && typeof this.selfGetInfo === "function") {
						this.selfGetInfo(dataForm)
					} else {
						this.dataForm = JSON.parse(JSON.stringify(dataForm))
					}
					if (includeList.includes(data.formEnCode) && this.dataForm.fileJson) {
						this.fileList = JSON.parse(this.dataForm.fileJson)
					}
					return
				} else {
					if (this.selfInit) this.selfInit(data)
					if (!this.billEnCode) return
					getBillNumber(this.billEnCode).then(res => {
						if (data.formEnCode === 'crmOrder') {
							this.dataForm.orderCode = res.data
						} else {
							this.dataForm.billNo = res.data
						}
					})
				}
			})
		},
		/* 提交 */
		submit(eventType, flowUrgent) {
			this.eventType = eventType
			this.$refs.dataForm.setRules(this.rules)
			this.$refs.dataForm.validate((valid) => {
				if (valid) {
					if (includeList.includes(this.setting.formEnCode)) {
						this.dataForm.fileJson = !!this.fileList.length ? JSON.stringify(this.fileList) : ''
					}
					if (!!this.checkChildRule()) return this.$u.toast(`${this.checkChildRule()}`)
					if (this.exist && !!this.exist()) return this.$u.toast(`${this.exist()}`)
					let dataForm = {}
					if (this.beforeSubmit && typeof this.beforeSubmit === "function") {
						dataForm = this.beforeSubmit()
					} else {
						dataForm = this.dataForm
					}
					if (includeList.includes(this.setting.formEnCode)) {
						dataForm.fileJson = JSON.stringify(this.fileList)
					}
					if (eventType === 'save' || eventType === 'submit') {
						if (this.selfSubmit && typeof this.selfSubmit === "function") {
							this.selfSubmit(this.dataForm, flowUrgent)
							return
						}
					}
					this.$emit('eventReceiver', {
						formData: dataForm,
						id: this.dataForm.id
					}, eventType)
				}
			})
		},
		updateDataRule() {
			let newRules = {}
			for (let i = 0; i < this.setting.formOperates.length; i++) {
				const item = this.setting.formOperates[i]
				if (item.required) {
					this.$set(this.requiredList, item.id, item.required)
					if (item.jnpfKey != 'rate' && item.jnpfKey != 'slider') this.requiredObj.push(item)
				}
				const newRulesItem = {
					required: item.required || false,
					message: item.name + '不能为空',
					trigger: item.trigger || ['blur', 'change'],
					type: ''
				}
				const numList = ['inputNumber', 'datePicker', 'switch', 'rate', 'slider']
				if (item.dataType === 'array') newRulesItem.type = item.dataType
				if (numList.includes(item.jnpfKey)) newRulesItem.type = 'number'
				if (['relationForm', 'popupSelect'].includes(item.jnpfKey)) {
					newRulesItem.type = 'any',
						newRulesItem.validator = (rule, value, callback) => {
							if (value || value === 0) {
								callback();
							} else {
								callback(new Error(item.name + '不能为空'));
							}
						}
				}
				if (!this.rules.hasOwnProperty(item.id)) {
					if (item.required) this.$set(newRules, item.id, [newRulesItem])
				} else {
					let withoutRequiredItem = true
					for (let i = 0; i < this.rules[item.id].length; i++) {
						if (this.rules[item.id][i].hasOwnProperty('required')) {
							this.rules[item.id][i].required = item.required || false
							withoutRequiredItem = false
						}
					}
					if (withoutRequiredItem && item.required) this.rules[item.id].push(newRulesItem)
				}
			}
			this.rules = {
				...this.rules,
				...newRules
			}
			this.$refs.dataForm.setRules(this.rules)
		},
		/* 可见 */
		judgeShow(id) {
			if (this.setting.opType == 4) return true
			if (!this.setting.formOperates || !this.setting.formOperates.length) return true
			let arr = this.setting.formOperates.filter(o => o.id === id) || []
			if (!arr.length) return true
			let item = arr[0]
			return item.read
		},
		/* 可写 */
		judgeWrite(id) {
			if (this.setting.readonly) return true
			if (!this.setting.formOperates || !this.setting.formOperates.length) return false
			let arr = this.setting.formOperates.filter(o => o.id === id) || []
			if (!arr.length) return true
			let item = arr[0]
			return !item.write
		}
	}
}