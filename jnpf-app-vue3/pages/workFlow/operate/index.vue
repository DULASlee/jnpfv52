<template>
	<view class="flow-popup-content">
		<u-form ref="dataForm" :model="dataForm" :label-width="250" :errorType="['toast']">
			<view class="content">
				<u-form-item label="分支选择" prop="branchList" required v-if="isBranch">
					<JnpfSelect v-model="dataForm.branchList" @change="branchChange" placeholder="请选择审批分支"
						:options="branchList" multiple :props="props" />
				</u-form-item>
				<u-form-item class="back-item" label="退回节点" v-if="config.type === 'back' && config.backType"
					prop="backNodeCode" required>
					<view class="u-flex-col back-item-inner">
						<JnpfSelect v-model="dataForm.backNodeCode" :options="config.backNodeList" :props="props"
							:disabled="config.backNodeCode != 2" />
						<view class="u-m-t-20 selectNode u-flex" v-if="config.backType == 3">
							<u-radio-group v-model="dataForm.backType">
								<u-radio @change="radioChange(item)" v-for="(item, index) in list" :key="index"
									:name="item.name" :disabled="item.disabled">
									{{ item.fullName }}
								</u-radio>
							</u-radio-group>
						</view>
					</view>
				</u-form-item>
				<u-form-item label="转审给谁" prop="handleIds"
					v-if="['transfer'].includes(config.type) && propertiesType !== 'processing'" required>
					<JnpfUserSelect v-model=" dataForm.handleIds" />
				</u-form-item>
				<u-form-item label="转办给谁" prop="handleIds"
					v-if="['transfer'].includes(config.type) && propertiesType === 'processing'" required>
					<JnpfUserSelect v-model=" dataForm.handleIds" />
				</u-form-item>
				<u-form-item label="协办给谁" prop="handleVal" v-if="['assist'].includes(config.type)" required>
					<JnpfUserSelect v-model=" dataForm.handleVal" multiple @change="changeUserSelect" />
				</u-form-item>
				<view v-if="config.type === 'freeApprover'">
					<u-form-item label="加签人员" prop="addSignUserIdList" required>
						<JnpfUserSelect v-model="dataForm.addSignUserIdList" multiple />
					</u-form-item>
					<u-form-item label="加签类型">
						<JnpfSelect :options="typeList" v-model="dataForm.addSignType" @change="freeApproverChange" />
					</u-form-item>
					<u-form-item label="审批方式">
						<JnpfRadio v-model="dataForm.counterSign" :options="options" />
					</u-form-item>
					<u-form-item label="会签比例" v-if="dataForm.counterSign == 1">
						<view class="u-flex-col free-box">
							<JnpfSelect :options="ratioList" v-model="dataForm.auditRatio" />
							<text class="u-m-l-10 free-box-txt">达到会签比例则通过</text>
						</view>
					</u-form-item>
				</view>
				<template v-for="(item, index) in candidateList" :key="index" v-if="showCandidate">
					<u-form-item :label="item.nodeName+ '审批人'" :required="!item.selected" :border-bottom="false">
						<u-input type="select" v-model="item.fullName" placeholder="请选择审批候选人" input-align="right"
							@click="openSelect(item)" v-if="item.hasCandidates" />
						<JnpfUserSelect v-model="item.value" multiple placeholder="请选择审批候选人" v-else />
					</u-form-item>
					<u-form-item label="已选审批人" v-if="item.selected">
						<u-input type="textarea" v-model="item.selected" class="textarea" border disabled
							placeholder="" />
					</u-form-item>
				</template>
				<u-form-item v-if="showOpinion" :label="opinionTitle">
					<HandleOpinion :commonList="commonList" v-model="dataForm.handleOpinion"
						@addCommonWords="addCommonWords" :showCommon="false"></HandleOpinion>
				</u-form-item>
				<u-form-item prop="handleOpinion" required label-position="top" :label="opinionTitle"
					v-if="showApproval">
					<HandleOpinion :commonList="commonList" v-model="dataForm.handleOpinion"
						@addCommonWords="addCommonWords"></HandleOpinion>
				</u-form-item>
				<template v-if="config.approvalField.length">
					<u-form-item label-position="left" :label="item.fieldName"
						v-for="(item,index) in config.approvalField" :key="index">
						<JnpfInput v-if="item.jnpfKey=='input'" v-model="item.value" />
						<JnpfTextarea v-if="item.jnpfKey=='textarea'" v-model="item.value" />
						<JnpfInputNumber v-if="item.jnpfKey=='inputNumber'" v-model="item.value" />
					</u-form-item>
				</template>
				<u-form-item :prop="signRule ? 'signImg' : '' " :required="signRule" v-if="config.hasSign">
					<JnpfSign v-model="dataForm.signImg" signType="ApprovalSign" />
				</u-form-item>
				<!-- #ifndef APP-HARMONY -->
				<u-form-item v-if="config.hasFile">
					<view class="uploadFile">
						<JnpfUploadFile v-model="dataForm.fileList" :limit="3" align="left" />
					</view>
				</u-form-item>
				<!-- #endif -->
				<u-form-item label="抄送人员" v-if="showCustomCopy">
					<JnpfUserSelect v-model="copyIds" multiple />
				</u-form-item>
			</view>
		</u-form>
		<view class="flowBefore-actions">
			<CustomButton class="u-flex buttom-btn-left-inner" :btnText="$t('common.cancelText')"
				btnIcon="icon-ym icon-ym-add-cancel" customIcon />
			<u-button class="buttom-btn" type="primary" @click="confirm('confirm')">{{$t('common.okText')}}
			</u-button>
		</view>
	</view>
