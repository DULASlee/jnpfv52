<template>
	<view class="alreadySelect">
		<view class="alreadySelect__box u-flex-col">
			<view class="alreadySelect_hd u-flex u-p-l-32 u-p-r-32">
				<view>{{$t('component.jnpf.common.selected')}}</view>
				<view v-if="clearable" @click="clean('all')" style="color: #2979ff;">
					{{$t('component.jnpf.common.clearAll')}}</view>
			</view>
			<view class="select__box u-flex-col" id="box">
				<scroll-view scroll-y="true" style="max-height: 240rpx;">
					<view class="u-flex select__list">
						<view class="u-selectTag u-flex" v-for="(item,index) in list" :key="index">
							<view class="avatar">
								<u-avatar :src="baseURL+item.headIcon" mode="circle" size="mini"
									v-if="item.type==='user'">
								</u-avatar>
								<div class="selected-item-icon" v-else>{{item.fullName.substring(0,1)}}</div>
							</view>
							<view class="u-font-24 select__content">
								<view class="nameSty u-flex">
									<view class="nameUp">{{item.fullName}}</view>
									<u-icon name="close" class="close" @click='clean(index)'></u-icon>
								</view>
								<view class="organizeSty">{{item.organize}}</view>
							</view>
						</view>
					</view>
				</scroll-view>
			</view>
		</view>
	</view>
</template>

<script>
	export default {
		props: {
			clearable: {
				type: Boolean,
				default: false
			},
			selectList: {
				type: Array,
				default () {
					return [];
				}
			},
		},
		data() {
			return {
				list: []
			}
		},
		watch: {
			selectList(val) {
				this.list = val
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			clean(e) {
				if (e === 'all') {
					this.list = [];
				} else {
					this.list.splice(e, 1);
				}
				this.$emit('setSelectList', this.list)
			}
		}
	}
</script>

<style lang="scss">
	.alreadySelect {
		width: 100%;

		.alreadySelect__box {
			.alreadySelect_hd {
				width: 100%;
				height: 60rpx;
				justify-content: space-between;
			}

			.select__box {
				width: 100%;
				justify-content: center;
				padding: 0 10px;

				.select__list {
					justify-content: flex-start;
					flex-wrap: wrap;
					border-bottom: 1rpx solid #c0c4cc;

					.avatar {
						margin-top: 8rpx;
					}

					.selected-item-icon {
						height: 70rpx;
						width: 70rpx;
						background: linear-gradient(193deg, #A7D6FF 0%, #1990FA 100%);
						border-radius: 50%;
						line-height: 70rpx;
						color: #FFFFFF;
						font-size: 28rpx;
						text-align: center;
						margin-bottom: 8rpx;
					}

					.u-selectTag {
						width: calc(50% - 20rpx);
						border: 1px solid #2194fa;
						background-color: #e8f4fe;
						line-height: 40rpx;
						margin: 10rpx;
						padding-left: 10rpx;
						align-items: center;
						border-radius: 8rpx;

						.select__content {
							width: 74%;
							margin-left: 10rpx;

							.nameSty {
								color: #353535;

								.nameUp {
									white-space: nowrap;
									overflow: hidden; //超出的文本隐藏
									text-overflow: ellipsis;
								}

								.close {
									width: 26px;
									justify-content: flex-end;
									color: #2194fa;
									margin-right: 8rpx;
									flex: 1;
								}
							}

							.organizeSty {
								height: 40rpx;
								color: #a0a1a1;
								white-space: nowrap;
								overflow: hidden; //超出的文本隐藏
								text-overflow: ellipsis
							}
						}
					}

					.u-size-default {
						padding: 6rpx 12rpx;
					}
				}
			}
		}
	}
</style>