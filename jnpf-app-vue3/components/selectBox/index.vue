<template>
	<view class="u-line-1 u-flex select-box" @click="openSelect">
		<view class="u-flex content-box">
			<view class="content" :style="{textAlign:textAlign}">
				<text class="u-line-1" v-if="modelValue">{{modelValue}}</text>
				<text v-else class="placeholder-style">{{placeholder}}</text>
			</view>
		</view>
		<view class="u-input__right-icon--select u-input__right-icon__item">
			<u-icon :name="selectOpen ? 'arrow-up' : 'arrow-right'" size="26" color="#c0c4cc"></u-icon>
		</view>
	</view>
</template>

<script>
	export default {
		props: {
			modelValue: {
				default: ''
			},
			textAlign: {
				type: String,
				default: 'end'
			},
			placeholder: {
				type: String,
				default: '请选择'
			},
			selectOpen: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {

			}
		},
		methods: {
			openSelect() {
				this.$emit('openSelect')
			}
		}
	}
</script>

<style lang="scss">
	.select-box {
		height: 78rpx;
		width: 100%;
		justify-content: space-between;

		.content-box {
			width: 100%;
			justify-content: flex-end;

			.content {
				width: 500rpx;
				overflow-y: scroll;

				.placeholder-style {
					color: rgb(192, 196, 204);
				}
			}
		}
	}
</style>