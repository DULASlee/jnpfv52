import {
	getDataInterfaceRes
} from '@/api/common'
export default {
	data() {
		return {
			ids: [],
			selectItems: []
		}
	},
	methods: {
		checkboxChange(e, item) {
			this.$nextTick(() => {
				if (e.value) {
					this.selectItems.push(item)
				} else {
					const i = this.selectItems.findIndex(o => !o.checked)
					this.selectItems.splice(i, 1)
				}
			})
		},
		openSelectDialog(item) {
			let actionConfig = item.actionConfig
			const data = {
				actionConfig,
				formData: this.formData,
				tableVmodel: this.config.__vModel__
			}
			if (item.actionType == 1) return uni.navigateTo({
				url: '/pages/apply/tableLinkage/index?data=' + JSON.stringify(data)
			})
			if (!this.tableFormData.some(item => item.checked)) return this.$u.toast('至少选中一条数据');
			if (item.actionType == 2) {
				if (actionConfig.executeType == 2) {
					this.handleScriptFunc(actionConfig)
				} else {
					this.handleInterface(actionConfig)
				}
			}
		},
		//自定义按钮JS操作
		handleScriptFunc(item) {
			let data = this.selectItems.map(child => {
				let obj = {};
				child.forEach(item => {
					obj[item.__vModel__] = item.value;
				});
				return obj;
			});
			const parameter = {
				data,
				refresh: this.initData,
				onlineUtils: this.jnpf.onlineUtils,
			}
			const func = this.jnpf.getScriptFunc.call(this, item.executeFunc)
			if (!func) return
			func.call(this, parameter)
		},
		//自定义按钮接口操作
		handleInterface(item) {
			let data = this.selectItems.flatMap(child => {
				return child.map(item => ({
					[item.__vModel__]: item.value
				}));
			});
			const handlerInterface = (data) => {
				let query = {
					paramList: this.getBatchParamList(item.executeTemplateJson, data) || [],
				}
				getDataInterfaceRes(item.executeInterfaceId, query).then(res => {
					uni.showToast({
						title: res.msg,
						icon: 'none'
					})
				})
			}
			if (!item.executeUseConfirm) return handlerInterface(data)
			uni.showModal({
				title: this.$t('common.tipTitle'),
				content: item.executeConfirmTitle || '确认执行此操作?',
				showCancel: true,
				confirmText: '确定',
				success: function(res) {
					if (res.confirm) {
						handlerInterface(data)
					}
				}
			});
		},
		getBatchParamList(templateJson, data) {
			if (!templateJson || !templateJson.length) return [];
			for (let i = 0; i < templateJson.length; i++) {
				const e = templateJson[i];
				let defaultValue = [];
				if (e.sourceType === 1 && data.length) {
					data.forEach(o => {
						if (o.hasOwnProperty(e.relationField)) {
							defaultValue.push(o[e.relationField]);
						}
					});
				}
				e.defaultValue = defaultValue;
				if (e.sourceType === 4 && e.relationField === '@formId' && data.id !== undefined) {
					e.defaultValue = [data.id];
				} else if (e.sourceType !== 1) {
					e.defaultValue = [];
				}
			}
			return templateJson;
		}
	}
}