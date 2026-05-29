<template>
	<view class="charts" v-if="show">
		<charts :config="config" :key="key" @getIndex="getIndex" @complete='complete' :markPoints="markPoints"
			:regionStep="regionStep" @regionStepClick="regionStepClick" ref="charts" :loading="pointLoading"></charts>
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
		methods: {
			complete(e) {
				if (this.config.jnpfKey === 'mapChart') this.setPoints(e)
			}
		}
	}
</script>
<style lang="scss">
	.charts {
		padding: 20rpx;
	}
</style>