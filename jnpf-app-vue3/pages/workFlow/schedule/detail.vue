<template>
	<view class="">
		<u-navbar title="详情" :custom-back="back">
			<template #right>
				<view class="navbar-right">
					<view class="message-box right-item" style="padding-right: 20rpx;">
						<u-icon name='more-dot-fill' @click="show=true" v-if="!groupId"></u-icon>
					</view>
				</view>
			</template>
		</u-navbar>
		<view class="scheduleForm-v jnpf-wrap">
			<u-toast ref="uToast" />
			<u-form :model="dataForm" ref="dataForm" :errorType="['toast']" label-position="left" label-width="150"
				label-align="left">
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="标题" prop="title">
						<u-input input-align='right' v-model="dataForm.title" label-align="right" placeholder=""
							disabled></u-input>
					</u-form-item>
					<u-form-item label="内容" prop="content">
						<u-input input-align='right' v-model="dataForm.content" type="textarea" placeholder=""
							disabled></u-input>
					</u-form-item>
				</view>
				<!-- #ifdef APP-HARMONY -->
				<view class="u-p-l-20 u-p-r-20 u-m-t-20 form-item-box">
					<u-form-item label="附件" prop="content">
						<JnpfUploadFile v-model="dataForm.files" detailed />
					</u-form-item>
				</view>
				<!-- #endif -->
				<view class="jnpf-card">
					<view class="u-p-l-20 u-p-r-20 form-item-box">
						<u-form-item label="紧急程度" prop="urgent">
							<u-input input-align='right' v-model="dataForm.urgent" label-align="right" placeholder=""
								disabled></u-input>
						</u-form-item>
						<u-form-item label="类型" prop="category">
							<u-input input-align='right' v-model="dataForm.category" placeholder="" disabled></u-input>
						</u-form-item>
						<u-form-item label="创建人" prop="creatorUserId">
							<u-input input-align='right' v-model="dataForm.creatorUserId" placeholder=""
								disabled></u-input>
						</u-form-item>
						<u-form-item label="参与人" prop="toUserIds">
							<u-input input-align='right' v-model="dataForm.toUserIds" placeholder="" disabled></u-input>
						</u-form-item>
					</view>
				</view>
				<view class="jnpf-card">
					<view class="u-p-l-20 u-p-r-20 form-item-box">
						<u-form-item label="开始时间" prop="startDay">
							<datetime :delayMin="0" v-model="startDay" :date="startDate" disabled
								:showtdatetime='showtdatetime' :type="1" :allDay='dataForm.allDay' placeholder="" />
						</u-form-item>
						<u-form-item label="结束时间" prop="endDay">
							<datetime :delayMin="0" v-model="endDay" :date="endDate" disabled
								:showtdatetime='showtdatetime' :type="2" :allDay='dataForm.allDay' placeholder="" />
						</u-form-item>
					</view>
				</view>
			</u-form>
		</view>
		<u-action-sheet @click="handleAction" :list="actionList" :tips="{ text:'' , color: '#000' , fontSize: 30 }"
			v-model="show">
		</u-action-sheet>
		<u-action-sheet @click="delAction" :list="delList"
			:tips="{ text:toUserType===true?dataForm.repetition != '1'?'此为重复日程，将删除（含参与人）应用于':'确认删除（含参与人）当前日程':dataForm.repetition != '1'?'此为重复日程，将删除应用于':'确认删除当前日程' , color: '#000' , fontSize: 30 }"
			v-model="showBtn">
		</u-action-sheet>
	</view>
</template>

