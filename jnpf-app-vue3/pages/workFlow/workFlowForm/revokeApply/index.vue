<template>
	<view class="jnpf-wrap jnpf-wrap-workflow">
		<u-form :model="dataForm" ref="dataForm" :errorType="['toast']" label-position="left" label-width="150"
			label-align="left">
			<view class="u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="审批编号" prop="billRule">
					<u-input v-model="dataForm.billRule" placeholder="审批编号" disabled input-align="right"></u-input>
				</u-form-item>
				<u-form-item label="提交日期" prop="creatorTime">
					<JnpfDatePicker v-model="dataForm.creatorTime" placeholder="请输入提交日期" disabled />
				</u-form-item>
				<u-form-item label="撤销理由" prop="handleOpinion">
					<u-input v-model="dataForm.handleOpinion" placeholder="请输入撤销理由" type="textarea" disabled
						input-align="right"></u-input>
				</u-form-item>
				<u-form-item label="关联流程" prop="revokeFlow">
					<view class="jnpf-link" @click="openRevokeFlow()">
						<text>{{dataForm.revokeTaskName}}</text>
					</view>
				</u-form-item>
			</view>
		</u-form>
	</view>
</template>

<script>
	import comMixin from '../mixin'
	import {
		FlowTask
	} from '@/api/workFlow/flowBefore'
	export default {
		name: 'revoke',
		mixins: [comMixin],
		data() {
			return {
				dataForm: {
					billRule: '',
					creatorTime: '',
					handleOpinion: '',
					revokeTaskId: '',
					revokeTaskName: ''
				}
			}
		},
		methods: {
			/* 打开关联流程 */
			openRevokeFlow() {
				const query = {
					opType: 5,
					id: this.dataForm.revokeTaskId
				};
				uni.navigateTo({
					url: '/pages/workFlow/flowBefore/revokeForm?config=' + this.jnpf.base64.encode(JSON.stringify(
						query))
				})
			},
			selfInit(data) {
				this.dataForm.flowTitle = this.userInfo.userName + "的撤销申请表"
			}
		}
	}
</script>

<style>
	.jnpf-link {
		width: 100%;
		color: #1890ff;
		text-align: right;
	}
</style>