</template>
<script>
	import CustomButton from '@/components/CustomButton'
	import HandleOpinion from './components/HandleOpinion.vue'
	import {
		getSelector,
		Create
	} from '@/api/commonWords'
	export default {
		components: {
			HandleOpinion,
			CustomButton
		},
		data() {
			return {
				copyIds: [],
				selectList: [],
				candidateList: [],
				commonList: [],
				title: '',
				label: '',
				name: {
					'reject': '拒绝',
					'launchRecall': '撤回',
					'auditRecall': '撤回',
					'audit': '同意',
					'back': '退回',
					'freeApprover': '加签',
					'transfer': '转审'
				},
				ratioList: [{
					fullName: '10%',
					id: 10
				}, {
					fullName: '20%',
					id: 20
				}, {
					fullName: '30%',
					id: 30
				}, {
					fullName: '40%',
					id: 40
				}, {
					fullName: '50%',
					id: 50
				}, {
					fullName: '60%',
					id: 60
				}, {
					fullName: '70%',
					id: 70
				}, {
					fullName: '80%',
					id: 80
				}, {
					fullName: '90%',
					id: 90
				}, {
					fullName: '100%',
					id: 100
				}],
				typeList: [{
					fullName: '审批前',
					id: 1
				}, {
					fullName: '审批后',
					id: 2
				}],
				options: [{
						fullName: '或签',
						id: 0
					}, {
						fullName: '会签',
						id: 1
					},
					{
						fullName: "依次审批",
						id: 2,
					}
				],
				list: [{
						fullName: "重新审批",
						disabled: false,
						name: 1,
					},
					{
						fullName: "直接提交给我",
						disabled: false,
						name: 2,
					},
				],
				props: {
					label: 'nodeName',
					value: 'nodeCode'
				},
				showCommonWords: false,
				dataForm: {
					auditRatio: 100,
					counterSign: 0,
					addSignType: 1,
					handleIds: '',
					handleVal: [],
					addSignUserIdList: '',
					fileList: [],
					handleOpinion: "",
					signImg: "",
					copyIds: "",
					branchList: [],
					candidateList: {},
					backNodeCode: "",
					backType: 1
				},
				config: {},
				show: false,
				selectVal: {},
				isCandidates: false,
				rules: {
					branchList: [{
						required: true,
						message: '请选择分支',
						type: 'array',
						trigger: 'blur,change'
					}],
					backNodeCode: [{
						required: true,
						message: '请选择退回节点',
						trigger: 'blur,change'
					}],
					signImg: [{
						required: true,
						message: '请签名',
						trigger: 'blur,change'
					}],
					addSignUserIdList: [{
						required: true,
						message: '请选择加签人员',
						type: 'array',
						trigger: 'blur,change'
					}],
					handleIds: [{
						required: true,
						message: '请选择人员',
						trigger: 'blur,change'
					}],
					handleVal: [{
						required: true,
						message: '请选择协办人员',
						trigger: 'blur,change',
						type: 'array'
					}],
					handleOpinion: [{
						required: true,
						message: '请输入意见',
						trigger: 'blur,change'
					}]
				},
				isCandidate: false,
				propertiesType: ''
			};
		},
		computed: {
			isBranch() {
				let show = false
				if (this.candidateType !== 3) show = true;
				this.branchList = this.config.branchList || [];
				if (!this.branchList.length) show = false;
				if (this.config.type === 'freeApprover' && this.dataForm.addSignType === 1) show = false;
				return show
			},
			showApproval() {
				return ['audit', 'reject', 'approvalButton'].includes(this.config.type)
			},
			showCustomCopy() {
				return this.config.isCustomCopy && ['audit', 'reject'].includes(this.config.type)
			},
			showCandidate() {
				return !['transfer', 'revoke', 'recall', 'back', 'assist'].includes(this.config.type) && this.isCandidate
			},
			showOpinion() {
				const list = ['transfer', 'assist', 'revoke', 'auditRecall', 'launchRecall', 'back', 'freeApprover']
				return list.includes(this.config.type)
			},
			opinionTitle() {
				const typeMap = {
					'transfer': '转审原因',
					'revoke': '撤销原因',
					'assist': '协办原因',
					'back': '退回意见',
					'freeApprover': '加签意见',
					'launchRecall': '撤回原因',
					'auditRecall': '撤回原因',
					'audit': '审批意见',
					'reject': '审批意见'
				};
				const specialTypeMap = {
					'transfer': {
						label: '转办原因'
					},
					'audit': {
						label: '办理意见'
					}
				};
				const setFormRules = (rules) => {
					this.$nextTick(() => {
						this.$refs.dataForm.setRules(rules);
					});
				};
				const type = this.config.type;
				let resultLabel = typeMap[type] || '';
				if (specialTypeMap[type] && this.propertiesType === 'processing') resultLabel = specialTypeMap[type].label;
				return resultLabel;
			},
			fileLabel() {
				const type = this.config.type;
				const typeMap = {
					'auditRecall': '撤回附件',
					'freeApprover': '加签附件',
					'back': '退回附件',
					'transfer': '转审附件',
					'audit': '审批附件',
					'reject': '审批附件'
				};
				if (type === 'transfer' && this.propertiesType === 'processing') return '转办附件';
				if (type === 'audit' && this.propertiesType === 'processing') return '办理附件';
				const result = typeMap[type] || '';
				return result;
			},
			signRule() {
				return this.config.hasSign && (['audit', 'reject'].includes(this.config.type))
			}
		},
		onLoad(data) {
			try {
				this.config = JSON.parse(decodeURIComponent(data.config));
			} catch {
				this.config = JSON.parse(data.config);
			}
			uni.$on("confirm", (data, nodeCode) => {
				this.selectConfirm(data, nodeCode);
			});
			this.init()
		},
		onReady() {
			this.$refs.dataForm.setRules(this.rules);
		},
		methods: {
			init() {
				this.candidateType = this.config.candidateType; /* 1==分支 2==候选人 3==直接通过*/
				this.candidateList = this.config.candidateList || [];
				this.propertiesType = this.config?.propertiesType
				this.getSelector()
				this.handleLabel()
				this.config.candidateList.map(o => {
					this.isCandidates = o.isCandidates
				})
				this.copyIds = this.config?.circulateUser || ''
				this.dataForm.backNodeCode = this.config?.backNodeList?.length ? this.config.backNodeList[0].nodeCode : '';
				if (this.candidateType != 3) this.isCandidate = true;
				this.branchList = this.config.branchList || [];
				if (this.candidateType == 1) {
					let list = [];
					this.isCandidate = false;
					const defaultList = this.candidateList;
					for (let i = 0; i < this.dataForm.branchList.length; i++) {
						inner: for (let j = 0; j < this.branchList.length; j++) {
							let o = this.branchList[j];
							if (this.dataForm.branchList[i] === o.nodeCode && o.isCandidates) {
								this.isCandidate = true;
								list.push({
									...o,
									label: o.nodeName + "审批人",
								});
								break inner;
							}
						}
						this.candidateList = [...defaultList, ...list];
					}
				}
				this.userInfo = uni.getStorageSync("userInfo") || {};
				this.dataForm.signImg = this.userInfo.signImg;
				if (this.config.type === 'freeApprover' && this.dataForm.addSignType == '1') this.isCandidates = false
			},
			handleLabel() {
				const config = this.config;
				const {
					type,
					propertiesType
				} = config;
				const typeMapping = {
					'transfer': {
						title: propertiesType === 'processing' ? '转办' : '转审',
						label: '转审'
					},
					'assist': {
						title: '协办',
						label: '协办'
					},
					'revoke': {
						title: '撤销流程',
						label: '撤销'
					},
					'launchRecall': {
						title: '撤回流程',
						label: '撤回'
					},
					'reject': {
						title: '审批拒绝',
						label: '拒绝'
					},
					'audit': (propertiesType) => ({
						title: propertiesType === 'processing' ? '办理' : '审批',
						label: '审批'
					}),
					'auditRecall': {
						title: '撤回审核',
						label: '撤回'
					},
					'freeApprover': {
						title: '加签',
						label: '加签'
					},
					'back': {
						title: '退回',
						label: '退回'
					},
					'submit': {
						title: '提交审核',
						label: '提交审核'
					}
				};
				const getTitleAndLabel = (typeKey, propertiesType) => {
					const mapping = typeMapping[typeKey];
					if (typeof mapping === 'function') return mapping(propertiesType);
					return mapping || {
						title: '',
						label: ''
					};
				};
				const {
					title,
					label
				} = getTitleAndLabel(type, propertiesType);
				this.title = title;
				this.label = label;
				uni.setNavigationBarTitle({
					title: this.title
				});
			},
			changeUserSelect(e) {
				this.dataForm.handleIds = e.join()
			},
			branchChange(e, list) {
				this.dataForm.branchList = e;
				this.candidateList = []
				this.init();
			},
			openSelect(item) {
				item.formData = this.config.formData;
				item.taskId = this.config.operatorId;
				item.selectList = item.selectList || [];
				item.candidateList = JSON.stringify(this.candidateList);
				item.delegateUser = this.config.delegateUser
				uni.navigateTo({
					url: "/pages/workFlow/candiDateUserSelect/index?data=" +
						encodeURIComponent(JSON.stringify(item)),
				});
			},
			selectConfirm(data, nodeCode) {
				let users = [];
				const item = this.candidateList.filter(o => o.nodeCode == nodeCode)[0] || {}
				item.value = data.map(o => o.id) || []
				item.fullName = (data.map(o => o.fullName) || []).join(',') || ''
				item.selectList = data || []
				for (let i = 0; i < this.candidateList.length; i++) {
					for (let j = 0; j < data.length; j++) {
						if (data[j].nodeCode === this.candidateList[i].nodeCode) {
							users.push(data[j].id);
						}
					}
				}
				this.$set(this.dataForm.candidateList, nodeCode, users);
			},
			getSelector() {
				getSelector().then(res => {
					this.commonList = res.data.list || []
				})
			},
			confirmCommonWord(e) {
				this.dataForm.handleOpinion = e.commonWordsText
			},
			handlePress(e) {
				this.$emit('handlePress')
			},
			addCommonWords() {
				let data = {
					commonWordsText: this.dataForm.handleOpinion,
					commonWordsType: 1
				}
				Create(data).then(res => {
					this.$u.toast(res.msg);
				})
			},
			freeApproverChange(e) {
				this.isCandidates = false;
				if (this.config.hasFreeApprover && e == 2 && this.candidateList.length) this.isCandidates = true;
			},
			confirm() {
				if (this.config.type === 'freeApprover') {
					this.dataForm.addSignParameter = {
						'addSignUserIdList': this.dataForm.addSignUserIdList,
						'auditRatio': this.dataForm.auditRatio,
						'counterSign': this.dataForm.counterSign,
						'addSignType': this.dataForm.addSignType
					}
				}
				if (!this.config.hasSign) delete this.dataForm.signImg
				this.dataForm.copyIds = Array.isArray(this.copyIds) && this.copyIds.length && this.copyIds.join()
				if (this.config.backType !== 3) this.dataForm.backType = this.config.backType
				let data = {
					...this.dataForm,
					eventType: ['auditRecall', 'launchRecall'].includes(this.config.type) ? 'recall' : this.config
						.type,
					approvalField: this.config.approvalField
				}
				if (this.isCandidates || this.isCandidate) {
					let candidateList = {};
					for (let i = 0; i < this.candidateList.length; i++) {
						let item = this.candidateList[i]
						if (!item.selected && !item.value?.length) return this.$u.toast('候选人不能为空')
						candidateList[item.nodeCode] = item.value || [];
					}
					data.candidateList = candidateList;
				}
				this.$refs.dataForm.validate(valid => {
					if (valid) {
						uni.$emit('operate', data)
						setTimeout(() => {
							uni.navigateBack()
						}, 500)
					}
				});
			}
		}
	}
