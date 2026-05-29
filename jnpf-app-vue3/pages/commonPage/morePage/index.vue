<template>
	<view class="morePage-v">
		<allCommonFlow ref="allCommonFlow" v-if="type == 1"></allCommonFlow>
		<allCommonMenus ref="allCommonMenus" v-if="type == 2"></allCommonMenus>
	</view>
</template>

<script>
	import allCommonFlow from './allCommonFlow.vue'
	import allCommonMenus from './allCommonMenus.vue'
	export default {
		components: {
			allCommonFlow,
			allCommonMenus
		},
		data() {
			return {
				type: '1'
			}
		},
		onLoad(e) {
			this.type = e?.type || '1'
			uni.setNavigationBarTitle({
				title: this.type == '1' ? '常用流程' : '常用菜单'
			})
		},
		methods: {}
	}
</script>

<style>
	page {
		background-color: #f0f2f6;
	}
</style>