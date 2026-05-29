<template>
	<view class="jnpf-wrap jnpf-wrap-form">
		<JnpfParser v-if="!loading" ref="dynamicForm" :formConf="formConf" :key="key" @submit="sumbitForm" />
		<view class="buttom-actions" v-if="origin !='scan'">
			<u-button class="buttom-btn" @click.stop="resetForm">{{$t('common.resetText')}}</u-button>
			<u-button class="buttom-btn" type="primary" @click.stop="submit" :loading="btnLoading">
				{{getOkText}}
			</u-button>
		</view>
	</view>
</template>

<script>
	import {
		createModel,
		getModelInfo
	} from '@/api/apply/visualDev'
	export default {
		props: ['config', 'modelId', 'isPreview', 'origin', 'id'],
		data() {
			return {
				dataForm: {
					data: ''
				},
				formConf: {},
				key: +new Date(),
				btnLoading: false,
				loading: true,
				isAdd: false,
				userInfo: {}
			}
		},
		computed: {
			getOkText() {
				const text = this.formConf.confirmButtonTextI18nCode ?
					this.$t(this.formConf.confirmButtonTextI18nCode, this.formConf.confirmButtonText) :
					this.formConf.confirmButtonText;
				return text || this.$t('common.okText');
			},
		},
		created() {
			this.init()
		},
		methods: {
			init() {
				this.userInfo = uni.getStorageSync('userInfo') || {}
				this.formConf = JSON.parse(this.config.formData)
				this.loading = true
				this.initData()
			},
			initData() {
				this.$nextTick(() => {
					if (this.origin === 'scan') {
						let extra = {
							modelId: this.modelId,
							id: this.id,
							type: 2
						}
						uni.setStorageSync('dynamicModelExtra', extra)
						getModelInfo(this.modelId, this.id).then(res => {
							this.dataForm = res.data
							if (!this.dataForm.data) return
							this.formData = JSON.parse(this.dataForm.data)
							this.fillFormData(this.formConf, this.formData)
							this.$nextTick(() => {
								this.loading = false
							})
						})
					} else {
						this.formData = {}
						this.loading = false
						this.isAdd = true
						this.fillFormData(this.formConf, this.formData)
					}
					this.key = +new Date()
				})
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
							if (this.origin === 'scan') this.$set(item, 'disabled', true)
							let noShow = !config.noShow ? false : config.noShow
							let isVisibility = false
							if (!config.visibility || (Array.isArray(config.visibility) && config.visibility.includes(
									'app'))) isVisibility = true
							this.$set(config, 'isVisibility', isVisibility)
							this.$set(config, 'noShow', noShow)
						} else {
							let noShow = false,
								isVisibility = false
							if (!config.visibility || (Array.isArray(config.visibility) && config.visibility.includes(
									'app'))) isVisibility = true
							this.$set(config, 'isVisibility', isVisibility)
							this.$set(config, 'noShow', noShow)
						}
						if (config && config.children && Array.isArray(config.children)) loop(config.children)
					}
				}
				loop(form.fields)
			},
			sumbitForm(data, callback) {
				if (!data) return
				this.btnLoading = true
				this.dataForm.data = JSON.stringify(data)
				if (callback && typeof callback === "function") callback()
				createModel(this.modelId, this.dataForm).then(res => {
					uni.showToast({
						title: res.msg,
						complete: () => {
							setTimeout(() => {
								this.btnLoading = false
								uni.navigateBack()
							}, 1500)
						}
					})
				}).catch(() => {
					this.btnLoading = false
				})
			},
			submit() {
				if (this.isPreview) return this.$u.toast('功能预览不支持数据保存')
				this.$refs.dynamicForm && this.$refs.dynamicForm.submitForm()
			},
			resetForm() {
				this.loading = true
				this.$nextTick(() => {
					this.loading = false
					this.$refs.dynamicForm && this.$refs.dynamicForm.resetForm()
					this.init()
					this.key = +new Date()
				})
			}
		}
	}
</script>