</script>

<style lang="scss">
	page {
		height: 100%;
		background-color: #fff !important;
	}

	::v-deep .u-form-item--left {
		align-items: flex-start !important;
	}

	::v-deep .u-form-item {
		line-height: 1.5rem !important;
	}

	.textarea {
		background-color: #f5f5f5;
	}

	::v-deep .u-input--border {
		border: 1rpx solid #f5f5f5 !important;
	}

	.buttom-btn-left-inner {
		width: 50% !important;
	}

	.free-box {
		width: 100%;

		.free-box-txt {
			text-align: end;
		}
	}

	.flow-popup-content {
		padding-bottom: 88rpx;

		::v-deep .u-form {
			.content {
				.u-form-item {
					.u-form-item__body {
						.u-form-item--left {
							// align-items: center !important;
						}
					}
				}
			}
		}

		.signature-box {
			border-top: none;
		}

		.content {
			padding: 0 20rpx;

			.back-item {
				.back-item-inner {
					justify-content: end;
					width: 100%;

					.selectNode {
						width: 100%;
						justify-content: flex-end;
					}
				}
			}

			.head-title {
				height: 80rpx;
				justify-content: space-between;
				color: #333333;
			}

			.uploadFile {
				width: 100%;
				padding-bottom: 8rpx;
				border-top: 1rpx solid #fbfbfc;
			}
		}
	}
</style>