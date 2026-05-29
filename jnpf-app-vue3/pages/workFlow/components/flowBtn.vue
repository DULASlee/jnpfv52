<template>
	<view class="flowBefore-actions" :style="{'height': '88rpx'}"
		v-if="hasComment ||  (actionList.length || rightBtnList.length) ">
		<CustomButton v-if="showMoreBtn" btnText="更多" btnType="more" iconName="more-dot-fill" size="28"
			@handleBtn="showAction = $event" :btnLoading="loading" class="u-flex-col buttom-btn-left-inner" />
		<template v-if="opType != 2">
			<CustomButton v-if="showSaveBtn" :btnText="saveBtnText || '暂存'" customIcon
				class="u-flex-col buttom-btn-left-inner" btnIcon="icon-ym icon-ym-extend-save" size="28"
				@handleBtn="handleBtn('save')" btnType="save" :btnLoading="loading" />
			<CustomButton v-if="showRejectBtn" btnText="拒绝" iconName="minus-circle" size="28"
				@handleBtn="handleBtn('reject')" btnType="reject" :btnLoading="loading"
				class="u-flex-col buttom-btn-left-inner" />
		</template>
		<view class="rightBtn u-flex">
			<u-button class="buttom-btn" @click.stop="handleBtn('comment')" v-if="!rightBtnList.length"
				:loading="loading">
				评论
			</u-button>
			<template v-if="opType != 2">
				<u-button v-for="btn,index in rightBtnList" :key="index" class="buttom-btn"
					@click.stop="handleBtn(btn.id)" :style="{'width':btn.width}"
					:class="{'delegate-submit-btn':btn.id === 'delegateSubmit'}" :loading="loading" :type="btn?.type">
					{{ btn.fullName}}
				</u-button>
			</template>
			<template class="" v-else>
				<u-button v-for="btn,index in todoBtnList" :key="index" class="buttom-btn"
					@click.stop="handleBtn(btn.id)" :style="{'width':btn.width}" :loading="loading" :type="btn?.type">
					{{ btn.fullName}}
				</u-button>
			</template>
		</view>
		<u-action-sheet v-model="showAction" :list="actionList" @click="handleAction" />
	</view>
</template>
<script>
	import CustomButton from '@/components/CustomButton'
	export default {
		components: {
			CustomButton
		},
		props: {
			actionList: {
				type: Array,
				default: () => ([])
			},
			rightBtnList: {
				type: Array,
				default: () => ([])
			},
			todoBtnList: {
				type: Array,
				default: () => ([])
			},
			btnInfo: {
				type: Object,
				default: () => ({})
			},
			opType: {
				type: [String, Number],
				default: ''
			},
			saveBtnText: {
				type: String,
				default: ''
			},
			btnLoading: {
				type: Boolean,
				default: false
			},
			hideSaveBtn: {
				type: Boolean,
				default: false
			},
			hasSignFor: {
				type: Boolean,
				default: false
			},
			hasComment: {
				type: Boolean,
				default: false
			},
			isProcessing: {
				type: [String, Number],
				default: ''
			}
		},
		computed: {
			loading() {
				return this.btnLoading
			},
			showMoreBtn() {
				if (this.actionList.length) return true
				return false
			},
			showRejectBtn() {
				if (this.btnInfo?.hasRejectBtn && !this.isProcessing && this.opType != 2) return true
				return false
			},
			showSaveBtn() {
				if (this.btnInfo?.hasSaveBtn && !this.hideSaveBtn) return true
				return false
			}
		},
		data() {
			return {
				showAction: false
			}
		},
		methods: {
			handleBtn(type, item) {
				if (!this.loading) this.$emit('handleBtn', type, item)
			},
			//更多按钮
			handleAction(index) {
				this.handleBtn(this.actionList[index].id, this.actionList[index])
			}
		}
	}
</script>

<style lang="scss">
	.flowBefore-actions {
		.poup-btn {
			background-color: #fff;
			width: 100%;
			height: 88rpx;
			padding: 0 20rpx;
			color: #7f7f7f;
			border-bottom: 1rpx solid #d7d7d7;

			.btn {
				background-color: #f2f2f2;
				border-radius: 8rpx;
				height: 60rpx;
				padding-left: 10rpx;
				width: 100%;
				line-height: 60rpx;
			}
		}
	}

	.delegate-submit-btn {
		background-color: #50abf6;

		button {
			background-color: #50abf6 !important;
		}
	}
</style>