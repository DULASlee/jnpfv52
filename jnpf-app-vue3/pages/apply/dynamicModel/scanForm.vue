<template>
	<view class="dynamicModel-v">
		<template v-if="showPage">
			<view class="jnpf-wrap jnpf-wrap-form" v-if="config.mt == 2">
				<JnpfParser :formConf="formConf" ref="dynamicForm" @submit="sumbitForm" :key="key" />
			</view>
			<template v-else>
				<FlowForm ref="flowForm" />
			</template>
		</template>
	</view>
</template>

<script>
	import FlowForm from '@/pages/workFlow/flowBefore/flowForm'
	import {
		getConfigData,
		getModelInfo
	} from '@/api/apply/visualDev'
	export default {
		name: 'scanForm',
		components: {
			FlowForm
		},
		data() {
			return {
				webType: '',
				showPage: false,
				origin: '',
				id: '',
				config: {},
				formConf: {},
				key: +new Date(),
				isAdd: false,
				userInfo: {}
			}
		},
		onLoad(option) {
			this.userInfo = uni.getStorageSync('userInfo') || {}
			this.config = JSON.parse(option.config)
			this.initData()
		},
		methods: {
			initData() {
				this.showPage = false
				if (this.config.mt == 2) {
					this.getConfigData()
				} else {
					this.isAdd = true
					let data = {
						flowId: this.config.fid,
						id: this.config.pid,
						formType: 2,
						opType: this.config.opt,
						taskId: this.config.ftid
					}
					this.showPage = true
					this.$nextTick(() => {
						this.$refs.flowForm.init(data)
					})
				}
			},
			getConfigData() {
				getConfigData(this.config.mid).then(res => {
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
					this.formConf = JSON.parse(res.data.formData)
					uni.setNavigationBarTitle({
						title: res.data.fullName
					})
					let extra = {
						modelId: this.config.mid,
						id: this.config.id,
						type: this.config.mt
					}
					uni.setStorageSync('dynamicModelExtra', extra)
					getModelInfo(this.config.mid, this.config.id).then(res => {
						if (!res.data.data) return
						let formData = JSON.parse(res.data.data)
						this.fillFormData(this.formConf, formData)
						this.$nextTick(() => {
							this.showPage = true
							this.key = +new Date()
						})
					})
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
							this.$set(item, 'disabled', true)
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
						if (item.__config__ && item.__config__.jnpfKey !== 'table' && item.__config__.children && Array
							.isArray(item.__config__.children)) {
							loop(item.__config__.children)
						}
					}
				}
				loop(form.fields)
			},
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.dynamicModel-v {
		height: 100%;
	}
</style>