<template>
	<view class="HTodo-v" :key="key">
		<view class="HTodo-box u-flex">
			<view class="HTodo-list u-flex" :style="{'flex-wrap':option.appStyleType==1?'nowrap':'wrap'}">
				<view class="u-flex-col HTodo-list-item" v-for="(item,index) in option.appDefaultValue" :key="index"
					@click="jump(item)" :style="option.style">
					<view class="" style="position: relative;margin-bottom: 8rpx;">
						<view :class="item.icon" class="icon"
							:style="{'background-color':item.iconColor||item.iconBgColor || '#008cff'}"></view>
					</view>
					<view class="u-line-1 title"
						:style="{'font-size':option.labelFontSize,'color':option.labelFontColor,'font-weight':option.labelFontWeight?700:400}">
						{{item.fullName}}
					</view>
				</view>
			</view>
		</view>
	</view>
</template>
<script>
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
				key: +new Date()
			}
		},
		created() {
			this.initData()
		},
		methods: {
			initData() {
				this.option = JSON.parse(JSON.stringify(this.config.option))
				this.option.appDefaultValue.map((o) => {
					if (o.fullNameI18nCode) o.fullName = this.$t(o.fullNameI18nCode)
				})
				let style;
				style = {
					'width': '240rpx'
				}
				if (this.option.appStyleType == 2) {
					style = {
						'width': this.option.appDefaultValue.lenght < 2 ? "" : 100 / this.option.appRowNumber + '%'
					}
					if (this.option.appShowBorder) {
						style['border-right'] = '2rpx solid #f0f2f6'
						style['border-bottom'] = '2rpx solid #f0f2f6'
					}
				}
				this.option.style = style
				this.option.labelFontSize = this.option.labelFontSize * 2 + 'rpx'
				this.key = +new Date()
			},
			jump(item) {
				if (this.config.platform === 'mp') return
				let url;
				if (item.linkType == 1 && item.type == 3) {
					let data = {
						id: item.moduleId,
						moduleId: item.moduleId,
						urlAddress: item.urlAddress,
						...JSON.parse(item.propertyJson)
					}
					url = '/pages/apply/dynamicModel/index?config=' +
						this.jnpf.base64.encode(JSON.stringify(data))
				} else if (item.linkType == 1 && item.type == 2) {
					url = item.urlAddress
				} else if (item.linkType == 1 && item.type == 8) {
					let propertyJson = JSON.parse(item.propertyJson)
					uni.navigateTo({
						url: "/pages/portal/scanPortal/index?id=" + propertyJson.moduleId +
							"&portalType=1&fullName=" +
							item.fullName,
						fail: (err) => {},
					});
					return
				} else {
					if (!item.urlAddress) return
					url = '/pages/apply/externalLink/index?url=' + encodeURIComponent(item.urlAddress) +
						'&fullName= ' + item.fullName
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
	.HTodo-v {
		width: 100%;
		height: 100%;

		.HTodo-box {
			width: 100%;
			overflow-x: scroll;

			.HTodo-list {
				width: 100%;

				.HTodo-list-item {
					align-items: center;
					padding: 16rpx 20rpx;

					.title {
						width: 100%;
						text-align: center;
					}

					.icon {
						width: 90rpx;
						height: 90rpx;
						font-size: 60rpx;
						color: #fff;
						text-align: center;
						line-height: 90rpx;
						border-radius: 16rpx;
					}
				}
			}
		}
	}
</style>