<template>
	<view class="charts" v-if="show">
		<charts :config="config" :key="key" ref="charts"></charts>
	</view>
</template>
<script>
	import charts from './charts.vue'
	import chartsJs from '../chartsJs.js'
	export default {
		mixins: [chartsJs],
		components: {
			charts
		},
		props: {
			config: {
				type: Object,
				default: () => {}
			}
		},
		data() {
			return {
				key: +new Date(),
			}
		},
		methods: {}
	}
</script>
<style lang="scss">
	.charts {
		padding: 20rpx;
	}
</style>