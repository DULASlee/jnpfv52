<template>
	<view class="u-flex-col buttom-btn-left-inner" @click.stop="handleClick"
		:class="btnType === 'reject' ? 'reject' : ''">
		<u-icon v-if="customIcon" :name="iconName" :size="size" :custom-prefix="btnIcon"></u-icon>
		<u-icon v-else :name="iconName" :size="size"></u-icon>
		<text>{{btnText}}</text>
	</view>
</template>

<script>
	export default {
		props: {
			btnLoading: {
				type: Boolean,
				default: false
			},
			customIcon: {
				type: Boolean,
				default: false
			},
			btnText: {
				type: String,
				default: ''
			},
			btnIcon: {
				type: String,
				default: 'icon-ym icon-ym-add-cancel'
			},
			btnType: {
				type: String,
				default: 'cancel'
			},
			iconName: {
				type: String,
				default: 'icon-ym'
			},
			size: {
				type: [Number, String],
				default: 24
			}
		},
		methods: {
			handleClick() {
				if (this.btnType === 'cancel') return this.jnpf.goBack()
				if (this.btnType === 'more') return this.$emit('handleBtn', this.btnLoading ? false : true)
				if (this.btnType === 'save' || this.btnType === 'reject') return this.$emit('handleBtn')
			}
		}
	}
</script>

<style>
</style>