<script>
	import datetime from './t-datetime.vue'
	import {
		ScheduleDetail,
		ScheduleDelete
	} from '@/api/workFlow/schedule.js'
	import {
		useBaseStore
	} from '@/store/modules/base'

	const baseStore = useBaseStore()

	export default {
		components: {
			datetime
		},
		data() {
			return {
				showAction: '',
				userInfo: {},
				show: false,
				showtdatetime: false,
				dataForm: {
					id: 0,
					category: '',
					creatorUserId: 0,
					userName: '',
					allDay: 0,
					urgent: '1',
					startDay: '',
					startTime: '',
					endDay: '',
					endTime: '',
					duration: '',
					content: '',
					title: '',
					toUserIds: '',
					reminderTime: -2,
					reminderType: 1,
					send: '',
					sendName: '',
					repetition: 1,
					repeatTime: '',
					color: '#188ae2'
				},
				showBtn: false,
				startDate: {},
				endDate: {},
				repeatDate: {},
				repeat: '',
				endDay: '',
				startDay: '',
				toUserType: false,
				actionList: [],
				delList: [],
				groupId: ''
			}
		},
		onReady() {
			this.$refs.dataForm.setRules(this.rules);
		},
		onLoad(option) {
			this.dataForm.id = option.id
			this.toUserType = option.type
			this.actionList = []
			this.delList = []
			this.groupId = option.groupId
			if (this.toUserType == 'true') {
				this.actionList.push({
					text: '编辑',
					id: 'update'
				})
				this.actionList.push({
					text: '删除',
					id: 'deldete',
					color: '#ff3a3a',
				})
			} else {
				this.actionList.push({
					text: '删除',
					id: 'deldete',
					color: '#ff3a3a',
				})
			}
			ScheduleDetail(this.groupId, option.id).then(res => {
				let data = res.data || {};
				data.files = data.files ? JSON.parse(data.files) : [];
				this.dataForm = data
				if (this.dataForm.repetition != '1') {
					this.delList.push({
						text: '仅删除此日程',
						id: '1'
					})
					this.delList.push({
						text: '删除此日程及后续日程',
						id: '2',
					})
					this.delList.push({
						text: '删除所有日程',
						id: '3',
					})
				} else {
					this.delList.push({
						text: '删除',
						id: '3',
						color: '#ff3a3a',
					})
				}
				this.startDate = this.timestampToTime(this.dataForm.startDay)
				this.endDate = this.timestampToTime(this.dataForm.endDay)
			}).catch((err) => {
				uni.showToast({
					title: err,
					complete: () => {
						setTimeout(() => {
							uni.navigateBack()
						}, 300)
					}
				})
			})
		},
		methods: {
			delAction(index) {
				if (this.dataForm.repetition != '1') {
					ScheduleDelete(this.dataForm.id, index + 1).then(res => {
						uni.showToast({
							title: res.msg,
							complete: () => {
								uni.$emit('refresh')
								uni.navigateBack()
							}
						})
					})
				} else {
					ScheduleDelete(this.dataForm.id, 3).then(res => {
						uni.showToast({
							title: res.msg,
							complete: () => {
								uni.$emit('refresh')
								uni.navigateBack()
							}
						})
					})
				}
			},
			handleAction(index) {
				if (this.actionList[index].id == 'update') {
					uni.navigateTo({
						url: './form?id=' + this.dataForm.id
					})
				} else {
					this.showBtn = true
				}
			},
			back() {
				uni.navigateBack();
			},
			getResult() {
				uni.navigateBack()
			},
			changeSend(id, name) {
				this.dataForm.send = id
				this.dataForm.sendName = name
			},
			getDictionaryData() {
				baseStore.getDictionaryData({
					sort: 'scheduleType'
				}).then((res) => {
					this.typeOptions = res
				})
			},
			timestampToTime(timestamp) {
				let list = {}
				var date = new Date(timestamp);
				let Y = date.getFullYear();
				let M = (date.getMonth() + 1 < 10 ? '0' + (date.getMonth() + 1) : date.getMonth() + 1);
				let D = date.getDate();
				let h = date.getHours();
				let m = date.getMinutes();
				let s = date.getSeconds();
				list.year = Y
				list.month = M
				list.date = D
				list.hours = h < 10 ? 0 + h : h
				list.minutes = m < 10 ? 0 + m : m
				list.seconds = s
				return list
			},
		}
	}
</script>
<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.scheduleForm-v {
		padding-bottom: 110rpx;
	}
</style>