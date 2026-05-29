<template>
	<u-popup v-model="showApply" :mode="mode" :height="height" @close="close" :width="width"
		:custom-style="customStyle">
		<slot></slot>
		<view class="tree-main">
			<ly-tree :props="prop" :node-key="prop.value" :tree-data="treeData" show-node-icon :defaultExpandAll='true'
				ref="tree" @node-click="handleNodeClick" :highlight-current="true" />
		</view>
	</u-popup>
</template>

<script>
	import LyTree from './ly-tree.vue'
	let _self;
	export default {
		components: {
			LyTree
		},
		props: {
			type: {
				type: String,
				default: ''
			},
			statusBarHeight: {
				type: Number,
				default: 196
			},
			show: {
				type: Boolean,
				default: false
			},
			treeData: Array,
			mode: {
				type: String,
				default: 'top'
			},
			height: {
				type: String,
				default: '600rpx'
			},
			width: {
				type: String,
				default: '400rpx'
			}
		},
		data() {
			return {
				showApply: false,
				isReady: false,
				prop: {
					label: 'fullName',
					isLeaf: 'isLeaf',
					value: "id",
					icon: 'avatar'
				},
				customStyle: {
					// #ifdef MP
					'margin-top': '160rpx'
					// #endif
					// #ifndef MP
					'margin-top': '80rpx'
					// #endif
				}
			}
		},
		watch: {
			show: {
				handler(val) {
					this.showApply = val
				},
				immediate: true,
			},
		},
		created() {
			_self = this
			this.isReady = true;
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			handleNodeClick(obj) {
				if (obj.data.type != 1) this.$emit('change', obj.data)
			},
			close() {
				this.$emit('close')
			}
		}
	}
</script>
<style>
	page {
		padding-top: 80rpx;
	}

	.ly-tree {
		/* padding: 20rpx !important; */
	}
</style>