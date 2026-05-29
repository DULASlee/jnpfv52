<template>
	<view class="" v-if="list.length">
		<view class="doc-list" v-if="modelValue">
			<checkbox-group @change="checkboxChange" @click.stop>
				<label class="item-label" v-for="(item,index) in list" :key="index">
					<view class="u-flex item-label-left u-line-1" @click.stop="goDetail(item)">
						<view class="doc-icon">
							<u-image :src="getRecordImg(item.fileExtension)" width="74" height="74" />
						</view>
						<view class="text">
							<p class="u-m-l-10 u-m-b-8 u-font-28 name u-line-1">{{item.fullName}}</p>
							<p class="u-m-l-10 u-m-t-8 u-font-24 time">
								{{item.time ? $u.timeFormat(item.time, 'yyyy-mm-dd hh:MM:ss'):''}}
							</p>
						</view>
					</view>
					<checkbox :value="item.id" :checked="item.checked" activeBackgroundColor="#0177FF" iconColor="#fff"
						style="transform:scale(0.7)" />
				</label>
			</checkbox-group>
		</view>
		<view class="u-flex u-p-l-20 u-p-r-20 doc-list2 u-p-t-20" v-else>
			<checkbox-group @change="checkboxChange" @click.stop class="checkbox-group">
				<label class="group-label" v-for="(item,index) in list" :key="index">
					<view class="u-flex-col doc-list-inner" @click.stop="goDetail(item)">
						<view class="doc-icon u-flex">
							<u-image :src="getRecordImg(item.fileExtension)" width="84" height="84" />
						</view>
						<view class="u-flex doc-name" @click.stop>
							<view class="u-line-1 name">{{item.fullName}}</view>
							<checkbox :value="item.id" :checked="item.checked" activeBackgroundColor="#0177FF"
								iconColor="#fff" style="transform:scale(0.7)" />
						</view>
					</view>
				</label>
			</checkbox-group>
		</view>
	</view>
</template>
<script>
	import mixin from "../mixin.js"
	export default {
		name: 'DocList',
		mixins: [mixin],
		props: {
			modelValue: {
				type: Boolean,
				default: true
			},
			documentList: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {
				list: []
			}
		},
		watch: {
			documentList: {
				handler(val) {
					this.list = val
				},
				immediate: true,
				deep: true
			}
		},
		methods: {
			goDetail(item) {
				this.$emit('goDetail', item)
			},
			checkboxChange(e) {
				this.$emit('checkboxChange', e.detail.value)
			}
		}
	}
</script>

<style lang="scss">
	.doc-list {
		background-color: #FFFFFF;
		position: relative;
		width: 100%;
		display: flex;
		flex-direction: column;
		padding: 0 20rpx;

		.item-label {
			position: relative;
			display: flex;
			flex-direction: row;
			align-items: center;
			border-bottom: 1rpx solid #f0f2f6;
			height: 126rpx;

			.item-label-left {
				flex: 1;

				.doc-icon {
					width: 74rpx;
					height: 74rpx;

					img {
						width: 100%;
						height: 100%;
					}
				}

				.text {
					width: 80%;

					.name {
						color: #303133;
					}

					.time {
						color: #909399;
					}
				}
			}

			.uni-checkbox-wrapper {
				.uni-checkbox-input {
					margin: 0 !important;
				}
			}
		}
	}

	.doc-list2 {
		background-color: #fff;

		.doc-list-inner {
			width: 100%;
			background-color: #f2f3f7;
			padding: 12rpx 12rpx 0 12rpx;
			margin-bottom: 20rpx;
			border-radius: 8rpx;

			.doc-icon {
				width: 100%;
				background-color: #FFFFFF;
				border-radius: 8rpx;
				height: 160rpx;
				justify-content: center;
			}

			.doc-name {
				width: 100%;
				height: 73rpx;

				.name {
					text-align: left;
					flex: 1;
				}

				::v-deep .uni-checkbox-wrapper {
					.uni-checkbox-input {
						margin: 0;
					}
				}
			}
		}
	}

	.checkbox-group {
		display: flex;
		flex-wrap: wrap;
		width: 100%;
		justify-content: space-between;

		.group-label {
			width: 48%;
		}

		::v-deep .uni-label-pointer {
			width: 48%;
			display: flex;
			text-align: center;
		}
	}
</style>