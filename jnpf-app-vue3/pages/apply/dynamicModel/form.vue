<template>
	<view class="dynamicModel-form-v jnpf-wrap jnpf-wrap-form" v-if="showPage">
		<JnpfParser :formConf="formConf" ref="dynamicForm" v-if="!loading" @submit="sumbitForm" :key="key" />
		<view class="buttom-actions" v-if="btnType === 'btn_edit' || btnType === 'btn_add'">
			<CustomButton class="u-flex buttom-btn-left-inner" :btnText="getCancelText"
				btnIcon="icon-ym icon-ym-add-cancel" customIcon :btnLoading="btnLoading" />
			<u-button class="buttom-btn" type="primary" @click.stop="submit" :loading="btnLoading">
				{{getOkText}}
			</u-button>
		</view>
	</view>
</template>
<script>
	import CustomButton from '@/components/CustomButton'
	import {
		getConfigData,
		createModel,
		updateModel,
		getModelInfo
	} from '@/api/apply/visualDev'
	export default {
		components: {
			CustomButton
		},
		data() {
			return {
				webType: '',
				showPage: false,
				btnLoading: false,
				loading: true,
				isPreview: '0',
				modelId: '',
				formConf: {},
				formData: {},
				dataForm: {
					id: '',
					data: ''
				},
				btnType: '',
				formPermissionList: {},
				formList: [],
				key: +new Date(),
				config: {},
				clickType: 'submit',
				prevDis: false,
				nextDis: false,
				index: 0,
				userInfo: {},
				isAdd: false
			}
		},
		computed: {
			getOkText() {
				const text = this.formConf.confirmButtonTextI18nCode ?
					this.$t(this.formConf.confirmButtonTextI18nCode, this.formConf.confirmButtonText) :
					this.formConf.confirmButtonText;
				return text || this.$t('common.okText');
			},
			getCancelText() {
				const text = this.formConf.cancelButtonTextI18nCode ?
					this.$t(this.formConf.cancelButtonTextI18nCode, this.formConf.cancelButtonText) :
					this.formConf.cancelButtonText;
				return text || this.$t('common.cancelText');
			}
		},
		onLoad(option) {
			this.userInfo = uni.getStorageSync('userInfo') || {}
			this.config = JSON.parse(this.jnpf.base64.decode(option.config))
			this.index = this.config.index
			this.formPermissionList = !this.config.currentMenu ? [] : JSON.parse(decodeURIComponent(this.config
				.currentMenu))
			this.formList = this.formPermissionList.formList
			this.btnType = this.config.btnType || ''
			this.modelId = this.config.modelId;
			this.isPreview = this.config.isPreview || '0';
			this.dataForm.id = this.config.id || ''
			uni.setNavigationBarTitle({
				title: this.dataForm.id ? this.$t('common.editText') : this.$t('common.addText')
			})
			this.getConfigData()
		},
		methods: {
			getConfigData() {
				getConfigData(this.modelId).then(res => {
					if (res.code !== 200 || !res.data) {
						uni.showToast({
							title: '暂无此页面',
							icon: 'none',
							complete: () => {
								setTimeout(() => {
									uni.navigateBack()
								}, 1500)
							}
						})
						return
					}
					this.formConf = res.data.formData ? JSON.parse(res.data.formData) : {};
					this.showPage = true
					this.initData()
				})
			},
			initData() {
				this.$nextTick(() => {
					if (this.dataForm.id) {
						let extra = {
							modelId: this.modelId,
							id: this.dataForm.id,
							type: 1
						}
						uni.setStorageSync('dynamicModelExtra', extra)
						getModelInfo(this.modelId, this.dataForm.id).then(res => {
							this.dataForm = res.data
							if (!this.dataForm.data) return
							this.formData = {
								...JSON.parse(this.dataForm.data),
								id: this.dataForm.id
							}
							this.fillFormData(this.formConf, this.formData)
							this.$nextTick(() => {
								this.loading = false
							})
						})
					} else {
						this.isAdd = true
						this.formData = {}
						this.loading = false
						this.fillFormData(this.formConf, this.formData)
					}
					this.key = +new Date()
				})
			},
			fillFormData(form, data) {
				this.key = +new Date()
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
							const btn_detail = this.$permission.hasBtnP('btn_detail', this.formPermissionList
								.menuId)
							const btn_edit = this.$permission.hasBtnP('btn_edit', this.formPermissionList
								.menuId)
							if (!!this.dataForm.id && !btn_edit && btn_detail) item.disabled = btn_detail
							let noShow = !config.noShow ? false : config.noShow
							let isVisibility = false
							if (!config.visibility || (Array.isArray(config.visibility) && config.visibility.includes(
									'app'))) isVisibility = true
							this.$set(config, 'isVisibility', isVisibility)
							if (this.formPermissionList.useFormPermission) {
								let id = config.isSubTable ? parent.__vModel__ + '-' + vModel : vModel
								noShow = true
								if (this.formList && this.formList.length) {
									noShow = !this.formList.some(o => o.enCode === id)
								}
								noShow = config.noShow ? config.noShow : noShow
								this.$set(config, 'noShow', noShow)
							}
						} else {
							let noShow = config.noShow ? config.noShow : false,
								isVisibility = false
							if (!config.visibility || (Array.isArray(config.visibility) && config.visibility.includes(
									'app'))) isVisibility = true
							this.$set(config, 'isVisibility', isVisibility)
							this.$set(config, 'noShow', noShow)
						}
						if (config && config.children && Array.isArray(config.children)) {
							loop(config.children, item)
						}
					}
				}
				loop(form.fields)
				form.formData = data
				this.key = +new Date()
			},
			sumbitForm(data, callback) {
				if (!data) return
				this.btnLoading = true
				const formData = {
					...this.formData,
					...data
				}
				this.dataForm.data = JSON.stringify(formData)
				if (callback && typeof callback === "function") callback()
				const formMethod = this.dataForm.id ? updateModel : createModel
				formMethod(this.modelId, this.dataForm).then(res => {
					uni.showToast({
						title: res.msg,
						complete: () => {
							setTimeout(() => {
								if (this.clickType == 'save_add') {
									this.key = +new Date()
									this.$nextTick(() => {
										this.$refs.dynamicForm && this.$refs
											.dynamicForm.resetForm()
									})
								}
								this.btnLoading = false
								this.initData()
								if (this.clickType != 'save_proceed' && this.clickType !=
									'save_add') {
									uni.$emit('refresh')
									uni.navigateBack()
								}
							}, 1500)
						}
					})
				}).catch(() => {
					this.btnLoading = false
				})
			},
			commonSubmit(type) {
				this.clickType = type
				this.submit(type)
			},
			submit(type) {
				this.clickType = type
				if (this.isPreview == '1') return this.$u.toast('功能预览不支持数据保存')
				this.$refs.dynamicForm && this.$refs.dynamicForm.submitForm()
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}
</style>