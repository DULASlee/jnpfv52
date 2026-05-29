<template>
	<view :class="['jnpf-textarea',showCount ? 'jnpf-textarea-count' : '']">
		<u-input input-align='right' :border="false" type="textarea" v-model="innerValue" :placeholder="placeholder"
			:disabled="disabled" @input="onInput" :clearable='disabled ? false : clearable'
			:maxlength="maxlength||maxlength===0?maxlength:9999" />
		<view class="textarea-count" v-if="showCount">
			<text>{{ innerValue?String(innerValue).length:0 }}</text><text v-if="maxlength">/{{ maxlength }}</text>
		</view>
	</view>
</template>
<script>
	export default {
		name: 'jnpf-textarea',
		props: {
			modelValue: {
				type: String,
				default: ''
			},
			placeholder: {
				type: String,
				default: '请输入'
			},
			maxlength: {
				type: [Number, String],
				default: null
			},
			disabled: {
				type: Boolean,
				default: false
			},
			clearable: {
				type: Boolean,
				default: false
			},
			showCount: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				innerValue: ''
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val
				},
				immediate: true,
			}
		},
		methods: {
			onInput(val) {
				this.$emit('update:modelValue', val)
				this.$emit('change', val)
			},
		}
	}
</script>
<style lang="scss">
	.jnpf-textarea {
		position: relative;
		width: 100%;

		&.jnpf-textarea-count {
			padding-bottom: 20rpx;
		}

		.textarea-count {
			color: #909399;
			font-size: 24rpx;
			position: absolute;
			bottom: 4rpx;
			right: 0;
			line-height: 24rpx;
		}
	}
</style>