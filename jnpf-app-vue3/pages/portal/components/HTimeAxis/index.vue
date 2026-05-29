<template>
	<view class="timeLine-v">
		<view class="timeLine" :key="key">
			<u-time-line v-if="option.isVertical" :class="option.isLeft?'timeLine-right':''">
				<u-time-line-item v-for="(item,i) in option.defaultValue" :key="i">
					<template v-slot:node>
						<view class="timeLine-dot" v-if="i==0" :style="{'background':'rgba(62, 213, 56, 0.39)'}">
						</view>
						<view class="timeLine-dot" v-else-if="i==option.defaultValue.length-1"
							:style="{'background':'rgba(228, 231, 237, 0.39)'}">
						</view>
						<view class="timeLine-dot" v-else :style="{'background':'rgba(25, 144, 250, 0.39)'}">
						</view>
					</template>
					<template v-slot:content>
						<view class="timeLine-content" :style="{'text-align':option.isLeft?'right':'left'}"
							@tap="jump(item)">
							<view class="u-font-24" v-if="option.isCrad">
								{{item.title}}
							</view>
							<view class="u-flex-col" :class="option.isCrad?'timeLine-title2':'timeLine-title'">
								<text class="name u-font-28">{{item.content}}</text>
								<text class="time u-font-28">{{item.timestamp}}</text>
							</view>
						</view>
					</template>
				</u-time-line-item>
			</u-time-line>
			<timeLine-row v-if="!option.isVertical" :list='option.defaultValue' :isCrad='option.isCrad'
				:isUpper="option.isUpper">
			</timeLine-row>
		</view>
	</view>
</template>

<script>
	import timeLineRow from './timeLine-row.vue'
	import {
		getDataInterfaceRes
	} from '@/api/common'
	export default {
		props: {
			config: {
				type: Object,
				default: () => {}
			}
		},
		components: {
			timeLineRow
		},
		data() {
			return {
				option: {},
				key: +new Date()
			}
		},
		created() {
			this.init()
			uni.$off('proRefresh')
			uni.$on('proRefresh', () => {
				this.initData()
			})
		},
		methods: {
			jump(item) {
				this.jnpf.solveAddressParam(item, this.config)
				this.jnpf.jumpLink(item.urlAddress)
			},
			init() {
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			initData() {
				if (this.config.dataType === "dynamic") {
					if (!this.config.propsApi) return
					const query = {
						paramList: this.config.templateJson
					};
					getDataInterfaceRes(this.config.propsApi, query).then(res => {
						this.config.option.defaultValue = res.data || []
						this.handleAttrs()
					})
				} else {
					this.handleAttrs()
				}
			},
			handleAttrs() {
				if (this.config.option.sortable == 2) this.config.option.defaultValue = this.config.option.defaultValue
					.reverse();
				this.config.option.isCrad = this.config.option.styleType == 2 ? true : false
				this.config.option.isLeft = false
				this.config.option.isVertical = false
				this.config.option.isUpper = false
				if (this.config.option.layout == 1 || this.config.option.layout == 2 || this.config.option.layout ==
					3 || this.config.option.layout == 4) {
					this.config.option.isVertical = true
					if (this.config.option.layout == 3) {
						this.config.option.isLeft = true
					}
				}
				if (!this.config.option.isVertical) {
					if (this.config.option.layout == 5 || this.config.option.layout == 6 || this.config.option
						.layout == 7)
						this.config.option.isUpper = true
				}
				this.config.option.appShowNumber = this.config.option.appShowNumber || 50
				if (this.config.option.appShowNumber && Array.isArray(this.config.option.defaultValue)) {
					this.config.option.defaultValue = this.config.option.defaultValue.slice(0, this.config.option
						.appShowNumber)
				}
				this.option = this.config.option
				this.key = +new Date()
			}
		}
	}
</script>


<style lang="scss">
	.timeLine-v {
		.timeLine {
			width: 100%;
			height: 100%;
			padding: 20rpx;

			.u-time-axis-item {
				.u-time-axis-node {
					top: 0 !important;
				}
			}

			.timeLine-right {
				padding-left: 0;
				padding-right: 40rpx !important;

				&::before {
					left: 670rpx !important;
				}

				.u-time-axis-item {
					.u-time-axis-node {
						left: 670rpx !important;
					}
				}
			}

			.timeLine-dot {
				width: 28rpx;
				height: 28rpx;
				border: 4rpx solid #FFFFFF;
				box-shadow: 0 6rpx 12rpx rgba(2, 7, 28, 0.16);
				border-radius: 50%;
			}

			.timeLine-content {
				padding: 0 10rpx;

				.timeLine-title {
					font-size: 30rpx;
					line-height: 36rpx;

					.name {
						margin-bottom: 6rpx;
					}
				}

				.timeLine-title2 {
					margin-top: 6rpx;
					background: rgba(255, 255, 255, 0.39);
					box-shadow: 0px 3px 10px rgba(0, 0, 0, 0.1);
					padding: 10rpx 20rpx;
					border-radius: 4px;
				}
			}


			.timeLine-desc {
				margin-top: 10rpx;
				font-size: 26rpx;
				line-height: 36rpx;
				color: #909399;
				margin-bottom: 10rpx;
			}
		}
	}
</style>