<template>
	<view class="commonTabs-v">
		<view class="icon-ym u-tabs-box" v-if="list.length" :class="{'boxShadow':isBoxShadow}">
			<view class="apply-tabs">
				<u-tabs :list="list" v-model="tabCurrent" @change="change" :is-scroll="isScroll" :name="prop"
					:height="height">
				</u-tabs>
			</view>
			<view class="more-icon" :class="icon" @click="iconClick "></view>
		</view>
		<u-popup v-model="showPopup" mode="bottom" closeable close-icon-color="#666666">
			<view class="classifyTitle">
				全部分类
			</view>
			<scroll-view scroll-y="true" :style="{'height':scrollHeight+'rpx'}">
				<view class="classify-scroll-view u-flex">
					<view v-for="(item,index) in list" :key="index" @click="classifyItem(index)"
						class="classify-item u-font-28 u-line-1">
						{{item.fullName}}
					</view>
				</view>
			</scroll-view>
		</u-popup>
	</view>
</template>
<script>
	export default {
		props: {
			list: {
				type: Array,
				default: () => []
			},
			current: {
				type: Number,
				default: 0
			},
			scrollHeight: {
				type: String,
				default: '360'
			},
			prop: {
				type: String,
				default: 'fullName'
			},
			icon: {
				type: String,
				default: 'icon-ym icon-ym-app-more'
			},
			height: {
				type: String,
				default: '84'
			},
			type: {
				type: String,
				default: 'flow'
			},
			isBoxShadow: {
				type: Boolean,
				default: false
			},
			isScroll: {
				type: Boolean,
				default: true
			}
		},
		data() {
			return {
				tabCurrent: 0,
				showPopup: false,
				categoryList: []
			}
		},
		watch: {
			current: {
				handler(val) {
					this.tabCurrent = val
				},
				immediate: true
			}
		},
		methods: {
			classifyItem(index) {
				this.change(index)
			},
			iconClick() {
				if (this.type != 'flow') return this.$emit('iconClick');
				this.showPopup = true
			},
			change(e) {
				this.tabCurrent = e;
				this.showPopup = false;
				this.$emit('change', e);
			}
		}
	}
</script>

<style lang="scss">
	.commonTabs-v {
		.boxShadow {
			box-shadow: 0 0 10rpx rgba(0, 0, 0, 0.1);
		}

		.u-tabs-box {
			position: relative;

			.apply-tabs {
				width: 94%;

				.u-tabs {
					height: 2.75rem;
				}

			}

			.more-icon {
				position: absolute;
				top: 0;
				right: 0;
				/* #ifdef MP */
				height: 85rpx;
				line-height: 85rpx;
				/* #endif */
				/* #ifndef MP */
				height: 88rpx;
				line-height: 88rpx;
				/* #endif */
				text-align: center;

				padding: 0 16rpx;
				background-color: #fff;
				opacity: 0.9;
				box-shadow: 0 0 20rpx rgba(0, 0, 0, 0.1);
			}
		}

		.classifyTitle {
			font-family: PingFang SC;
			height: 110rpx;
			width: 100%;
			line-height: 110rpx;
			padding-left: 20rpx;
			padding-right: 20rpx;
			font-size: 32rpx;
			color: #333333;
			font-weight: 500;
		}

		.classify-scroll-view {
			padding-bottom: 70rpx;
			padding: 0 20rpx 64rpx;
			justify-content: flex-start;
			flex-wrap: wrap;

			.classify-item {
				width: 22%;
				text-align: center;
				margin: 10rpx;
				background-color: #EEF0F4;
				border-radius: 8rpx;
				background: rgba(238, 240, 244, 0.39);
				color: #666666;
				line-height: 58rpx;
				padding: 0 20rpx;
			}
		}
	}
</style>