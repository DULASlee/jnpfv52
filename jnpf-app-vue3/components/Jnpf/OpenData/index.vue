<template>
	<u-input input-align='right' :modelValue="modelValue" placeholder="系统自动生成" disabled />
</template>

<script>
	export default {
		name: 'jnpf-open-data',
		props: {
			modelValue: {
				type: String,
				default: ''
			},
			/**
			 * currUser - 当前用户
			 * currTime - 当前时间
			 * currOrganize - 所属组织
			 * currPosition - 所属岗位
			 */
			type: {
				type: String,
				default: ''
			},
			showLevel: {
				type: String,
				default: 'last'
			},
		},
		data() {
			return {
				innerValue: '',
				userInfo: ''
			}
		},
		watch: {
			showLevel() {
				this.setDefault()
			}
		},
		created() {
			this.userInfo = uni.getStorageSync('userInfo') || {}
			this.setDefault()
		},
		methods: {
			setDefault() {
				if (this.type === 'currUser') {
					this.innerValue = this.userInfo.userName + '/' + this.userInfo.userAccount
					if (!this.userInfo.userName && !this.userInfo.userAccount) this.innerValue = ""
				}
				if (this.type === 'currTime') {
					this.innerValue = this.$u.timeFormat(new Date(), 'yyyy-mm-dd hh:MM:ss')
				}
				if (this.type === 'currOrganize') {
					this.innerValue = this.showLevel === 'last' ? this.userInfo.departmentName : this.userInfo.organizeName
				}
				if (this.type === 'currPosition') {
					this.innerValue = this.userInfo.positionName || ""
				}
			}
		}
	}
</script>