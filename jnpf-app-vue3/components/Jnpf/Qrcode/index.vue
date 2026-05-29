<template>
	<tki-qrcode class="jnpf-qrcode" v-if="qrcode&&showQrcode" ref="qrcode" :cid="cid" :val="qrcode" :size="width*2"
		:background="colorLight" :foreground="colorDark" onval loadMake :showLoading="false" />
</template>
<script>
	import tkiQrcode from "./tki-qrcode/tki-qrcode.vue"
	let unique = 0

	export default {
		name: 'qrcode',
		props: {
			dataType: {
				type: String,
				default: 'static'
			},
			colorLight: {
				type: String,
				default: "#fff"
			},
			colorDark: {
				type: String,
				default: "#000"
			},
			relationField: {
				type: String,
				default: ''
			},
			formData: {
				type: Object
			},
			width: {
				type: Number,
				default: 200
			},
			staticText: {
				type: String,
				default: ''
			}
		},
		components: {
			tkiQrcode
		},
		computed: {
			qrcode() {
				if (this.dataType === 'static') {
					return this.staticText
				} else if (this.dataType === 'relation') {
					return this.relationText?.toString()
				} else {
					if (this.formData && this.dynamicModelExtra && this.dynamicModelExtra.id && this.dynamicModelExtra
						.modelId) {
						const json = {
							t: 'DFD',
							id: this.dynamicModelExtra.id,
							mid: this.dynamicModelExtra.modelId,
							mt: this.dynamicModelExtra.type,
							fid: this.dynamicModelExtra.flowId || '',
							pid: this.dynamicModelExtra.processId || '',
							ftid: this.dynamicModelExtra.taskId || '',
							opt: this.dynamicModelExtra.opType
						}
						return JSON.stringify(json)
					}
					return ''
				}
			},
			dynamicModelExtra() {
				return uni.getStorageSync('dynamicModelExtra') || null
			}
		},
		data() {
			return {
				cid: '',
				relationText: "",
				showQrcode: false
			}
		},
		mounted() {
			this.cid = this.uuid()
			this.showQrcode = true
			uni.$on('updateCode', () => {
				this.showQrcode = false
				this.$nextTick(() => {
					this.showQrcode = true
				})
			})
		},
		watch: {
			formData: {
				handler(val) {
					const relationValue = val[this.relationField] || val[this.relationField] === 0 || val[this
						.relationField] === false ? val[this.relationField] : ''
					if (val && this.dataType === 'relation' && this.relationField) this.relationText = relationValue
				},
				deep: true,
				immediate: true
			},
		},
		methods: {
			uuid() {
				const time = Date.now()
				const random = Math.floor(Math.random() * 1000000000)
				unique++
				return 'qrcode_' + random + unique + String(time)
			}
		},
	}
</script>
<style lang="scss" scoped>
	.jnpf-qrcode {
		width: 100%;
		overflow: hidden;
		margin-bottom: -20rpx;
	}
</style>