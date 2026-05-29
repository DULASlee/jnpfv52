<template>
	<view class="opinion">
		<view class="opinion-box">
			<u-input type="textarea" v-model="handleOpinion" placeholder="请输入" :inputBorder='false' @input="onInput"
				class="easyinput" :clearable="true" focus></u-input>
			<view class="u-m-t-10 u-flex opinion-inner" v-if="showCommon">
				<view class="u-flex opinion-l" v-if="commonList.length">
					<text class="u-line-1 common-txt"
						@click.stop="addTextareaValue(0)">{{commonList[0].commonWordsText}}</text>
					<text class="common-txt u-m-l-10 u-line-1"
						@click.stop="addTextareaValue(1)">{{commonList[1].commonWordsText}}</text>
				</view>
				<view class="u-flex opinion-r">
					<text class="txt" @click.stop="addCommonWords">设为常用语</text>
					<view class="icon-box" @click="showCommonList">
						<u-icon name="search" color="#565656" size="28"></u-icon>
					</view>
				</view>
			</view>
		</view>
	</view>
	<CommonList ref="CommonList" @confirm="confirmCommonWord"></CommonList>
</template>

<script>
	import CommonList from './CommonList'
	export default {
		emits: ['addCommonWords', 'update:modelValue'],
		name: 'handle-opinion',
		components: {
			CommonList
		},
		props: {
			modelValue: {
				type: [String, Number]
			},
			commonList: {
				type: Array,
				default: () => []
			},
			showCommon: {
				type: Boolean,
				default: true
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.handleOpinion = val
				},
				immediate: true,
				deep: true
			}
		},
		data() {
			return {
				handleOpinion: ''
			}
		},
		methods: {
			confirmCommonWord(e) {
				this.handleOpinion = e.commonWordsText
				this.$emit('update:modelValue', this.handleOpinion)
			},
			addCommonWords() {
				if (!this.handleOpinion) return this.$u.toast('请输入意见');
				this.$emit('addCommonWords')
			},
			onInput(e) {
				this.$emit('update:modelValue', e)
			},
			addTextareaValue(i) {
				this.handleOpinion += this.commonList[i].commonWordsText
				this.$emit('update:modelValue', this.handleOpinion)
			},
			showCommonList() {
				this.$nextTick(() => {
					this.$refs.CommonList.open();
				})
			}
		}
	}
</script>

<style lang="scss">
	.opinion {
		width: 100%;

		.opinion-box {
			border-radius: 8rpx;
			padding: 0 20rpx 10rpx 20rpx;
			background: #f5f5f5 !important;

			.opinion-inner {


				.opinion-l {
					max-width: 62%;

					.common-txt {
						max-width: 240rpx;
						display: inline-block;
						border: 1rpx dashed #dcdfe6;
						padding: 0 10rpx;
						background-color: #fff;
						color: #303133;
						height: 50rpx;
						line-height: 50rpx;
					}
				}

				.opinion-r {
					flex: 1;
					justify-content: flex-end;

					.txt {
						padding: 0 10rpx;
						color: #0177FF;
					}

					.icon-box {
						width: 60rpx;
						text-align: center;
						margin-left: 12rpx;
					}
				}
			}
		}
	}
</style>