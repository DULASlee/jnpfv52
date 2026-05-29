<template>
	<view class="page_v u-flex-col">
		<view class="lists_box" v-if="show" v-for="(item,index) in list" :key="index"
			:class="item.isDefault ? 'active' : '' " @click="clickRadio(item)">
			<view class="icon-checked-box" v-if="item.isDefault">
				<text>{{majorType === 'Organize' ? '默认' : '主岗'}}</text>
				<view class="icon-checked">
					<u-icon name="checkbox-mark" color="#fff" size="28"></u-icon>
				</view>
			</view>
			<view class="list_inner">
				<text class="icon-ym"
					:class="majorType === 'Organize' ? 'icon-ym-organization' : 'icon-ym-wf-outgoingApply'"></text>
				<text class="txt">{{item.fullName}}</text>
			</view>
		</view>
		<view v-if="!show" class="notData-box u-flex-col">
			<view class="u-flex-col notData-inner">
				<image :src="icon" mode="" class="iconImg"></image>
				<text class="notData-inner-text">{{$t('common.noData')}}</text>
			</view>
		</view>
	</view>
</template>

<script>
	import {
		getUserOrganizes,
		getUserPositions,
		setMajor,
		getCurrentUser
	} from '@/api/common.js'
	import resources from '@/libs/resources.js'
	import {
		useUserStore
	} from '@/store/modules/user'

	export default {
		data() {
			return {
				value: '',
				list: [],
				organizeList: [],
				majorType: '',
				icon: resources.message.nodata,
				oldVal: "",
				show: true,
				disabled: false
			}
		},
		onLoad(e) {
			this.majorType = e.majorType
			this.getUserOrganizes(this.majorType)
			let title = this.majorType === 'Organize' ? this.$t('app.my.organization') : this.$t('app.my.position')
			uni.setNavigationBarTitle({
				title: title
			})
		},
		methods: {
			getUserOrganizes(majorType) {
				let method = majorType === 'Organize' ? getUserOrganizes : getUserPositions
				method().then(res => {
					let data = res.data || []
					if (data.length === []) {
						this.show = this.list.length > 0 ? true : false
						return
					}
					this.organizeList = JSON.parse(JSON.stringify(data));
					this.list = this.organizeList;
					this.list.map(o => {
						if (o.isDefault) {
							this.value = o.id
							this.oldVal = o.id
							return
						}
					})
					this.show = this.list.length > 0 ? true : false
				})
			},
			clickRadio(item) {
				if (this.disabled || item.isDefault) return
				this.changeMajor(item.id)
			},
			change(id) {
				this.value = id
				this.list.map((o, i) => {
					o.isDefault = false;
					if (o.id === id) o.isDefault = true;
				})
			},
			changeMajor(majorId) {
				let query = {
					majorId,
					majorType: this.majorType
				}
				setMajor(query).then(res => {
					if (res.code === 200) {
						this.value = majorId
						this.disabled = true
						this.$u.toast('修改成功')
						this.$nextTick(() => {
							this.change(majorId)
						})
						this.getCurrentUser()
					}
				}).catch(() => {
					this.value = this.oldVal
				})
			},
			getCurrentUser() {
				const userStore = useUserStore()
				userStore.getCurrentUser().then(() => {
					setTimeout(() => {
						uni.navigateBack()
					}, 500)
				})
			},
		}
	}
</script>

<style lang="scss" scoped>
	page {
		background-color: #f0f2f6;
	}

	.page_v {
		/* #ifdef MP */
		background-color: #f0f2f6;
		/* #endif */
		height: calc(100vh - 88rpx);
		padding: 0 20rpx;

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


		.active {
			border: 1rpx solid #2979FF;
			color: #2979FF;

			.icon-ym-organization {
				&::before {
					color: #2979FF !important;
				}
			}
		}

		.lists_box {
			width: 100%;
			height: 180rpx;
			border-radius: 8rpx;
			position: relative;
			display: flex;
			flex-direction: column;
			justify-content: center;
			background-color: #FFFFFF;
			margin-top: 20rpx;

			.icon-checked-box {
				display: flex;
				width: 140rpx;
				height: 80rpx;
				position: absolute;
				transform: scale(0.9);
				right: -4rpx;
				bottom: -2rpx;
				flex-direction: row;
				align-items: center;


				.icon-checked {
					width: 44rpx;
					height: 44rpx;
					border: 40rpx solid #1890ff;
					border-left: 40rpx solid transparent;
					border-top: 40rpx solid transparent;
					border-bottom-right-radius: 12rpx;
					position: absolute;
					transform: scale(0.95);
					right: -8rpx;
					bottom: -6rpx;
				}
			}

			.list_inner {
				width: 100%;
				display: flex;
				flex-direction: row;
				padding: 0 40rpx;
				align-items: center;

				.icon-ym-wf-outgoingApply {
					&::before {
						margin-right: 6rpx;
						font-size: 40rpx;
					}
				}

				.icon-ym-organization {
					&::before {
						margin-right: 6rpx;
						font-size: 40rpx;
						color: #606266;
					}
				}

				.txt_icon {}

				.txt {
					width: 100%;
					align-items: flex-end;
					word-wrap: break-word;
				}
			}
		}
	}
</style>