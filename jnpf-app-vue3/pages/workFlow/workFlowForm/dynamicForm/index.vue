<template>
	<view class="jnpf-wrap jnpf-wrap-workflow">
		<JnpfParser :formConf="formConf" ref="dynamicForm" v-if="!loading" @submit="sumbitForm" :key="key" />
	</view>
</template>

<script>
	export default {
		props: {
			config: {
				type: Object,
				default: () => {}
			},
		},
		data() {
			return {
				loading: true,
				key: +new Date(),
				setting: {},
				formConf: {},
				formData: {},
				eventType: '',
				flowUrgent: 1,
				dataForm: {
					id: '',
					flowId: ''
				},
				isAdd: false,
				userInfo: {}
			}
		},
		mounted() {
			this.init(this.config)
		},
		methods: {
			init(data) {
				this.userInfo = uni.getStorageSync('userInfo') || {}
				this.setting = data
				this.formConf = data.formConf ? JSON.parse(data.formConf) : {}
				this.dataForm.id = data.id || null;
				this.dataForm.flowId = data.flowId;
				this.loading = true;
				this.formData = {};
				this.$nextTick(() => {
					let extra = {}
					if (data.id) {
						this.isAdd = false;
						extra = {
							modelId: data.flowId,
							id: this.dataForm.id,
							type: data.type,
							flowId: data.flowId,
							processId: data.id,
							opType: data.opType,
							taskId: data.taskId
						}
						uni.setStorageSync('dynamicModelExtra', extra)
						const formData = data.draftData || data.formData || {}
						this.formData = {
							...formData,
							flowId: data.flowId
						}
					} else {
						this.isAdd = true;
					}
					if (data.previewType == 'initiationForm') this.formData = data.formData || {}
					this.fillFormData(this.formConf, this.formData)
					this.$nextTick(() => {
						this.loading = false
					})
					this.dataForm.flowId = data.flowId
					this.key = +new Date()
				})
			},
			fillFormData(form, data) {
				form.disabled = this.setting.readonly
				const loop = (list, parent) => {
					for (let i = 0; i < list.length; i++) {
						let item = list[i]
						let vModel = item.__vModel__
						let config = item.__config__
						if (vModel) {
							let val = data.hasOwnProperty(vModel) ? data[vModel] : config.defaultValue
							if (!config.isSubTable) config.defaultValue = val
							if (this.isAdd || config.isSubTable) { //新增时候，默认当前
								if (config.defaultCurrent) {
									if (config.jnpfKey === 'datePicker') {
										if (!data.hasOwnProperty(vModel)) {
											let format = this.jnpf.handelFormat(item.format)
											let dateStr = this.jnpf.toDate(new Date().getTime(), format)
											let time = format === 'yyyy' ? '-01-01 00:00:00' : format === 'yyyy-MM' ?
												'-01 00:00:00' : format === 'yyyy-MM-dd' ?
												' 00:00:00' : ''
											val = new Date(dateStr + time).getTime()
											config.defaultValue = val
										}
									}
									if (config.jnpfKey === 'timePicker') {
										if (!data.hasOwnProperty(vModel)) {
											config.defaultValue = this.jnpf.toDate(new Date(), item.format)
										}
									}
									const organizeIdList = this.userInfo.organizeIdList
									if (config.jnpfKey === 'organizeSelect' && Array.isArray(organizeIdList) &&
										organizeIdList.length) {
										config.defaultValue = item.multiple ? [organizeIdList] : organizeIdList
									}
									const departmentId = this.userInfo.departmentId
									if (config.jnpfKey === 'depSelect' && departmentId) {
										config.defaultValue = item.multiple ? [departmentId] : departmentId;
									}
									const positionIds = this.userInfo.positionIds
									if (config.jnpfKey === 'posSelect' && Array.isArray(positionIds) && positionIds
										.length) {
										config.defaultValue = item.multiple ? positionIds.map(o => o.id) : positionIds[
											0].id
									}
									const roleIds = this.userInfo.roleIds
									if (config.jnpfKey === 'roleSelect' && Array.isArray(roleIds) && roleIds.length) {
										config.defaultValue = item.multiple ? roleIds : roleIds[0];
									}
									const groupIds = this.userInfo.groupIds
									if (config.jnpfKey === 'groupSelect' && Array.isArray(groupIds) && groupIds
										.length) {
										config.defaultValue = item.multiple ? groupIds : groupIds[0];
									}
									const userId = this.userInfo.userId
									if (config.jnpfKey === 'userSelect' && userId) {
										config.defaultValue = item.multiple ? [userId] : userId;
									}
									if (config.jnpfKey === 'usersSelect' && userId) {
										config.defaultValue = item.multiple ? [userId + '--user'] : userId + '--user';
									}
									if (config.jnpfKey === 'sign' && this.userInfo.signImg) {
										config.defaultValue = this.userInfo.signImg
									}
								}
							}
							let noShow = item.__config__.noShow || false,
								isDisabled = item.disabled || false,
								required = item.__config__.required || false,
								isVisibility = false
							if (!item.__config__.visibility || (Array.isArray(item.__config__.visibility) && item
									.__config__.visibility.includes('app'))) isVisibility = true
							if (this.setting.formOperates && this.setting.formOperates.length) {
								let id = item.__config__.isSubTable ? parent?.__vModel__ + '-' + item?.__vModel__ :
									item
									.__vModel__
								let arr = this.setting.formOperates.filter(o => o.id === id) || []
								if (arr.length) {
									let obj = arr[0]
									noShow = !obj.read
									isDisabled = !obj.write
									required = obj.required ? obj.required : item.__config__.required
								}
							}
							isDisabled = item.readonly ? item.readonly : isDisabled;
							if (this.setting.readonly || config.disabled) isDisabled = true
							if (this.setting.origin === 'scan') isDisabled = true
							this.$set(item, 'disabled', isDisabled)
							this.$set(item.__config__, 'noShow', noShow)
							this.$set(item.__config__, 'required', required)
							this.$set(item.__config__, 'isVisibility', isVisibility)
						} else {
							let noShow = item.__config__.noShow ? item.__config__.noShow : false,
								isVisibility = false
							if (!item.__config__.visibility || (Array.isArray(item.__config__.visibility) && item
									.__config__.visibility.includes('app'))) isVisibility = true
							this.$set(item.__config__, 'isVisibility', isVisibility)
							this.$set(item.__config__, 'noShow', noShow)
						}
						if (item.__config__ && item.__config__.children && Array.isArray(item.__config__.children)) {
							loop(item.__config__.children, item)
						}
					}
				}
				loop(form.fields)
				form.formData = data
			},
			sumbitForm(data, callback) {
				if (!data) return
				const formData = {
					...this.formData,
					...data
				}
				this.dataForm.formData = formData
				if (callback && typeof callback === "function") callback()
				this.$emit('eventReceiver', this.dataForm, this.eventType)
			},
			submit(eventType, flowUrgent) {
				if (this.setting.isPreview == '1') return this.$u.toast('功能预览不支持数据保存')
				this.eventType = eventType
				this.flowUrgent = flowUrgent
				this.$refs.dynamicForm && this.$refs.dynamicForm.submitForm()
			},
		}
	}
</script>