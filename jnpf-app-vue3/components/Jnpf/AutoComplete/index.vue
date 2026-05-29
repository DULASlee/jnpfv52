<template>
	<view class="jnpf-auto-complete">
		<u-input input-align='right' type="select" @click="showSearch" v-model="innerValue"
			:placeholder="placeholder" />
		<SearchForm ref="searchForm" :interfaceId="interfaceId" :relationField="relationField"
			:templateJson="templateJson" @confirm="confirm" :total="total || 50" :formData="formData"
			:clearable="clearable" :rowIndex="rowIndex" />
	</view>
</template>
<script>
	import SearchForm from './SearchForm';
	export default {
		name: 'jnpf-auto-complete',
		components: {
			SearchForm
		},
		props: {
			modelValue: {
				default: ''
			},
			formData: {
				type: Object
			},
			options: {
				type: Array,
				default: () => []
			},
			placeholder: {
				type: String,
				default: '请输入'
			},
			clearable: {
				type: Boolean,
				default: false
			},
			disabled: {
				type: Boolean,
				default: false
			},
			templateJson: {
				type: Array,
				default: () => []
			},
			interfaceId: {
				type: String,
				default: ''
			},
			relationField: {
				type: String,
				default: 'fullName'
			},
			total: {
				type: Number,
				default: 50
			},
			rowIndex: {
				default: null
			}
		},
		data() {
			return {
				innerValue: ''
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val || ''
				},
				immediate: true
			}
		},
		methods: {
			showSearch() {
				if (this.disabled) return
				this.$nextTick(() => {
					this.$refs.searchForm.init(this.innerValue)
				})
			},
			confirm(e) {
				this.innerValue = e
				this.$emit('update:modelValue', e);
				this.$emit('change', e);
			}
		}
	}
</script>

<style lang="scss">
	.jnpf-auto-complete {
		width: 100%;

		.str-auto-complete-container {
			width: 549rpx;
			height: 360px;
			border-radius: 8rpx;
			box-shadow: 0rpx 0rpx 12rpx #dfe3e9;
			position: absolute;
			z-index: 9997;
			background: #fff;
			top: 94rpx;
			left: 0;
			right: 0;
			overflow-y: scroll;

			.str-auto-complete-mask {
				width: 100%;
				height: calc(100% - 90rpx);
				position: fixed;
				left: 0;
				top: 0;
				bottom: 0;
				right: 0;
				z-index: 9999;
			}

			.str-auto-complete-item {
				position: relative;
				padding: 10rpx;
				z-index: 9999
			}
		}

		.auto-complete-b {
			width: 549rpx;
			height: 360px;
			z-index: 999;
			position: absolute;
			background-color: #fff;
			border: 1px solid red;
			top: 94rpx;
			left: 0;
			right: 0;
		}
	}
</style>