<template>
	<u-form class="jnpf-wrap-form" :model="formData" ref="dataForm"
		:label-position="formConf.labelPosition==='top'?'top':'left'"
		:label-align="formConf.labelPosition==='right'?'right':'left'"
		:label-width="formConf.labelWidth?formConf.labelWidth*1.5:150" :class='formConf.className'>
		<template v-for="(item, index) in formConf.fields" :key="item.__config__.renderKey">
			<Item :itemData="item" :formConf="formConf" :class="item.__config__.className" :formData="formData"
				:ref="item.__vModel__?item.__vModel__: undefined" @toDetail="toDetail" @clickIcon='clickIcon' />
		</template>
		<u-modal v-model="show" :content="content" width='70%' border-radius="16" :content-style="contentStyle"
			:titleStyle="titleStyle" :confirm-style="confirmStyle" :title="title" :confirm-text="$t('common.okText')">
		</u-modal>
	</u-form>
</template>
<script>
	import Item from './Item'
	export default {
		components: {
			Item
		},
		props: {
			formConf: {
				type: Object,
				required: true
			},
			formData: {
				type: Object,
			},
			loading: {
				type: Boolean,
				default: false
			}
		},
		data() {
			return {
				show: false,
				content: '',
				contentStyle: {
					fontSize: '28rpx',
					padding: '20rpx',
					lineHeight: '44rpx',
					textAlign: 'left'
				},
				titleStyle: {
					padding: '20rpx'
				},
				confirmStyle: {
					height: '80rpx',
					lineHeight: '80rpx',
				},
				title: this.$t('common.tipTitle'),
			}
		},
		methods: {
			clickIcon(e) {
				if (!e.__config__.tipLabel && !e.helpMessage) return
				this.content = e.helpMessage || e.__config__.tipLabel
				this.title = e.__config__.label
				if (e.__config__.jnpfKey === 'card') this.title = e.header
				if (e.__config__.jnpfKey === 'groupTitle') this.title = e.content
				this.show = true
			},
			toDetail(item) {
				this.$emit('toDetail', item)
			}
		}
	}
</script>