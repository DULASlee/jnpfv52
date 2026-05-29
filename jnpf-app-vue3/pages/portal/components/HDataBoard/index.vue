<template>
	<view class="HTodo-v" :key="key">
		<view class="HTodo-box u-flex">
			<view class="HTodo-list u-flex" :style="{'flex-wrap':option.appStyleType==1?'nowrap':'wrap'}">
				<view class="u-flex-col HTodo-list-item" v-for="(item,index) in option.appDefaultValue" :key="index"
					@click="jump(item)" :style="option.style">
					<view class=" u-m-b-8">
						<view :class="item.icon" class="icon"
							:style="{'background-color':item.iconColor||item.iconBgColor || '#008cff'}"></view>
					</view>
					<view class="u-line-1 title"
						:style="{'font-size':option.labelFontSize,'color':option.labelFontColor,'font-weight':option.labelFontWeight?700:400}">
						{{item.fullName}}
					</view>
					<view class="u-line-1 title"
						:style="{'font-size':option.valueFontSize,'color':option.valueFontColor,'font-weight':option.valueFontWeight?700:400}">
						{{item.num+' '}}
						<text
							:style="{'font-size':option.unitFontSize,'color':option.unitFontColor,'font-weight':option.unitFontWeight?700:400}">{{item.unit}}</text>
					</view>
				</view>
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
				propsApi: '',
				key: +new Date(),
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
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			initData() {
				this.option = JSON.parse(JSON.stringify(this.config.option))
				this.option.appDefaultValue.map((o) => {
					if (o.fullNameI18nCode) o.fullName = this.$t(o.fullNameI18nCode)
				})
				let style;
				style = {
					'width': '238rpx'
				}
				if (this.option.appStyleType == 2) {
					style.width = 100 / this.option.appRowNumber + '%'
					if (this.option.appShowBorder) {
						style['border-right'] = '2rpx solid #f0f2f6'
						style['border-bottom'] = '2rpx solid #f0f2f6'
					}
				}
				this.option.style = style
				this.option.appDefaultValue = this.option.appDefaultValue.filter((o) => !o.noShow)
				if (this.config.dataType == 'dynamic') {
					for (let i = 0; i < this.option.appDefaultValue.length; i++) {
						this.option.appDefaultValue[i].num = '';
					}
					if (!this.config.propsApi) return;
					let list = this.option.appDefaultValue || []
					const query = {
						paramList: this.config.templateJson
					};
					getDataInterfaceRes(this.config.propsApi, query).then(res => {
						for (let i = 0; i < this.option.appDefaultValue.length; i++) {
							const list = this.option.appDefaultValue[i]
							list.num = list.field ? res.data[list.field] : '';
						}
						this.handleAttrs()
					})
				} else {
					this.handleAttrs()
				}
			},
			handleAttrs() {
				this.option.valueFontSize = this.option.valueFontSize * 2 + 'rpx'
				this.option.unitFontSize = this.option.unitFontSize * 2 + 'rpx'
				this.key = +new Date()
			},
			jump(item) {
				this.jnpf.solveAddressParam(item, this.config)
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
						url: "/pages/portal/scanPortal/index?id=" + propertyJson.moduleId + "&protalType=1" +
							"&fullName=" + item.fullName,
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