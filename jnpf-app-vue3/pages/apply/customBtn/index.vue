<template>
	<view class="jnpf-wrap jnpf-wrap-form">
		<JnpfParser :formConf="formConf" ref="dynamicForm" v-if="!loading" @submit="sumbitForm" :key="key" />
		<view class="buttom-actions">
			<u-button class="buttom-btn" @click.stop="cancel">取消</u-button>
			<u-button class="buttom-btn" type="primary" @click.stop="submit" :loading="btnLoading">
				{{config.confirmButtonText||'确定'}}
			</u-button>
		</view>
	</view>
</template>

<script>
	import {
		getConfigData,
		getModelInfo,
		createModel
	} from '@/api/apply/visualDev'
	import {
		getDataInterfaceRes
	} from '@/api/common'
	export default {
		data() {
			return {
				config: {},
				id: "",
				modelId: "",
				formConf: {},
				dataForm: {},
				key: +new Date(),
				loading: false,
				btnLoading: false,
				isPreview: true,
				formData: {},
				isAdd: false,
				userInfo: {}
			}
		},
		onLoad(e) {
			this.userInfo = uni.getStorageSync('userInfo') || {}
			this.loading = true
			let data = e.data ? JSON.parse(decodeURIComponent(e.data)) : {}
			this.config = data.config
			this.id = data.id
			this.modelId = data.modelId
			this.isPreview = data.isPreview
			if (this.id != null && this.id != undefined && this.id != '') {
				this.isAdd = false
			} else {
				this.isAdd = true
			}
			uni.setNavigationBarTitle({
				title: this.config.popupTitle
			})
			if (this.config.modelId) this.getConfigData(data.row)
		},
		methods: {
			getConfigData(row) {
				getConfigData(this.config.modelId).then(res => {
					if (res.code !== 200 || !res.data) {
						uni.showToast({
							title: res.msg || '请求出错，请重试',
							icon: 'none'
						})
						return
					}
					this.formConf = JSON.parse(res.data.formData)
					const setDataFun = (formData) => {
						if (this.config.formOptions.length) {
							for (let k in formData) {
								for (let i = 0; i < this.config.formOptions.length; i++) {
									const e = this.config.formOptions[i]
									if (e.currentField == '@formId') this.formData[e.field] = formData.id;
									if (e.currentField == k) this.formData[e.field] = formData[k]
								}
							}
						}
						this.fillFormData(this.formConf, this.formData)
						this.key = +new Date()
						this.loading = false
					}
					if (this.id) {
						getModelInfo(this.modelId, this.id).then(res => {
							let dataForm = res.data
							if (!dataForm.data) return
							const formData = JSON.parse(dataForm.data)
							this.formData = {}
							setDataFun({
								...formData,
								id: this.id
							})
						})
					} else {
						const formData = row
						setDataFun(formData)
					}
				}).catch(() => {})
			},
			fillFormData(form, data) {
				const loop = list => {
					for (let i = 0; i < list.length; i++) {
						let item = list[i]
						let vModel = item.__vModel__
						let config = item.__config__
						if (vModel) {
							let val = data.hasOwnProperty(vModel) ? data[vModel] : config.defaultValue
							if (!config.isSubTable) config.defaultValue = val
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
							let noShow = !item.__config__.noShow ? false : item.__config__.noShow
							let isVisibility = false
							if (!item.__config__.visibility || (Array.isArray(item.__config__.visibility) && item
									.__config__.visibility.includes('app'))) isVisibility = true
							this.$set(item.__config__, 'isVisibility', isVisibility)
							this.$set(item.__config__, 'noShow', noShow)
						} else {
							let noShow = false,
								isVisibility = false
							if (!item.__config__.visibility || (Array.isArray(item.__config__.visibility) && item
									.__config__.visibility.includes('app'))) isVisibility = true
							this.$set(item.__config__, 'isVisibility', isVisibility)
							this.$set(item.__config__, 'noShow', noShow)
						}
						if (item.__config__ && item.__config__.children && Array
							.isArray(item.__config__.children)) {
							loop(item.__config__.children)
						}

					}
				}
				loop(form.fields)
			},
			cancel() {
				uni.navigateBack();
			},
			sumbitForm(data, callback) {
				if (!data) return
				this.btnLoading = true
				const successFun = (res, callback) => {
					if (callback && typeof callback === "function") callback()
					uni.showToast({
						title: res.msg,
						complete: () => {
							setTimeout(() => {
								this.btnLoading = false
								if (this.config.isRefresh) uni.$emit('refresh')
								uni.navigateBack()
							}, 1500)
						}
					})
				}
				if (this.config.customBtn) {
					const query = {
						paramList: this.jnpf.getParamList(this.config.templateJson, {
							...data,
							id: this.id
						}) || []
					};
					getDataInterfaceRes(this.config.interfaceId, query).then(res => {
						successFun(res, callback)
					}).catch(() => {
						this.btnLoading = false
					})
				} else {
					this.dataForm.data = JSON.stringify(data)
					createModel(this.config.modelId, this.dataForm).then(res => {
						successFun(res, callback)
					}).catch(() => {
						this.btnLoading = false
					})
				}
			},
			submit() {
				if (this.isPreview) {
					uni.showToast({
						title: '功能预览不支持数据保存',
						icon: 'none'
					})
					return
				}
				this.$refs.dynamicForm && this.$refs.dynamicForm.submitForm()
			},
		}
	}
</script>

<style scoped lang="scss">
	page {
		background-color: #f0f2f6;
	}
</style>