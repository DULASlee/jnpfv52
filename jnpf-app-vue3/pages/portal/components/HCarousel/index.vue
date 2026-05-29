<template>
	<view class="dataBoard-v">
		<view class="dataBoard-box">
			<u-swiper :list="list" v-if="swiper" height="300" mode='rect'
				:indicator-pos="option.carouselIndicatorPosition" :autoplay="option.carouselAutoplay"
				:interval="option.carouselInterval" :img-mode="option.imageFillStyle" :title="true"
				:title-style="option.titleStyle" @click="swiperC" name="imageUrl" :effect3d="option.carouselType">
			</u-swiper>
		</view>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
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
		data() {
			return {
				option: {},
				propsApi: '',
				list: [],
				swiper: false
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
			init() {
				this.option = JSON.parse(JSON.stringify(this.config.option))
				this.option.titleStyle = {
					"text-align": this.option.textLeft,
					"font-size": this.option.textFontSize * 2 + 'rpx',
					"font-weight": this.option.textFontWeight ? 700 : 400,
					'color': this.option.textFontColor,
					'background-color': this.option.textBgColor
				}
				this.option.carouselIndicatorPosition = this.option.carouselIndicatorPosition ===
					"bottomRight" ? "bottomCenter" : this.option.carouselIndicatorPosition ===
					"topLeft" ? "topCenter" : "none"
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			async initData() {
				this.swiper = false
				let val = JSON.parse(JSON.stringify(this.option.appDefaultValue));
				for (let i = 0; i < val.length; i++) {
					let ele = val[i]
					ele.title = ele.textDefaultValue
					if (ele.dataType == 3) {
						const res = await getDataInterfaceRes(ele.propsApi, {})
						ele.imageUrl = res?.data ? res.data : ''
					}
					if (ele.dataType == 1) ele.imageUrl = this.define.baseURL + ele.imageUrl
				}
				this.list = val;
				this.$nextTick(() => {
					this.swiper = true
				})
			},
			swiperC(e) {
				if (this.config.platform === 'mp') return
				let item = this.option.appDefaultValue[e]
				let url = '';
				if (item.linkType == 1 && item.type == 8) {
					let propertyJson = JSON.parse(item.propertyJson)
					uni.navigateTo({
						url: "/pages/portal/scanPortal/index?id=" + propertyJson.moduleId + "&protalType=1" +
							"&fullName=" + item.textDefaultValue || '',
						fail: (err) => {},
					});
					return
				}
				if (item.linkType == '1') {
					let data = {
						id: item.moduleId,
						moduleId: item.moduleId,
						urlAddress: item.urlAddress,
						...JSON.parse(item.propertyJson)
					}
					url = '/pages/apply/dynamicModel/index?config=' +
						this.jnpf.base64.encode(JSON.stringify(data))
					if (item.type == '2') url = item.urlAddress
				} else {
					url = '/pages/apply/externalLink/index?url=' + encodeURIComponent(item.urlAddress)
				}
				uni.navigateTo({
					url: url,
					fail: (err) => {
						this.$u.toast("暂无此页面")
					}
				})
			}
		}
	}
</script>
<style lang="scss">
	.dataBoard-v {
		padding: 20rpx;

		.dataBoard-box {
			margin: 20rpx 0;
		}
	}
</style>