<template>
	<view class="image-v">
		<view class="custom-cover" @click="jump()">
			<image class="cover-image" :mode="option.imageFillStyle" :src="imgUrl"></image>
			<view class="cover-content"
				:style="{'justify-content':option.textLeft,'background-color':option.textBgColor}">
				<text class=" uni-subtitle uni-white"
					:style="{'text-decoration':option.textUnderLine,'color':option.textFontColor,'font-weight':option.textFontWeight?700:0,'font-size':option.textFontSize}">{{option.textDefaultValue}}</text>
			</view>
		</view>
	</view>
</template>

<script>
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
				imgUrl: ''
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
				this.option.imageFillStyle = this.option.imageFillStyle === 'fill' ? 'scaleToFill' : this.option
					.imageFillStyle === 'cover' ? ' aspectFill' : 'aspectFit'
				this.option.textFontSize = this.option.textFontSize * 2 + 'rpx'
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			initData() {
				if (this.option.styleType == 1) this.imgUrl = this.define.baseURL + this.option.defaultValue
				if (this.option.styleType == 2) this.imgUrl = this.option.defaultValue
				if (this.config.dataType === "dynamic") {
					if (!this.config.propsApi) return
					const query = {
						paramList: this.config.templateJson
					};
					getDataInterfaceRes(this.config.propsApi, query).then(res => {
						this.imgUrl = toString.call(res.data) === `[object String]` ? res.data : ''
					})
				}
			},
			jump() {
				if (this.config.platform === 'mp') return
				let url = '';
				if (this.config.option.appLinkType == 1 && this.config.option.appType == 3) {
					let data = {
						id: this.config.option.appModuleId,
						moduleId: this.config.option.appModuleId,
						urlAddress: this.config.option.appUrlAddress,
						...JSON.parse(this.config.option.propertyJson)
					}
					url = '/pages/apply/dynamicModel/index?config=' +
						this.jnpf.base64.encode(JSON.stringify(data))
				} else if (this.config.option.appLinkType == 1 && this.config.option.appType == 2) {
					url = this.config.option.appUrlAddress
				} else if (this.config.option.appLinkType == 1 && this.config.option.appType == 8) {
					let propertyJson = JSON.parse(this.config.option.appPropertyJson)
					uni.navigateTo({
						url: "/pages/portal/scanPortal/index?id=" + propertyJson.moduleId + "&protalType=1" +
							"&fullName=" + this.config.option.textDefaultValue || '',
						fail: (err) => {},
					});
					return
				} else {
					if (!this.config.option.appUrlAddress) return
					url = '/pages/apply/externalLink/index?url=' + encodeURIComponent(this.config.option.appUrlAddress) +
						'&fullName= ' + this.config.option.fullName
				}
				uni.navigateTo({
					url: url,
					fail: (err) => {
						// this.$u.toast("暂无此页面")
					}
				})
			},
		}
	}
</script>


<style lang="scss">
	.image-v {
		.custom-cover {
			display: flex;
			width: 100%;
			flex: 1;
			flex-direction: row;
			position: relative;
			margin: 20rpx 0;

			.cover-image {
				width: 100%;
			}

			.cover-content {
				position: absolute;
				bottom: 0;
				left: 0;
				right: 0;
				height: 40px;
				display: flex;
				flex-direction: row;
				align-items: center;
				padding-left: 15px;
			}
		}
	}
</style>