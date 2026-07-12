<template>
	<view class="dynamicModel-v">
		<Form v-if="webType == 1" :config="config" :modelId="modelId" :isPreview="isPreview" />
		<List v-if="webType == 2 || webType == 4" :config="config" :modelId="modelId" :isPreview="isPreview"
			:title="title" :menuId="menuId" ref="List" />
	</view>
</template>

<script>
	import Form from "./components/form/index.vue";
	import List from "./components/list/index.vue";
	import {
		getFlowStartFormId
	} from "@/api/workFlow/flowEngine";
	import {
		getConfigData
	} from "@/api/apply/visualDev";
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()

	export default {
		name: "dynamicModel",
		components: {
			Form,
			List,
		},
		data() {
			return {
				webType: "",
				showPage: false,
				isPreview: false,
				modelId: "",
				menuId: "",
				title: "",
				config: {},
				preview: false,
				flowId: '',
				enableFlow: 0,
			};
		},
		onLoad(obj) {
			baseStore.getDictionaryDataAll()
			this.config = JSON.parse(this.jnpf.base64.decode(obj.config)) || {};
			this.isPreview = this.config.isPreview || false;
			this.enableFlow = this.config.type === 9 ? 1 : 0;
			this.title = this.config.fullName || "";
			this.menuId = this.config.id || "";
			uni.setNavigationBarTitle({
				title: this.title,
			});
			if (!this.enableFlow) return this.getConfigData();
			this.flowId = this.config.moduleId
			this.getModelId()
		},
		methods: {
			// 获取流程版本ID和发起节点表单ID
			getModelId() {
				getFlowStartFormId(this.flowId).then(res => {
					if (!res.data || !res.data.formId) return;
					this.config.moduleId = res.data.formId
					this.getConfigData();
				})
			},
			getConfigData() {
				getConfigData(this.config.moduleId, undefined).then((res) => {
					if (res.code !== 200 || !res.data) return this.handleError('暂无此页面')
					if (this.enableFlow && res.data.webType == 1) return this.jump();
					this.config = {
						...res.data,
						...this.config,
						enableFlow: this.enableFlow,
						flowId: this.flowId
					};
					this.showPage = true;
					this.isPreview = !!this.config.isPreview;
					this.modelId = this.config.moduleId;
					this.webType = this.config.webType;
				});
			},
			jump() {
				const config = {
					id: "",
					flowId: this.flowId,
					opType: "-1",
					hideCancelBtn: true,
					hideSaveBtn: true
				};
				uni.redirectTo({
					url: "/pages/workFlow/flowBefore/index?config=" +
						this.jnpf.base64.encode(JSON.stringify(config)),
					fail: () => {
						this.$u.toast("暂无此页面");
					},
				});
			},
			handleError(msg) {
				this.$u.toast(msg);
				setTimeout(() => {
					uni.navigateBack();
				}, 1500);
			}
		},
	};
</script>
<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.dynamicModel-v {
		height: 100%;
	}
</style>