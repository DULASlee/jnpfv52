<template>
	<view class="dynamicModel-v">
		<view class="jnpf-wrap jnpf-wrap-form" v-if="isShow">
			<childForm ref="child" :config="config" />
		</view>
	</view>
</template>
<script>
	import childForm from '@/pages/workFlow/flowBefore/form'
	import {
		getConfigData
	} from '@/api/apply/visualDev'
	import {
		getStartFormInfo
	} from '@/api/workFlow/workFlowForm'
	export default {
		name: 'scanForm',
		components: {
			childForm
		},
		data() {
			return {
				webType: '',
				origin: '',
				config: {},
				formConf: {},
				flowConfig: {},
				isShow: false,
				dataSource: '',
				formInfo: {},
			}
		},
		onLoad(data) {
			let obj = JSON.parse(data.config)
			this.initData(obj)
		},
		methods: {
			initData(data) {
				if (data.previewType === 'initiationForm') {
					uni.setNavigationBarTitle({
						title: data.title
					})
					getStartFormInfo(data.taskId).then(res => {
						this.formInfo = res.data.formInfo;
						this.config = {
							...data,
							formData: res.data.formData || {},
							formConf: this.formInfo.formData,
							readonly: true,
							formType: 1
						}
						this.isShow = true
					})
				} else {
					getConfigData(data.id, {
						type: data.previewType
					}).then(res => {
						if (!res.data || !res.data.formData) return
						this.config = {
							formEnCode: data.enCode,
							flowId: res.data.id,
							formConf: res.data.formData
						}
						this.isShow = true
					})
				}
			}
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