<template>
	<view class="video-v">
		<view class="uni-padding-wrap uni-common-mt">
			<view v-if="option.defaultValue && !Array.isArray(option.defaultValue)" class="video-box">
				<video id="myVideo" :src="option.defaultValue" object-fit="fill" :loop="option.playNumber"
					:autoplay="option.videoAutoplay" :muted="option.mutePlay" v-show="showVideo" :key="key" />
			</view>
			<view class="nodata" v-else>
				<u-image width="100px" height="100px" :src="nodata" class="img" />
				<text>暂无视频</text>
			</view>
		</view>
	</view>
</template>
<script>
	import {
		getDataInterfaceRes
	} from '@/api/common'
	import nodata from '@/pages/portal/static/image/portal-nodata.png'
	export default {

		props: {
			config: {
				type: Object,
				default: () => {}
			}
		},
		data() {
			return {
				title: 'video',
				src: '',
				danmuValue: '',
				showVideo: true,
				autoplay: true,
				option: {},
				key: +new Date(),
				nodata
			}
		},
		onReady: function(res) {
			// #ifndef MP-ALIPAY
			this.videoContext = uni.createVideoContext('myVideo')
			// #endif
		},
		created() {
			this.init()
			uni.$off('proRefresh')
			uni.$on('proRefresh', () => {
				this.initData()
			})
			// #ifdef APP-PLUS
			uni.$on('showVideo', (show) => {
				this.showVideo = show
				this.key = +new Date()
			})
			// #endif
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
		},
		methods: {
			init() {
				this.config.option.playNumber = this.config.option.playNumber == 1 ? false : true
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			initData() {
				this.option = this.config.option
				if (this.config.dataType === "dynamic") {
					if (!this.config.propsApi) return
					const query = {
						paramList: this.config.templateJson
					};
					getDataInterfaceRes(this.config.propsApi, query).then(res => {
						this.config.option.defaultValue = res.data
						this.option = this.config.option
					})
				} else {
					if (this.config.option.styleType == 1) {
						this.option.defaultValue = this.baseURL + this.config.option.defaultValue.url
					}
				}
			}
		}
	}
</script>
<style lang="scss">
	.video-v {
		padding: 20rpx;
		width: 100%;
		height: auto;

		.video-box {
			width: 100%;
			height: auto;
		}
	}

	.nodata {
		width: 100%;
		height: 100%;
		text-align: center;

		.img {
			margin: 0 auto;
		}
	}

	video {
		width: 100%;
	}
</style>