<template>
	<view class="candidateForm-v">
		<u-popup mode="left" :popup="false" v-model="showPopup" length="auto" @close="close" width="100%">
			<view class="jnpf-wrap jnpf-wrap-form">
				<u-form :model="candidateForm" ref="candidateForm" :errorType="['toast']" label-position="left"
					label-width="150" label-align="left">
					<u-form-item label="分支选择" prop="branch" v-if="candidateType == 1" required>
						<JnpfSelect v-model="candidateForm.branchList" @change="branchChange" placeholder="请选择审批分支"
							:options="branchList" :multiple="true" :props='props' />
					</u-form-item>
					<u-form-item label-width="250" v-for="(item,index) in candidateForm.list" :key="index"
						:label="item.label">
						<u-input type="select" :select-open="item.selectShow" v-model="item.value"
							@click="openSelect(item)" placeholder="请选择审批候选人">
						</u-input>
					</u-form-item>
				</u-form>
				<view class="buttom-actions">
					<u-button class="buttom-btn" @click="cancel">取消</u-button>
					<u-button class="buttom-btn" type="primary" @click="submit">确定</u-button>
				</view>
			</view>
		</u-popup>
	</view>
</template>

<script>
	export default {
		props: {
			modelValue: {
				type: Boolean,
				default: false
			},
			formData: {
				type: Object,
				default: () => ({})
			},
			taskId: {
				type: String,
				default: ""
			}
		},
		data() {
			return {
				showPopup: false,
				innerValue: '',
				candidateForm: {
					list: [],
					branchList: []
				},

				nodeId: '',
				selectId: {},
				rules: {},
				selectList: [],
				selectVal: {},
				isCandidate: false,
				props: {
					label: 'nodeName',
					value: 'nodeId'
				},
				candidateType: 1,
				branchList: [],
				candidateList: []
			};
		},
		watch: {
			modelValue: {
				immediate: true,
				handler(val) {
					this.showPopup = val
				}
			},
		},
		created() {
			uni.$on('confirm', (data, id) => {
				this.selectConfirm(data, id)
			})
		},
		onUnload() {
			uni.$off('confirm')
		},
		methods: {
			branchChange(e) {
				this.candidateForm.branchList = e;
				this.init()
			},
			init(candidateType, branchList, candidateList) {
				this.candidateType = candidateType;
				this.branchList = branchList;
				this.candidateList = candidateList
				if (this.candidateType == 1) {
					let list = [];
					for (let i = 0; i < this.candidateForm.branchList.length; i++) {
						inner: for (let j = 0; j < this.branchList.length; j++) {
							let o = this.branchList[j]
							if (o.isCandidates) this.isCandidate = o.isCandidates
							if (this.candidateForm.branchList[i] === o.nodeId && o.isCandidates) {
								list.push({
									...o,
									label: o.nodeName + '审批人',
									value: '',
									selectShow: false
								})
								break inner
							}
						}
						this.candidateForm.list = list
					}
				} else {
					if (Array.isArray(this.candidateList) && this.candidateList.length) {
						this.isCandidate = true
						this.candidateForm.list = this.candidateList.map(o => ({
							...o,
							label: o.nodeName + '审批人',
							value: '',
							selectShow: false
						}))
					}
				}
			},
			openSelect(item) {
				this.selectList = []
				for (let o in this.selectVal) {
					if (o === item.nodeId) this.selectList = this.selectVal[o]
				}
				item.formData = this.formData
				item.taskId = this.taskId
				item.selectList = !item.value ? [] : this.selectList
				uni.navigateTo({
					url: '/pages/workFlow/candiDateUserSelect/index?data=' + encodeURIComponent(JSON.stringify(
						item))
				})
			},

			selectConfirm(e, id) {
				let selectData = e;
				let selectVal = [];
				let val = [];
				let selectId = [];
				let nodeId = '';
				if (!selectData.length) {
					delete this.selectVal[id]
					delete this.selectId[id]
					for (let i = 0; i < this.candidateForm.list.length; i++) {
						if (id === this.candidateForm.list[i].nodeId) {
							this.candidateForm.list[i].value = ""
						}
					}
				}
				for (let i = 0; i < this.candidateForm.list.length; i++) {
					for (let o = 0; o < selectData.length; o++) {
						if (selectData[o].nodeId === this.candidateForm.list[i].nodeId) {
							nodeId = selectData[o].nodeId
							val.push(selectData[o].userName)
							selectVal.push(selectData[o])
							this.candidateForm.list[i].value = val.join(',')
							selectId.push(selectData[o].userId)
							this.$set(this.selectId, selectData[o].nodeId, selectId)
						}
					}
				}
				this.$set(this.selectVal, nodeId, selectVal)
			},
			submit() {
				const query = {
					candidateType: this.candidateType,
					branchList: this.candidateForm.branchList
				}
				if (this.isCandidate) {
					query.candidateList = this.selectId
					for (let rules of this.candidateForm.list) {
						if (!rules.value) return this.$u.toast(
							`${rules.nodeName}不能为空`
						)
					}
				}
				this.$emit('submitCandidate', query);
			},
			cancel() {
				this.close()
			},
			close() {
				this.$emit('input', false);
			}

		}
	};
</script>
<style lang="scss" scoped>
	.candidateForm-v {

		.jnpf-wrap,
		.jnpf-wrap-form {
			padding: 0;
		}
	}
</style>