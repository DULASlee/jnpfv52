<template>
	<view class="notData-box u-flex-col"
		:style="{'padding-top':paddingTop+'rpx','margin-top':marginTop+'rpx','background-color':backgroundColor,'z-index':zIndex}">
		<view class="u-flex-col notData-inner">
			<image :src="icon" mode="" class="notData-inner-img"></image>
			<text class="notData-inner-text">{{$t('common.noData')}}</text>
		</view>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	export default {
		props: {
			paddingTop: {
				type: [String, Number],
				default: 300
			},
			marginTop: {
				type: [String, Number],
				default: 0
			},
			zIndex: {
				type: [String, Number],
				default: 999
			},
			backgroundColor: {
				type: String,
				default: '#f0f2f6'
			}
		},
		data() {
			return {
				icon: resources.message.nodata,
			}
		},
	}
</script>

<style lang="scss">
	.notData-box {
		width: 100%;
		height: 100%;
		justify-content: center;
		align-items: center;

		.notData-inner {
			width: 154px;
			height: 170px;
			align-items: center;

			.notData-inner-text {
				padding: 30rpx 0;
				color: #909399;
			}

			.notData-inner-img {
				width: 100%;
				height: 100%;
			}
		}
	}
</style>