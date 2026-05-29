<template>
	<view class="scheduleForm-v jnpf-wrap">
		<u-toast ref="uToast" />
		<u-form :model="dataForm" :rules="rules" ref="dataForm" :errorType="['toast']" label-position="left"
			label-width="150" label-align="left">
			<view class="u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="标题" prop="title" required>
					<u-input input-align='right' v-model="dataForm.title" placeholder="请输入"></u-input>
				</u-form-item>
			</view>
			<view class="u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="内容" prop="content">
					<u-input input-align='right' v-model="dataForm.content" placeholder="请输入" type="textarea"></u-input>
				</u-form-item>
			</view>
			<!-- #ifndef APP-HARMONY -->
			<view class="jnpf-card u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="附件" prop="files">
					<JnpfUploadFile v-model="dataForm.files" />
				</u-form-item>
			</view>
			<!-- #endif -->
			<view class="jnpf-card">
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="紧急程度" prop="urgent">
						<JnpfSelect v-model="dataForm.urgent" :options='urgentList' placeholder="请选择" />
					</u-form-item>
				</view>
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="类型" prop="category" required>
						<JnpfSelect v-model="dataForm.category" :options='categoryOptions' placeholder="请选择" />
					</u-form-item>
				</view>
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="创建人" prop="creatorUserId" required>
						<JnpfUserSelect v-model="dataForm.creatorUserId" placeholder="请输入" disabled="disabled" />
					</u-form-item>
				</view>
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="参与人" prop="toUserIds">
						<JnpfUserSelect v-model="dataForm.toUserIds" placeholder="请选择" multiple />
					</u-form-item>
				</view>
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="标签颜色" prop="color">
						<JnpfColorPicker v-model="dataForm.color" />
					</u-form-item>
				</view>
			</view>
			<view class="u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="全天" prop="allDay">
					<JnpfSwitch v-model="dataForm.allDay" @change="change_providerType" />
				</u-form-item>
			</view>
			<view class="u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="开始时间" prop="startDay" required>
					<tdatetime :delayMin="0" v-model="startDay" :date="startDate" placeholder="请选择"
						:showtdatetime='showtdatetime' :type="1" @confirm='confirm' :allDay='dataForm.allDay' />
				</u-form-item>
			</view>
			<view class="u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="时长" prop="duration" required v-if="dataForm.duration!=-1&&dataForm.allDay==0">
					<JnpfSelect v-model="dataForm.duration" :options='durationList' />
				</u-form-item>
			</view>
			<view class="u-p-l-20 u-p-r-20 form-item-box">
				<u-form-item label="结束时间" prop="endDay" required v-if='dataForm.duration==-1||dataForm.allDay'>
					<tdatetime :delayMin="0" v-model="endDay" :date="endDate" placeholder="请选择"
						:showtdatetime='showtdatetime' :type="2" @confirm='confirm' :allDay='dataForm.allDay' />
				</u-form-item>
			</view>
			<view class="jnpf-card">
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="提醒时间" prop="reminderTime">
						<JnpfSelect v-model="dataForm.reminderTime"
							:options='dataForm.allDay?reminderTimeList_:reminderTimeList' placeholder="请选择" />
					</u-form-item>
				</view>
				<view v-if="dataForm.reminderTime!=-2" class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="提醒方式" prop="reminderType">
						<jnpf-select v-model="dataForm.reminderType" :options='remindList'
							placeholder="请选择"></jnpf-select>
					</u-form-item>
					<u-form-item label="发送配置" prop="send" v-if="dataForm.reminderType==2">
						<sendSelect v-model="dataForm.sendName" :send='dataForm.send' @change='changeSend'
							placeholder="请选择" />
					</u-form-item>
				</view>
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="重复提醒" prop="repetition">
						<jnpf-select v-model="dataForm.repetition" :options='repeatReminderList'
							@change='repeatTimeChange' placeholder="请选择"></jnpf-select>
					</u-form-item>
				</view>
				<view class="u-p-l-20 u-p-r-20 form-item-box">
					<u-form-item label="结束重复" prop="repeatTime" required v-if='dataForm.repetition!=1'>
						<tdatetime :delayMin="0" v-model="repeat" :date="repeatDate" placeholder="请选择"
							:showtdatetime='showtdatetime' :type="3" @confirm='confirm' :allDay='1' />
					</u-form-item>
				</view>
			</view>
		</u-form>
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" @click="getResult('cancel')">{{$t('common.cancelText')}}</u-button>
			<u-button class="buttom-btn" type="primary" :loading='btnLoading'
				@click.stop="save()">{{$t('common.okText')}}</u-button>
		</view>
		<u-action-sheet @click="handleAction" :list="actionList"
			:tips="{ text:'此为重复日程，将修改应用于' , color: '#000' , fontSize: 30 }" v-model="show">
		</u-action-sheet>
	</view>
