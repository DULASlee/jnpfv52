<template>
	<view class="jnpf-wrap personalData">
		<view style="background-color: #fff;" class="u-p-l-20 u-p-r-20">
			<u-form :model="dataForm" :errorType="['toast']" label-position="left" label-width="150" label-align="right"
				ref="dataForm">
				<u-form-item label="姓名" prop='realName' required>
					<u-input input-align='right' v-model="dataForm.realName" placeholder="请输入"></u-input>
				</u-form-item>
				<u-form-item label="民族">
					<JnpfSelect v-model="dataForm.nation" placeholder="请选择" :options='nationOptions' />
				</u-form-item>
				<u-form-item label="性别">
					<JnpfSelect v-model="dataForm.gender" placeholder="请选择" :options='genderOptions' :props='props' />
				</u-form-item>
				<u-form-item label="籍贯">
					<u-input input-align='right' v-model="dataForm.nativePlace" placeholder="请输入"></u-input>
				</u-form-item>
				<u-form-item label="证件类型">
					<JnpfSelect v-model="dataForm.certificatesType" placeholder="请选择"
						:options='certificatesTypeOptions' />
				</u-form-item>
				<u-form-item label="证件号码">
					<u-input input-align='right' v-model="dataForm.certificatesNumber" placeholder="请输入">
					</u-input>
				</u-form-item>
				<u-form-item label="文化程度">
					<JnpfSelect v-model="dataForm.education" placeholder="请选择" :options='educationOptions' />
				</u-form-item>
				<u-form-item label="出生年月">
					<JnpfDatePicker v-model="dataForm.birthday" placeholder="请选择" />
				</u-form-item>
				<u-form-item label="办公电话">
					<u-input input-align='right' v-model="dataForm.telePhone" placeholder="请输入">
					</u-input>
				</u-form-item>
				<u-form-item label="办公座机">
					<u-input input-align='right' v-model="dataForm.landline" placeholder="请输入">
					</u-input>
				</u-form-item>
				<u-form-item label="手机号码">
					<u-input input-align='right' v-model="dataForm.mobilePhone" placeholder="请输">
					</u-input>
				</u-form-item>
				<u-form-item label="电子邮箱">
					<u-input input-align='right' v-model="dataForm.email" placeholder="请输入">
					</u-input>
				</u-form-item>
				<u-form-item label="紧急联系">
					<u-input input-align='right' v-model="dataForm.urgentContacts" placeholder="请输入">
					</u-input>
				</u-form-item>
				<u-form-item label="紧急电话">
					<u-input input-align='right' v-model="dataForm.urgentTelePhone" placeholder="请输入">
					</u-input>
				</u-form-item>
				<u-form-item label="通讯地址">
					<u-input input-align='right' v-model="dataForm.postalAddress" placeholder="请输入">
					</u-input>
				</u-form-item>
				<u-form-item label="自我介绍">
					<u-input input-align='right' v-model="dataForm.signature" placeholder="请输入" type="textarea" />
				</u-form-item>
			</u-form>
		</view>
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" type="primary" @click='submit'>保存</u-button>
		</view>
	</view>
</template>

<script>
	import {
		UpdateUser
	} from '@/api/common'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
		props: {
			personalData: {
				type: Object,
				default: () => ({})
			}
		},
		data() {
			const data = {
				show: false,
				props: {
					label: 'fullName',
					value: 'enCode'
				},
				dataForm: {
					birthday: null,
					certificatesNumber: "",
					certificatesType: "",
					education: "",
					email: "",
					gender: "",
					landline: "",
					mobilePhone: "",
					nation: "",
					nativePlace: "",
					postalAddress: "",
					realName: "",
					signature: null,
					telePhone: "",
					urgentContacts: "",
					urgentTelePhone: "",
					id: null
				},
				nationOptions: [],
				genderOptions: [],
				certificatesTypeOptions: [],
				educationOptions: [],
				rules: {
					realName: [{
						required: true,
						message: '请输入姓名',
						trigger: ['change', 'blur'],
					}]
				}
			}
			return data
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		watch: {
			personalData: {
				handler(val) {
					this.init()
				},
				deep: true,
				immediate: true
			}
		},
		mounted() {
			this.$refs.dataForm.setRules(this.rules);
		},
		methods: {
			init() {
				let initData = JSON.parse(JSON.stringify(this.personalData))
				for (let key in initData) {
					for (let k in this.dataForm) {
						if (key === k) {
							this.dataForm[key] = initData[key]
						}
					}
				}
				this.getOptions()
			},
			getOptions() {
				baseStore.getDictionaryData({
					sort: 'Education'
				}).then((res) => {
					this.educationOptions = JSON.parse(JSON.stringify(res))
					baseStore.getDictionaryData({
						sort: 'certificateType'
					}).then((res) => {
						this.certificatesTypeOptions = JSON.parse(JSON.stringify(res))
					})
					baseStore.getDictionaryData({
						sort: 'sex'
					}).then(res => {
						this.genderOptions = JSON.parse(JSON.stringify(res))
					})
					baseStore.getDictionaryData({
						sort: 'Nation'
					}).then(res => {
						this.nationOptions = JSON.parse(JSON.stringify(res))
					})
				})
				this.show = true
			},
			submit() {
				this.$refs.dataForm.validate(valid => {
					if (valid) {
						UpdateUser(this.dataForm).then(res => {
							uni.showToast({
								title: '保存成功',
								duration: 800,
								icon: 'none'
							});
							setTimeout(() => {
								uni.navigateBack()
							}, 1000)
						})
					}
				});
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.slot-btn {
		width: 329rpx;
		height: 140rpx;
		display: flex;
		justify-content: center;
		align-items: center;
		background: rgb(244, 245, 246);
		border-radius: 10rpx;
	}

	.slot-btn__hover {
		background-color: rgb(235, 236, 238);
	}
</style>