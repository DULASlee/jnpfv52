<template>
	<u-alert-tips class="jnpf-alert" :type="type" :title="title" :show-icon="showIcon" :description="description"
		:close-able="closable" :close-text="closeText" @close="show=false" :show="show" :title-style="titleStyle"
		:desc-style="descStyle" />
</template>
<script>
	export default {
		name: 'jnpf-alert',
		props: {
			type: {
				type: String,
				default: 'success'
			},
			title: {
				type: String,
				default: '这是一个提示'
			},
			tagIcon: {
				type: String,
				default: 'icon-ym icon-ym-generator-alert'
			},
			showIcon: {
				type: Boolean,
				default: false
			},
			closable: {
				type: Boolean,
				default: false
			},
			description: {
				type: String,
				default: ''
			},
			closeText: {
				type: String,
				default: ''
			}
		},
		data() {
			return {
				show: true,
				titleStyle: {
					'word-break': 'break-all',
					'line-height': '34rpx'
				},
				descStyle: {
					'word-break': 'break-all',
					'line-height': '40rpx',
				},
			}
		}
	}
</script>
<style lang="scss">
	.jnpf-alert {
		width: 100%;
		min-height: 72rpx;
	}
</style>