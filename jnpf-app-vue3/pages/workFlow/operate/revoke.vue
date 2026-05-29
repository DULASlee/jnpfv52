<template>
	<view class="flow-popup-content">
		<u-form ref="dataForm" :model="dataForm" :label-width="150" :errorType="['toast']">
			<view class="content">
				<u-form-item label="撤销原因">
					<HandleOpinion v-model="dataForm.handleOpinion" :showCommon="false"></HandleOpinion>
				</u-form-item>
			</view>
		</u-form>
		<view class="flowBefore-actions">
			<view class="u-flex-col buttom-btn-left-inner" @click.stop="jnpf.goBack()">
				<u-icon name="icon-ym" size="24" custom-prefix="icon-ym icon-ym-add-cancel"></u-icon>
				<text>取消</text>
			</view>
			<u-button class="buttom-btn" type="primary" @click="confirm('confirm')">确定
			</u-button>
		</view>
	</view>
</template>
<script>
	import HandleOpinion from './components/HandleOpinion.vue'
	import {
		getSelector,
		Create
	} from '@/api/commonWords'
	export default {
		components: {
			HandleOpinion
		},
		data() {
			return {
				dataForm: {
					handleOpinion: ""
				},
				config: {}
			};
		},

		onLoad(data) {
			try {
				this.config = JSON.parse(decodeURIComponent(data.config));
			} catch {
				this.config = JSON.parse(data.config);
			}
		},

		methods: {
			confirm() {
				let data = {
					...this.dataForm,
					eventType: this.config.type
				}
				uni.$emit('operate', data)
				setTimeout(() => {
					uni.navigateBack()
				}, 500)
			}
		}
	}
</script>

<style lang="scss">
	page {
		height: 100%;
		background-color: #fff !important;
	}

	::v-deep .u-form-item--left {
		align-items: flex-start !important;
	}

	.buttom-btn-left-inner {
		width: 50%;
	}

	.flow-popup-content {
		.signature-box {
			border-top: none;
		}

		.content {
			padding: 0 20rpx;
		}
	}
</style>