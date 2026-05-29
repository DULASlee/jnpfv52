<template>
	<view class="card-v">
		<uni-card padding="0px" margin="0px" spacing="0px" :border="false">
			<template #title v-if="cardData.title">
				<view class="u-flex card-content" :style="{'background-color':cardData.card.titleBgColor}">
					<view class="u-flex left"
						:style="{'justify-content':cardData.card.titleLeft==='left'?'flex-start':cardData.card.titleLeft==='right'?'flex-end':'center'}">
						<view class="u-flex">
							<view :class="cardData.card.cardIcon?cardData.card.cardIcon:'icon'"
								:style="{'color':cardData.card.cardIconColor}"></view>
							<view class="txt u-line-1"
								:style="{'color':cardData.card.titleFontColor,'font-size':cardData.card.titleFontSize*2+'rpx','font-weight':cardData.card.titleFontWeight?700:400}">
								{{cardData.title}}
							</view>
							<span v-if="cardData.viceTitle" class="u-m-l-10"
								:style="{'color':cardData.card.viceTitleFontColor,'font-size':cardData.card.viceTitleFontSize*2+'rpx'}">{{cardData.viceTitle}}</span>
						</view>
					</view>
					<view class="link" @click="jump">
						<view class="u-line-1" style="color: #2979ff;">
							{{cardData.card.cardRightBtn}}
						</view>
					</view>
				</view>
			</template>
			<view class="card-actions">
				<slot name="content"></slot>
			</view>
		</uni-card>
	</view>
</template>

<script>
	export default {
		props: {
			cardData: {
				type: Object,
				default: () => {
					return {
						'title': "",
						'viceTitle': '',
						'card': {
							'titleBgColor': "#fff",
							'cardIcon': "",
							'titleFontColor': '#6a6a6a',
							'viceTitleFontColor': '#606266',
							'titleFontSize': 14,
							'viceTitleFontSize': 12,
							'titleFontWeight': false,
							'cardRightBtn': '',
							'titleLeft': 'left'
						}
					}
				}
			}
		},
		methods: {
			jump() {
				if (this.cardData.platform === 'mp') return
				let url;
				if (!this.cardData.card.appLinkType) return
				if (this.cardData.card.appLinkType == 1 && this.cardData.card.appType == 8) {
					let propertyJson = JSON.parse(this.cardData.card.appPropertyJson)
					uni.navigateTo({
						url: "/pages/portal/scanPortal/index?id=" + propertyJson.moduleId + "&protalType=1" +
							"&fullName=" + this.cardData.card.cardRightBtn || '',
						fail: (err) => {},
					});
					return
				}
				if (this.cardData.card.appLinkType == 1) {
					let data = {
						id: this.cardData.card.appModuleId,
						moduleId: this.cardData.card.appModuleId,
						urlAddress: this.cardData.card.appUrlAddress,
						...JSON.parse(this.cardData.card.appPropertyJson)
					}
					if (this.cardData.card.appType == 3) {
						url = '/pages/apply/dynamicModel/index?config=' +
							this.jnpf.base64.encode(JSON.stringify(data))
					} else if (this.cardData.card.appType == 2) {
						url = this.cardData.card.appUrlAddress + '?menuId=' + this.cardData.card.appModuleId
					} else {
						url = '/pages/apply/externalLink/index?url=' + encodeURIComponent(this.cardData.card.appUrlAddress)
					}
				} else {
					url = '/pages/apply/externalLink/index?url=' + encodeURIComponent(this.cardData.card.appUrlAddress)
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
	.card-v {
		::v-deep .uni-card--shadow {
			box-shadow: none !important;
			border: 2rpx solid #f0f2f6;
		}

		.card-content {
			padding: 0rpx 20rpx;
			width: 100%;
			min-height: 90rpx;
			box-sizing: border-box;
			border-bottom: 2rpx solid #f0f2f6;

			.left {
				flex: 1;
			}

			.center {
				line-height: 40rpx;
				flex: 1;
			}

			.icon {
				border-left: 4px solid #1890ff;
				margin-top: 2px;
			}

			.txt {
				max-width: 437rpx;
				height: 100%;
				padding-left: 16rpx;

			}

			.link {
				min-width: 60rpx;
				max-width: 232rpx;
				padding: 0 10rpx;
				text-align: center;
			}

		}
	}
</style>