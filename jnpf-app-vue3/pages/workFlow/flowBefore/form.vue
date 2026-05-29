<template>
	<view>
		<template v-if="config.formType == 1">
			<dynamicForm ref="form" @eventReceiver="eventReceiver" @setBtnLoad="setBtnLoad" :config="config" />
		</template>
		<template v-if="config.formType == 2">
			<crmOrder ref="form" @eventReceiver="eventReceiver" v-if="config.formEnCode==='crmOrder'"
				:config="config" />
			<leaveApply ref="form" @eventReceiver="eventReceiver" v-if="config.formEnCode==='leaveApply'"
				:config="config" />
			<salesOrder ref="form" @eventReceiver="eventReceiver" v-if="config.formEnCode==='salesOrder'"
				:config="config" />
			<revokeApply ref="form" @eventReceiver="eventReceiver" v-if="config.formEnCode==='revoke'"
				:config="config" />
		</template>
	</view>
</template>

<script>
	import dynamicForm from '@/pages/workFlow/workFlowForm/dynamicForm'
	import salesOrder from '@/pages/workFlow/workFlowForm/salesOrder'
	import leaveApply from '@/pages/workFlow/workFlowForm/leaveApply'
	import crmOrder from '@/pages/workFlow/workFlowForm/crmOrder'
	import revokeApply from '@/pages/workFlow/workFlowForm/revokeApply'
	export default {
		components: {
			crmOrder,
			dynamicForm,
			leaveApply,
			salesOrder,
			revokeApply
		},
		props: {
			config: {
				type: Object,
				default: () => {}
			},
		},
		methods: {
			eventReceiver(formData, eventType) {
				this.$emit('eventReceiver', formData, eventType)
			},
			setBtnLoad(val) {
				this.$emit('setBtnLoad', val)
			}
		}
	}
</script>