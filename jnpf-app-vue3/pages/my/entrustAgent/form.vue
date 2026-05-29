<template>
	<view class="jnpf-wrap personalData">
		<view class="u-p-l-20 u-p-r-20 content">
			<u-form :model="dataForm" :errorType="['toast']" label-width="180" label-align="left" ref="dataForm">
				<u-form-item :label="titleList[current]" prop='toUserId' required>
					<userSelect v-model="dataForm.toUserId" @change="toChangeUser" :disabled="disabled" multiple
						:placeholder="$t('common.chooseText')" :scope="scope" :entrustType="type" />
				</u-form-item>
				<u-form-item :label="current == 2 ? '代理流程' : '委托流程'">
					<flowSelect v-model="dataForm.flowId" :toUserId="dataForm.toUserId"
						:placeholder="$t('app.my.allFlow')" multiple @change="onChange" :disabled="disabled" clearable
						:current="current" />
				</u-form-item>
				<u-form-item label="开始时间" prop='startTime' required>
					<JnpfDatePicker v-model="dataForm.startTime" :placeholder="$t('common.chooseTextPrefix')"
						:disabled="disabled" @change="change" format="yyyy-MM-dd HH:mm:ss" />
				</u-form-item>
				<u-form-item label="结束时间" prop='endTime' required>
					<JnpfDatePicker v-model="dataForm.endTime" :placeholder="$t('common.chooseTextPrefix')"
						@change="change" :disabled="disabled" format="yyyy-MM-dd HH:mm:ss" />
				</u-form-item>
				<u-form-item label="委托说明">
					<u-input input-align='right' v-model="dataForm.description" type="textarea"
						:placeholder="$t('common.inputTextPrefix')" :disabled="disabled" />
				</u-form-item>
			</u-form>
		</view>
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" type="primary" @click.stop="entrustStop"
				v-if="config.status == 1">{{$t('app.my.stop')}}</u-button>
			<template v-else>
				<u-button class="buttom-btn" @click="getResult('cancel')">{{$t('common.cancelText')}}</u-button>
				<u-button class="buttom-btn" type="primary"
					@click.stop="getResult('confirm')">{{$t('common.okText')}}</u-button>
			</template>
		</view>
	</view>
</template>

<script>
	import {
		UpdateUser
	} from '@/api/common'
	import {
		Create,
		Update,
		FlowDelegateInfo
	} from '@/api/workFlow/entrust.js'
	import flowSelect from './components/flowSelect/index.vue'
	import userSelect from './components/userSelect/index.vue'

	export default {
		components: {
			flowSelect,
			userSelect
		},
		data() {
			return {
				showBtn: false,
				showctionSheet: false,
				show: false,
				props: {
					label: 'fullName',
					value: 'enCode'
				},
				dataForm: {
					id: '',
					toUserId: [],
					flowId: [],
					description: '',
					startTime: '',
					endTime: '',
					flowName: '',
					toUserName: '',
					type: 0,
				},
				typeOptions: [{
					enCode: "0",
					fullName: '发起委托'
				}, {
					enCode: "1",
					fullName: '审批委托'
				}],
				userInfo: {},
				current: '1',
				rules: {
					toUserId: [{
						required: true,
						message: `受委托人不能为空`,
						trigger: ['change', 'blur'],
						type: 'array'
					}],
					endTime: [{
						required: true,
						message: '结束时间不能为空',
						trigger: 'blur',
						type: 'number'
					}],
					startTime: [{
						required: true,
						message: '开始时间不能为空',
						trigger: 'blur',
						type: 'number'
					}]
				},
				myNameAccount: '',
				actionList: [],
				disabled: false,
				config: {},
				scope: 1,
				titleList: ['受委托人', '委托人', '代理人', '被代理人'],
				type: 0
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		onLoad(e) {
			this.userInfo = uni.getStorageSync('userInfo') || {}
			if (e.data) this.config = JSON.parse(decodeURIComponent(e?.data));
			if (this.config) this.init()
		},
		methods: {
			init() {
				this.current = this.config.current || 0
				this.type = this.config.type
				this.config.status = this.config.status || 0
				this.disabled =
					this.config.status == 1 ? true : false
				this.dataForm.id = this.config.id || ''
				let {
					delegateScope,
					proxyScope
				} = uni.getStorageSync('sysConfigInfo') || {}
				this.scope = this.config.type == '0' ? delegateScope : proxyScope
				uni.setNavigationBarTitle({
					title: this.dataForm.id ? this.$t('common.editText') : this.$t('common.addText')
				})
				this.rules.toUserId[0].message = `${this.type == 1 ? '代理人' : '受委托人'}不能为空`;
				if (this.current != 2) {
					this.rules.type = [{
						required: true,
						message: '委托类型不能为空',
						trigger: ['change', 'blur'],
						type: 'number'
					}]
				}
				this.$nextTick(() => {
					this.$refs.dataForm.setRules(this.rules);
				})
				//初始化数据
				if (this.dataForm.id) {
					this.$nextTick(() => {
						FlowDelegateInfo(this.dataForm.id).then(res => {
							this.dataForm = res.data || {}
							this.dataForm.flowId = this.dataForm.flowId ? this.dataForm.flowId.split(",") :
								[]
						})
					})
				}
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
			onChange(id, listData) {
				if (listData && listData.length) {
					let arr = []
					listData.forEach(item => {
						arr.push(item.fullName)
					})
					this.dataForm.flowName = arr.join(",")
				} else {
					this.dataForm.flowName = "全部流程"
				}
			},
			change(val, list) {
				this.$nextTick(() => {
					this.$emit('change', this.dataForm)
				})
			},
			toChangeUser(id, selectedData) {
				this.dataForm.toUserName = selectedData.map(user => user.fullName).join(',');
				return this.dataForm.toUserName
			},
			// 点击确定或者取消
			getResult(event = null) {
				// #ifdef MP-WEIXIN
				if (this.moving) return;
				// #endif
				this.keyword = '';
				if (event === 'cancel') return this.close();
				this.submit()
			},
			close() {
				uni.navigateBack();
			},
			submit() {
				let startTime = this.dataForm.startTime;
				let endTime = this.dataForm.endTime;
				this.dataForm.type = this.config.type || 0
				this.$refs.dataForm.validate(valid => {
					if (valid) {
						if (startTime > endTime) return this.$u.toast('开始时间不能大于等于结束时间')
						const formMethod = this.dataForm.id ? Update : Create
						let params = {
							...this.dataForm
						}
						params.flowId = this.dataForm.flowId ? this.dataForm.flowId.join(",") : ""
						if (!params.flowId) params.flowName = "全部流程"
						formMethod(params).then(res => {
							uni.showToast({
								title: res.msg,
								complete: () => {
									setTimeout(() => {
										uni.$emit('refreshDetail')
										uni.navigateBack()
									}, 1500)
								}
							});
						}).catch()
					}
				});
			},
			onTypeChange() {
				this.dataForm.flowId = []
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
	}
</style>