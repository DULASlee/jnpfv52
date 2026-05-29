<template>
	<u-popup v-model="showPopup" mode="center" length="auto">
		<view class="jnpf-wrap jnpf-wrap-workflow">
			<u-form :model="dataForm" :rules="rules" ref="dataForm" label-position="left" label-width="150"
				label-align="left">
				<u-form-item label="旧密码" prop="oldPassword" required>
					<u-input v-model="dataForm.oldPassword" placeholder="旧密码" type="password"></u-input>
				</u-form-item>
				<u-form-item label="新密码" prop="password" required>
					<u-input v-model="dataForm.password" placeholder="新密码" type="password"></u-input>
				</u-form-item>
				<u-form-item label="重复密码" prop="repeatPsd" required>
					<u-input v-model="dataForm.repeatPsd" placeholder="重复密码" type="password"></u-input>
				</u-form-item>
				<u-form-item label="验证码" prop="code" required>
					<view class="u-flex">
						<u-input v-model="dataForm.code" placeholder="验证码"></u-input>
						<view style="flex: 0.1">
							<u-image :showLoading="true" :src="baseURL + imgUrl" width="130px" height="38px"
								@click="changeCode">
							</u-image>
						</view>
					</view>
				</u-form-item>
				<u-button class="buttom-btn" type="primary" @click.stop="dataFormSubmit">
					{{ "保存" }}
				</u-button>
			</u-form>
		</view>
	</u-popup>
</template>

<script>
	import md5Libs from "@/uni_modules/vk-uview-ui/libs/function/md5";
	export default {
		props: {
			passwordShow: {
				type: Boolean,
				default: true
			},
			baseForm: {
				type: Object,
				default: () => {}
			},
		},
		data() {
			var validatePass = (rule, value, callback) => {
				// const passwordreg = /(?=.*\d)(?=.*[a-zA-Z])(?=.*[^a-zA-Z0-9]).{6,16}/
				//是否包含数字
				const containsNumbers = /[0-9]+/;
				//是否包含小写字符
				const includeLowercaseLetters = /[a-z]+/;
				//是否包含大写字符
				const includeUppercaseLetters = /[A-Z]+/;
				//是否包含字符
				const containsCharacters = /\W/;
				if (value === "") {
					callback(new Error("新密码不能为空"));
				} else if (this.baseForm.passwordStrengthLimit == 1) {
					if (this.baseForm.passwordLengthMin) {
						if (value.length < this.baseForm.passwordLengthMinNumber) {
							callback(
								new Error(
									"新密码长度不能小于" +
									this.baseForm.passwordLengthMinNumber +
									"位"
								)
							);
						}
					}
					if (this.baseForm.containsNumbers) {
						if (!containsNumbers.test(value)) {
							callback(new Error("新密码必须包含数字"));
						}
					}
					if (this.baseForm.includeLowercaseLetters) {
						if (!includeLowercaseLetters.test(value)) {
							callback(new Error("新密码必须包含小写字母"));
						}
					}
					if (this.baseForm.includeUppercaseLetters) {
						if (!includeUppercaseLetters.test(value)) {
							callback(new Error("新密码必须包含大写字字母"));
						}
					}
					if (this.baseForm.containsCharacters) {
						if (!containsCharacters.test(value)) {
							callback(new Error("新密码必须包含字符"));
						}
					}
					callback();
				} else {
					callback();
				}
			};
			var validatePass2 = (rule, value, callback) => {
				if (value === "") {
					callback(new Error("重复密码不能为空"));
				} else if (value !== this.dataForm.password) {
					callback(new Error("两次密码输入不一致"));
				} else {
					callback();
				}
			};
			return {
				showPopup: false,
				imgUrl: "",
				timestamp: "",
				dataForm: {
					oldPassword: "",
					password: "",
					repeatPsd: "",
					code: "",
					timestamp: "",
				},
				rules: {
					oldPassword: [{
						required: true,
						message: "旧密码不能为空",
						trigger: "blur",
					}, ],
					password: [{
						required: true,
						validator: validatePass,
						trigger: "blur",
					}, ],
					repeatPsd: [{
						required: true,
						validator: validatePass2,
						trigger: "blur",
					}, ],
					code: [{
						required: true,
						message: "验证码不能为空",
						trigger: "blur",
					}, ],
				},
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL;
			}
		},
		watch: {
			passwordShow(val) {
				if (val) {
					this.showPopup = val
					this.$nextTick(() => {
						this.$refs.dataForm.setRules(this.rules);
					});
				}
			},
		},
		created() {
			this.changeCode()
		},
		methods: {
			changeCode() {
				let timestamp = Math.random();
				this.timestamp = timestamp;
				this.imgUrl = `/api/file/ImageCode/${timestamp}`;
			},
			dataFormSubmit() {
				this.$refs["dataForm"].validate((valid) => {
					if (valid) {
						let query = {
							oldPassword: md5Libs.md5(this.dataForm.oldPassword),
							password: md5Libs.md5(this.dataForm.password),
							code: this.dataForm.code,
							timestamp: this.timestamp,
						};
						this.$emit('submit', query)
					}
				});
			},
		}
	}
</script>

<style>
</style>