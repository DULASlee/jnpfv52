<template>
	<view class="jnpf-wrap personalData">
		<view class="u-p-l-20 u-p-r-20 content">
			<u-form :model="dataForm" :errorType="['toast']" label-width="180" label-align="left" ref="dataForm">
				<u-form-item :label="titleList[config?.current]" prop='toUserId' required>
					<view class="txt">
						{{dataForm.userName}}
					</view>
				</u-form-item>
				<u-form-item v-if="config.current == 0 || config.current == 2">
					<view class="u-flex tag-box">
						<view v-for="(item,index) in infoList" :key="index" size="mini">{{item.toUserName}}<u-tag
								class="u-m-l-8" size="mini"
								:type="item.status=== 0?'info':item.status=== 1?'success':'error'"
								:text="item.status === 0 ? '待确认' : item.status === 1 ? '已接受' : '已拒绝'" /></view>
					</view>
				</u-form-item>
				<u-form-item :label="config.current == 2 ? '代理流程' : '委托流程'">
					<view class="txt">{{dataForm.flowName}}</view>
				</u-form-item>
				<u-form-item label="开始时间" prop='startTime' required>
					<view class="txt">
						{{$u.timeFormat(dataForm.startTime,'yyyy-mm-dd hh:MM:ss')}}
					</view>
				</u-form-item>
				<u-form-item label="结束时间" prop='endTime' required>
					<view class="txt">
						{{$u.timeFormat(dataForm.endTime,'yyyy-mm-dd hh:MM:ss')}}
					</view>
				</u-form-item>
				<u-form-item label="委托说明">
					<u-input input-align='right' v-model="dataForm.description" type="textarea" placeholder="请输入"
						disabled />
				</u-form-item>
			</u-form>
		</view>
		<view class="flowBefore-actions" v-if="config.confirmStatus != 1 && config.status != 2">
			<u-button class="buttom-btn" type="primary" @click.stop="jumpForm"
				v-if="isEdit">{{$t('common.editText')}}</u-button>
			<template v-if='isAccept'>
				<u-button class="buttom-btn" type="primary" @click.stop="getResult('accept')">接受</u-button>
				<u-button class="buttom-btn" @click="getResult('refuse')" type="error">拒绝</u-button>
			</template>
			<u-button class="buttom-btn" type="primary" @click.stop="entrustStop" v-if="isStop">终止</u-button>
		</view>
	</view>
</template>
<script>
	import {
		entrustStop,
		getPrincipalDetails,
		entrustHandle,
		FlowDelegateInfo
	} from '@/api/workFlow/entrust.js'
	export default {
		data() {
			const data = {
				dataForm: {
					id: '',
					userId: '',
					toUserId: [],
					flowId: [],
					description: '',
					startTime: '',
					endTime: '',
					flowName: '',
					toUserName: '',
					type: undefined,
				},
				config: {},
				infoList: [],
				titleList: ['受委托人', '委托人', '代理人', '被代理人'],
			}
			return data
		},
		computed: {
			isAccept() {
				if (this.config.current == 1 && this.config.confirmStatus == 2) return false
				if (this.config.current == 0 || this.config.current == 2) return false
				if (this.config.status == 0 || this.config.status == 1) return true
				return false
			},
			isStop() {
				return (this.config.current == 0 || this.config.current == 2) && this.config.status == 1
			},
			isEdit() {
				return (this.config.current == 0 || this.config.current == 2) && this.config.status == 0
			}
		},
		onShow() {
			uni.$on('refreshDetail', () => {
				this.flowDelegateInfo()
			})
		},
		onUnload() {
			uni.$emit('refresh')
			uni.$off('refreshDetail')
		},
		onLoad(e) {
			this.config = JSON.parse(decodeURIComponent(e.data));
			if (this.config) this.init()
		},
		methods: {
			flowDelegateInfo() {
				FlowDelegateInfo(this.config.id).then(res => {
					this.dataForm = res.data || {}
					this.dataForm.flowId = this.dataForm.flowId ? this.dataForm.flowId.split(",") : [];
					this.config = {
						...this.config,
						...this.dataForm
					}
					if (this.dataForm.id) {
						if (this.config.current == 0 || this.config.current == 2) this.getPrincipalDetails()
					}
				})
			},
			init() {
				this.dataForm.id = this.config.id || ''
				this.dataForm.userName = this.config.userName
				this.dataForm.flowName = this.config.flowName
				this.dataForm.description = this.config.description
				this.dataForm.startTime = this.config.startTime
				this.dataForm.endTime = this.config.endTime
				if (this.dataForm.id) {
					if (this.config.current == 1) this.dataForm = this.config || {}
					if (this.config.current == 0 || this.config.current == 2) this.getPrincipalDetails()
				}
			},
			getPrincipalDetails() {
				getPrincipalDetails(this.dataForm.id).then(res => {
					this.infoList = res.data || []
					this.dataForm.userName = this.infoList.map(o => o.toUserName).join()
				})
			},
			entrustStop() {
				let currTime = Math.round(new Date())
				uni.showModal({
					title: '提示',
					content: '结束后,流程不再进行委托!',
					success: (res) => {
						if (res.confirm) {
							entrustStop(this.dataForm.id).then(res => {
								this.dataForm.endTime = currTime
								uni.$emit('refresh')
								uni.navigateBack()
							})
						}
					}
				})
			},
			getResult(entrustType) {
				let data = {
					'type': entrustType === 'accept' ? 1 : 2
				}
				entrustHandle(this.dataForm.id, data).then(res => {
					uni.$emit('refresh')
					uni.navigateBack()
				})
			},
			jumpForm() {
				let isEdit = this.infoList.some(o => o.status == 1)
				if (isEdit) return this.$u.toast("已有人接受，不可编辑");
				uni.navigateTo({
					url: `/pages/my/entrustAgent/form?data=${encodeURIComponent(JSON.stringify(this.config))}`
				});
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.content {
		background-color: #fff;

		:deep(.u-form-item) {
			min-height: 112rpx;
		}

		.u-form {
			padding: 0;
		}

		.tag-box {
			flex-wrap: wrap;
			justify-content: space-between;
			width: 100%;
		}

		.txt {
			text-align: right;
			width: 100%;
		}
	}
</style>