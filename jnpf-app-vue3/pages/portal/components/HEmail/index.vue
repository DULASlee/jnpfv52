<template>
	<view class="portal-todoList-box-body">
		<template v-if="list.length">
			<a class="item com-hover" v-for="(item, i) in list" :key="i">
				<span class="name">{{item.fullName}}</span>
				<span class="time">{{item.creatorTime | date('yyyy-mm-dd')}}</span>
			</a>
		</template>
		<view v-else class="notData-box u-flex-col">
			<view class="u-flex-col notData-inner">
				<image :src="icon" mode="" class="iconImg"></image>
				<text class="notData-inner-text">{{$t('common.noData')}}</text>
			</view>
		</view>
	</view>
</template>
<script>
	import {
		getEmail
	} from '@/api/home'
	import resources from '@/libs/resources.js'
	export default {
		props: {
			config: {
				type: Object,
				default: () => {}
			}
		},
		data() {
			return {
				list: [],
				icon: resources.message.nodata,
			}
		},
		created() {
			this.getData()
		},
		methods: {
			getData() {
				getEmail().then(res => {
					this.list = res.data.list.slice(0, 7)
				})
			}
		}
	}
</script>
<style lang="scss">
	.portal-todoList-box-body {
		padding: 42rpx 10rpx 10rpx;
		max-height: 472rpx;
		overflow-y: scroll;

		.item {
			display: block;
			line-height: 40rpx;
			font-size: 0;
			margin-bottom: 24rpx;
			cursor: pointer;


			.name {
				font-size: 28rpx;
				display: inline-block;
				width: calc(100% - 180rpx);
				white-space: nowrap;
				text-overflow: ellipsis;
				overflow: hidden;
				word-break: break-all;
				vertical-align: top;
			}

			.time {
				font-size: 28rpx;
				display: inline-block;
				color: #999;
				width: 180rpx;
				text-align: right;
			}
		}
	}

	.notData-box {
		width: 100%;
		height: 100%;
		justify-content: center;
		align-items: center;

		.notData-inner {
			width: 280rpx;
			height: 308rpx;
			align-items: center;

			.iconImg {
				width: 100%;
				height: 100%;
			}

			.notData-inner-text {
				padding: 30rpx 0;
				color: #909399;
			}
		}
	}
</style>