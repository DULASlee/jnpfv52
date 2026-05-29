<template>
	<view class="jnpf-signature" :class="align=='right'?'flex-end':'flex-start'">
		<view class="jnpf-signature-box">
			<template v-if="showBtn">
				<image class="jnpf-signature-img" :src="baseURL+innerValue" mode="scaleToFill" v-show="innerValue"
					@tap.stop="handlePreviewImage(baseURL+innerValue)" />
				<view class="jnpf-signature-btn" v-if="!detailed" @click="open()">
					<i class="icon-ym icon-ym-signature1" />
					<view class="title" v-if="!innerValue">电子签章</view>
				</view>
			</template>
		</view>
		<MultSelect :show="show" :list="options" @confirm="confirm" @close="show = false" :default-value="defaultValue"
			filterable />
	</view>
</template>
<script>
	import {
		getListByIds
	} from '@/api/signature.js'
	import MultSelect from '@/components/MultSelect'
	export default {
		name: 'jnpf-sign',
		components: {
			MultSelect
		},
		props: {
			modelValue: {
				type: [String, Number, Boolean],
			},
			disabled: {
				type: Boolean,
				default: false
			},
			detailed: {
				type: Boolean,
				default: false
			},
			showBtn: {
				type: Boolean,
				default: true
			},
			align: {
				type: String,
				default: 'right'
			},
			ableIds: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {
				innerValue: '',
				show: false,
				options: [],
				defaultValue: []
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val || ''
				},
				immediate: true,
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			getListByIds() {
				getListByIds({
					'ids': this.ableIds
				}).then(res => {
					this.options = res.data.list || []
					const index = this.options.findIndex(o => this.innerValue === o.icon)
					if (index > -1) this.defaultValue = [this.options[index].id]
					this.show = true
				})
			},
			open() {
				if (this.disabled) return
				if (!this.ableIds.length) return this.show = true
				if (this.ableIds.length) this.getListByIds()
			},
			confirm(val) {
				if (!val.length) return
				this.innerValue = val[0].icon || ''
				this.$emit('update:modelValue', this.innerValue)
				this.$emit('change', val[0])
			},
			handlePreviewImage(url) {
				// #ifdef H5
				uni.previewImage({
					urls: [url],
					current: url,
					success: () => {},
					fail: () => {
						uni.showToast({
							title: '预览图片失败',
							icon: 'none'
						});
					}
				});
				// #endif
			}
		}
	}
</script>
<style scoped lang="scss">
	.jnpf-signature {
		width: 100%;
		display: flex;

		align-items: center;

		&.flex-end {
			justify-content: flex-end;
		}

		&.flex-start {
			justify-content: flex-start;
		}

		.jnpf-signature-box {
			display: flex;
		}

		.jnpf-signature-img {
			width: 160rpx;
			height: 80rpx;
			flex-shrink: 0;
		}

		.jnpf-signature-btn {
			color: #2188ff;
			width: 100%;
			display: flex;
			flex-shrink: 0;

			.icon-ym-signature1 {
				font-size: 52rpx;
			}

			.title {
				font-size: 28rpx;
			}
		}


	}
</style>