</template>

<script>
	import tdatetime from './t-datetime.vue'
	import sendSelect from './sendSelect/index.vue'
	import {
		ScheduleInfo,
		ScheduleCreate,
		ScheduleUpdate
	} from '@/api/workFlow/schedule.js'
	import {
		useBaseStore
	} from '@/store/modules/base'

	const baseStore = useBaseStore()


	export default {
		components: {
			tdatetime,
			sendSelect
		},
		data() {
			return {
				title: '',
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
					duration: 60,
					content: '',
					title: '',
					toUserIds: [],
					reminderTime: -2,
					reminderType: 1,
					send: '',
					sendName: '',
					repetition: 1,
					repeatTime: '',
					color: '#FFFFFF',
					files: []
				},
				btnLoading: false,
				showBtn: false,
				startDate: {},
				endDate: {},
				repeatDate: {},
				repeat: '',
				endDay: '',
				startDay: '',
				urgentList: [{
					id: "1",
					fullName: '普通'
				}, {
					id: '2',
					fullName: '重要'
				}, {
					id: '3',
					fullName: '紧急'
				}],
				durationList: [{
					id: 30,
					fullName: '30分钟'
				}, {
					id: 60,
					fullName: '1小时'
				}, {
					id: 90,
					fullName: '1小时30分钟'
				}, {
					id: 120,
					fullName: '2小时'
				}, {
					id: 180,
					fullName: '3小时'
				}, {
					id: -1,
					fullName: '自定义'
				}],
				categoryOptions: [],
				repetitionType: false,
				reminderTimeList: [{
					id: -2,
					fullName: '不提醒'
				}, {
					id: -1,
					fullName: '开始时'
				}, {
					id: 5,
					fullName: '提前5分钟'
				}, {
					id: 10,
					fullName: '提前10分钟'
				}, {
					id: 15,
					fullName: '提前15分钟'
				}, {
					id: 30,
					fullName: '提前30分钟'
				}, {
					id: 60,
					fullName: '提前1小时'
				}, {
					id: 120,
					fullName: '提前2小时'
				}, {
					id: 1440,
					fullName: '1天前'
				}, {
					id: 2880,
					fullName: '2天前'
				}, {
					id: 10080,
					fullName: '1周前'
				}],
				reminderTimeList_: [{
						id: -2,
						fullName: '不提醒'
					},
					{
						id: 1,
						fullName: '当天8:00'
					},
					{
						id: 2,
						fullName: '当天9:00'
					},
					{
						id: 3,
						fullName: '当天10:00'
					},
					{
						id: 4,
						fullName: '1天前8:00'
					},
					{
						id: 5,
						fullName: '1天前9:00'
					},
					{
						id: 6,
						fullName: '1天前10:00'
					},
					{
						id: 7,
						fullName: '2天前8:00'
					},
					{
						id: 8,
						fullName: '2天前9:00'
					},
					{
						id: 9,
						fullName: '2天前10:00'
					},
					{
						id: 10,
						fullName: '1周前8:00'
					},
					{
						id: 11,
						fullName: '1周前9:00'
					},
					{
						id: 12,
						fullName: '1周前10:00'
					}
				],
				remindList: [{
					id: 1,
					fullName: '默认'
				}, {
					id: 2,
					fullName: '自定义'
				}],
				repeatReminderList: [{
					id: 1,
					fullName: '不重复'
				}, {
					id: 2,
					fullName: '每天重复'
				}, {
					id: 3,
					fullName: '每周重复'
				}, {
					id: 4,
					fullName: '每月重复'
				}, {
					id: 5,
					fullName: '每年重复'
				}],
				actionList: [],
				rules: {
					startTime: [{
						required: true,
						message: '开始时间不能为空',
						trigger: 'change',
						type: 'number'
					}],
					startTime: [{
						required: true,
						message: '开始时间不能为空',
						trigger: 'change',
						type: 'number'
					}],
					endTime: [{
						required: true,
						message: '结束时间不能为空',
						trigger: 'change',
						type: 'number'
					}],
					repeat: [{
						required: true,
						message: '记录不能为空',
						trigger: 'change',
					}],
					category: [{
						required: true,
						message: '请选择类型',
						trigger: 'change'
					}],
					title: [{
						required: true,
						message: '请输入标题',
						trigger: 'blur'
					}],
					duration: [{
						required: true,
						message: '请选择时长',
						trigger: 'change',
						type: 'number'
					}],
					send: [{
						required: true,
						message: '发送配置不能为空',
						trigger: 'change'
					}],
					reminderType: [{
						required: true,
						message: '提醒方式不能为空',
						trigger: 'change',
						type: 'number'
					}]
				}
			}
		},
		onReady() {
			this.$refs.dataForm.setRules(this.rules);
		},
		onLoad(option) {
			this.repetitionType = false
			this.userInfo = uni.getStorageSync('userInfo') || {}
			this.dataForm.id = option.id
			this.btnLoading = false
			this.title = this.dataForm.id ? '编辑' : '新建'
			uni.setNavigationBarTitle({
				title: this.title
			});
			if (this.dataForm.id) {
				ScheduleInfo(option.id).then(res => {
					let data = res.data
					data.files = data.files ? JSON.parse(data.files) : [];
					this.dataForm = data
					this.startDate = this.timestampToTime(this.dataForm.startDay)
					this.endDate = this.timestampToTime(this.dataForm.endDay)
					this.repeatDate = this.dataForm.repeatTime ? this.timestampToTime(this.dataForm.repeatTime) :
						{}
					if (this.dataForm.repetition != "1") return this.repetitionType = true
				})
			} else {
				let startDate = this.timestampToTime(+new Date())
				this.startDate = this.timestampToTime(option.startTime || +new Date())
				this.startDate.hours = startDate.hours + 1
				this.startDate.minutes = '00'
				this.confirm(this.startDate, 1)
				this.endDate = this.timestampToTime(option.startTime || +new Date())
				this.endDate.hours = startDate.hours + 2
				this.endDate.minutes = '00'
				this.confirm(this.endDate, 2)
				this.repeatDate = {}
				this.dataForm.creatorUserId = this.userInfo.userId
				this.dataForm.duration = Number(option.duration) || 60
			}
			this.getDictionaryData()
		},
		methods: {
			back() {
				if (!this.dataForm.id) return uni.navigateBack()
				uni.navigateBack({
					delta: 2
				})
			},
			handleAction(index) {
				ScheduleUpdate(this.dataForm, index + 1).then(res => {
					uni.showToast({
						title: res.msg,
						complete: () => {
							uni.$emit('refresh')
							uni.navigateBack({
								delta: 2
							})
						}
					})
				})
			},
			change_providerType(val) {
				if (!val) {
					let startDate = this.timestampToTime(+new Date())
					this.startDate = this.timestampToTime(this.dataForm.startDay)
					this.startDate.hours = startDate.hours + 1
					this.startDate.minutes = '00'
					this.confirm(this.startDate, 1)
					this.endDate = this.timestampToTime(this.dataForm.endDay)
					this.endDate.hours = startDate.hours + 2
					this.endDate.minutes = '00'
					this.confirm(this.endDate, 2)
				} else {
					this.dataForm.endDay = this.dataForm.startDay
				}
				this.dataForm.reminderTime = -2
			},
			repeatTimeChange(val) {
				let time = new Date()
				time.setFullYear(time.getFullYear() + 1)
				if (val != 1) {
					let date = time.getTime()
					this.repeatDate = this.timestampToTime(date)
					this.repeatDate.minutes = '00'
					this.confirm(this.repeatDate, 3)
				}
			},
			getResult() {
				if (!this.dataForm.id) return uni.navigateBack()
				uni.showModal({
					title: '退出此次编辑？',
					content: '日程信息将不会保存',
					success: res => {
						if (res.confirm) {
							uni.$emit('refresh')
							uni.navigateBack({
								delta: 2
							})
						}
					}
				})

			},
			changeSend(id, name) {
				this.dataForm.send = id
				this.dataForm.sendName = name
			},
			getDictionaryData() {
				baseStore.getDictionaryData({
					sort: 'scheduleType'
				}).then((res) => {
					this.categoryOptions = res || []
					if (this.categoryOptions.length) this.dataForm.category = this.categoryOptions[0].id
				})
			},
			confirm(e, type) {
				if (type == 1) {
					this.dataForm.startDay = e.year + '-' + e.month + '-' + e.date
					this.dataForm.startDay = new Date(this.dataForm.startDay).getTime()
					this.dataForm.startTime = e.hours + ":" + e.minutes
				} else if (type == 2) {
					this.dataForm.endDay = e.year + '-' + e.month + '-' + e.date
					this.dataForm.endDay = new Date(this.dataForm.endDay).getTime()
					this.dataForm.endTime = e.hours + ":" + e.minutes
				} else {
					this.dataForm.repeatTime = e.year + '-' + e.month + '-' + e.date
					this.dataForm.repeatTime = new Date(this.dataForm.repeatTime).getTime()
				}
			},
			save() {
				this.$refs.dataForm.validate((valid) => {
					if (valid) {
						if (this.dataForm.duration == -1) {
							if (this.dataForm.startDay > this.dataForm.endDay) {
								this.$refs.uToast.show({
									title: '结束时间必须晚于开始时间',
									type: 'error'
								})
								return
							}
						}
						if (this.dataForm.allDay == 1) {
							let startDay = this.timestampToData(this.dataForm.startDay)
							let endDay = this.timestampToData(this.dataForm.endDay)
							if (this.dataForm.startDay > this.dataForm.endDay) {
								this.$refs.uToast.show({
									title: '结束时间必须晚于开始时间',
									type: 'error'
								})
								return
							}
							this.dataForm.startDay = new Date(startDay).getTime()
							this.dataForm.endDay = new Date(endDay).getTime()
						}
						if (this.dataForm.repetition != 1) {
							if (this.dataForm.startDay > this.dataForm.repeatTime) {
								this.$refs.uToast.show({
									title: '结束重复时间必须晚于开始时间',
									type: 'error'
								})
								return
							}
						}
						if (this.dataForm.id && this.repetitionType) {
							this.show = true
							this.actionList = []
							this.actionList.push({
								text: '仅修改此日程',
								id: '1'
							})
							this.actionList.push({
								text: '修改此日程及后续日程',
								id: '2',
							})
						} else {
							this.btnLoading = true
							const formMethod = this.dataForm.id ? ScheduleUpdate : ScheduleCreate;
							const query = {
								...this.dataForm,
								files: JSON.stringify(this.dataForm.files)
							}
							formMethod(query, 3).then(res => {
								uni.showToast({
									title: res.msg,
									complete: () => {
										this.btnLoading = false
										uni.$emit('refresh')
										if (this.dataForm.id) {
											uni.navigateBack({
												delta: 2
											})
										} else {
											uni.navigateBack()
										}
									}
								})
							})
						}
					}
				})
			},
			timestampToTime(timestamp) {
				let list = {}
				timestamp = timestamp || 0
				var date = new Date(Number(timestamp));
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
				list.seconds = s < 10 ? 0 + s : s
				return list
			},
			timestampToData(timestamp) {
				var date = new Date(timestamp);
				let Y = date.getFullYear();
				let M = (date.getMonth() + 1 < 10 ? '0' + (date.getMonth() + 1) : date.getMonth() + 1);
				let D = date.getDate();
				return Y + '-' + M + '-' + D + " 00:00:00"
			}
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